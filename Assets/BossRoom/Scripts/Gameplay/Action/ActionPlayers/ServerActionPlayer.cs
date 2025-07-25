using System.Collections.Generic;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.BossRoom.Gameplay.Actions
{
    /// <summary>
    /// Class responsible for playing back action inputs from user.
    /// </summary>
    public class ServerActionPlayer
    {
        private ServerCharacter _mServerCharacter;

        private ServerCharacterMovement _mMovement;

        private List<Action> _mQueue;

        private List<Action> _mNonBlockingActions;

        private Dictionary<ActionID, float> _mLastUsedTimestamps;

        /// <summary>
        /// To prevent the action queue from growing without bound, we cap its play time to this number of seconds. We can only ever estimate
        /// the time-length of the queue, since actions are allowed to block indefinitely. But this is still a useful estimate that prevents
        /// us from piling up a large number of small actions.
        /// </summary>
        private const float KMaxQueueTimeDepth = 1.6f;

        private ActionRequestData _mPendingSynthesizedAction = new ActionRequestData();
        private bool _mHasPendingSynthesizedAction;

        public ServerActionPlayer(ServerCharacter serverCharacter)
        {
            _mServerCharacter = serverCharacter;
            _mMovement = serverCharacter.Movement;
            _mQueue = new List<Action>();
            _mNonBlockingActions = new List<Action>();
            _mLastUsedTimestamps = new Dictionary<ActionID, float>();
        }

        /// <summary>
        /// Perform a sequence of actions.
        /// </summary>
        public void PlayAction(ref ActionRequestData action)
        {
            if (!action.ShouldQueue && _mQueue.Count > 0 &&
                (_mQueue[0].Config.ActionInterruptible ||
                    _mQueue[0].Config.CanBeInterruptedBy(action.ActionID)))
            {
                ClearActions(false);
            }

            if (GetQueueTimeDepth() >= KMaxQueueTimeDepth)
            {
                //the queue is too big (in execution seconds) to accommodate any more actions, so this action must be discarded.
                return;
            }

            var newAction = ActionFactory.CreateActionFromData(ref action);
            _mQueue.Add(newAction);
            if (_mQueue.Count == 1) { StartAction(); }
        }

        public void ClearActions(bool cancelNonBlocking)
        {
            if (_mQueue.Count > 0)
            {
                // Since this action was canceled, we don't want the player to have to wait Description.ReuseTimeSeconds
                // to be able to start it again. It should be restartable immediately!
                _mLastUsedTimestamps.Remove(_mQueue[0].ActionID);
                _mQueue[0].Cancel(_mServerCharacter);
            }

            //clear the action queue
            {
                var removedActions = ListPool<Action>.Get();

                foreach (var action in _mQueue)
                {
                    removedActions.Add(action);
                }

                _mQueue.Clear();

                foreach (var action in removedActions)
                {
                    TryReturnAction(action);
                }

                ListPool<Action>.Release(removedActions);
            }


            if (cancelNonBlocking)
            {
                var removedActions = ListPool<Action>.Get();

                foreach (var action in _mNonBlockingActions)
                {
                    action.Cancel(_mServerCharacter);
                    removedActions.Add(action);
                }
                _mNonBlockingActions.Clear();

                foreach (var action in removedActions)
                {
                    TryReturnAction(action);
                }

                ListPool<Action>.Release(removedActions);
            }
        }

        /// <summary>
        /// If an Action is active, fills out 'data' param and returns true. If no Action is active, returns false.
        /// This only refers to the blocking action! (multiple non-blocking actions can be running in the background, and
        /// this will still return false).
        /// </summary>
        public bool GetActiveActionInfo(out ActionRequestData data)
        {
            if (_mQueue.Count > 0)
            {
                data = _mQueue[0].Data;
                return true;
            }
            else
            {
                data = new ActionRequestData();
                return false;
            }
        }

        /// <summary>
        /// Figures out if an action can be played now, or if it would automatically fail because it was
        /// used too recently. (Meaning that its ReuseTimeSeconds hasn't elapsed since the last use.)
        /// </summary>
        /// <param name="actionID">the action we want to run</param>
        /// <returns>true if the action can be run now, false if more time must elapse before this action can be run</returns>
        public bool IsReuseTimeElapsed(ActionID actionID)
        {
            if (_mLastUsedTimestamps.TryGetValue(actionID, out float lastTimeUsed))
            {
                var abilityConfig = GameDataSource.Instance.GetActionPrototypeByID(actionID).Config;

                float reuseTime = abilityConfig.ReuseTimeSeconds;
                if (reuseTime > 0 && Time.time - lastTimeUsed < reuseTime)
                {
                    // still needs more time!
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns how many actions are actively running. This includes all non-blocking actions,
        /// and the one blocking action at the head of the queue (if present).
        /// </summary>
        public int RunningActionCount
        {
            get
            {
                return _mNonBlockingActions.Count + (_mQueue.Count > 0 ? 1 : 0);
            }
        }

        /// <summary>
        /// Starts the action at the head of the queue, if any.
        /// </summary>
        private void StartAction()
        {
            if (_mQueue.Count > 0)
            {
                float reuseTime = _mQueue[0].Config.ReuseTimeSeconds;
                if (reuseTime > 0
                    && _mLastUsedTimestamps.TryGetValue(_mQueue[0].ActionID, out float lastTimeUsed)
                    && Time.time - lastTimeUsed < reuseTime)
                {
                    // we've already started one of these too recently
                    AdvanceQueue(false); // note: this will call StartAction() recursively if there's more stuff in the queue ...
                    return;              // ... so it's important not to try to do anything more here
                }

                int index = SynthesizeTargetIfNecessary(0);
                SynthesizeChaseIfNecessary(index);

                _mQueue[0].TimeStarted = Time.time;
                bool play = _mQueue[0].OnStart(_mServerCharacter);
                if (!play)
                {
                    //actions that exited out in the "Start" method will not have their End method called, by design.
                    AdvanceQueue(false); // note: this will call StartAction() recursively if there's more stuff in the queue ...
                    return;              // ... so it's important not to try to do anything more here
                }

                // if this Action is interruptible, that means movement should interrupt it... character needs to be stationary for this!
                // So stop any movement that's already happening before we begin
                if (_mQueue[0].Config.ActionInterruptible && !_mMovement.IsPerformingForcedMovement())
                {
                    _mMovement.CancelMove();
                }

                // remember the moment when we successfully used this Action!
                _mLastUsedTimestamps[_mQueue[0].ActionID] = Time.time;

                if (_mQueue[0].Config.ExecTimeSeconds == 0 && _mQueue[0].Config.BlockingMode == BlockingModeType.OnlyDuringExecTime)
                {
                    //this is a non-blocking action with no exec time. It should never be hanging out at the front of the queue (not even for a frame),
                    //because it could get cleared if a new Action came in in that interval.
                    _mNonBlockingActions.Add(_mQueue[0]);
                    AdvanceQueue(false); // note: this will call StartAction() recursively if there's more stuff in the queue ...
                    return;              // ... so it's important not to try to do anything more here
                }
            }
        }

        /// <summary>
        /// Synthesizes a Chase Action for the action at the Head of the queue, if necessary (the base action must have a target,
        /// and must have the ShouldClose flag set). This method must not be called when the queue is empty.
        /// </summary>
        /// <returns>The new index of the Action being operated on.</returns>
        private int SynthesizeChaseIfNecessary(int baseIndex)
        {
            Action baseAction = _mQueue[baseIndex];

            if (baseAction.Data.ShouldClose && baseAction.Data.TargetIds != null)
            {
                ActionRequestData data = new ActionRequestData
                {
                    ActionID = GameDataSource.Instance.GeneralChaseActionPrototype.ActionID,
                    TargetIds = baseAction.Data.TargetIds,
                    Amount = baseAction.Config.Range
                };
                baseAction.Data.ShouldClose = false; //you only get to do this once!
                Action chaseAction = ActionFactory.CreateActionFromData(ref data);
                _mQueue.Insert(baseIndex, chaseAction);
                return baseIndex + 1;
            }
            return baseIndex;
        }

        /// <summary>
        /// Targeted skills should implicitly set the active target of the character, if not already set.
        /// </summary>
        /// <param name="baseIndex">The new index of the base action in m_Queue</param>
        /// <returns></returns>
        private int SynthesizeTargetIfNecessary(int baseIndex)
        {
            Action baseAction = _mQueue[baseIndex];
            var targets = baseAction.Data.TargetIds;

            if (targets != null &&
                targets.Length == 1 &&
                targets[0] != _mServerCharacter.TargetId.Value)
            {
                //if this is a targeted skill (with a single requested target), and it is different from our
                //active target, then we synthesize a TargetAction to change  our target over.

                ActionRequestData data = new ActionRequestData
                {
                    ActionID = GameDataSource.Instance.GeneralTargetActionPrototype.ActionID,
                    TargetIds = baseAction.Data.TargetIds
                };

                //this shouldn't run redundantly, because the next time the base Action comes up to play, its Target
                //and the active target in our NetState should match.
                Action targetAction = ActionFactory.CreateActionFromData(ref data);
                _mQueue.Insert(baseIndex, targetAction);
                return baseIndex + 1;
            }

            return baseIndex;
        }

        /// <summary>
        /// Optionally end the currently playing action, and advance to the next Action that wants to play.
        /// </summary>
        /// <param name="endRemoved">if true we call End on the removed element.</param>
        private void AdvanceQueue(bool endRemoved)
        {
            if (_mQueue.Count > 0)
            {
                if (endRemoved)
                {
                    _mQueue[0].End(_mServerCharacter);
                    if (_mQueue[0].ChainIntoNewAction(ref _mPendingSynthesizedAction))
                    {
                        _mHasPendingSynthesizedAction = true;
                    }
                }
                var action = _mQueue[0];
                _mQueue.RemoveAt(0);
                TryReturnAction(action);
            }

            // now start the new Action! ... unless we now have a pending Action that will supercede it
            if (!_mHasPendingSynthesizedAction || _mPendingSynthesizedAction.ShouldQueue)
            {
                StartAction();
            }
        }

        private void TryReturnAction(Action action)
        {
            if (_mQueue.Contains(action))
            {
                return;
            }

            if (_mNonBlockingActions.Contains(action))
            {
                return;
            }

            ActionFactory.ReturnAction(action);
        }

        public void OnUpdate()
        {
            if (_mHasPendingSynthesizedAction)
            {
                _mHasPendingSynthesizedAction = false;
                PlayAction(ref _mPendingSynthesizedAction);
            }

            if (_mQueue.Count > 0 && _mQueue[0].ShouldBecomeNonBlocking())
            {
                // the active action is no longer blocking, meaning it should be moved out of the blocking queue and into the
                // non-blocking one. (We use this for e.g. projectile attacks, so the projectiles can keep flying, but
                // the player can enqueue other actions in the meantime.)
                _mNonBlockingActions.Add(_mQueue[0]);
                AdvanceQueue(false);
            }

            // if there's a blocking action, update it
            if (_mQueue.Count > 0)
            {
                if (!UpdateAction(_mQueue[0]))
                {
                    AdvanceQueue(true);
                }
            }

            // if there's non-blocking actions, update them! We do this in reverse-order so we can easily remove expired actions.
            for (int i = _mNonBlockingActions.Count - 1; i >= 0; --i)
            {
                Action runningAction = _mNonBlockingActions[i];
                if (!UpdateAction(runningAction))
                {
                    // it's dead!
                    runningAction.End(_mServerCharacter);
                    _mNonBlockingActions.RemoveAt(i);
                    TryReturnAction(runningAction);
                }
            }
        }

        /// <summary>
        /// Calls a given Action's Update() and decides if the action is still alive.
        /// </summary>
        /// <returns>true if the action is still active, false if it's dead</returns>
        private bool UpdateAction(Action action)
        {
            bool keepGoing = action.OnUpdate(_mServerCharacter);
            bool expirable = action.Config.DurationSeconds > 0f; //non-positive value is a sentinel indicating the duration is indefinite.
            var timeElapsed = Time.time - action.TimeStarted;
            bool timeExpired = expirable && timeElapsed >= action.Config.DurationSeconds;
            return keepGoing && !timeExpired;
        }

        /// <summary>
        /// How much time will it take all remaining Actions in the queue to play out? This sums up all the time each Action is blocking,
        /// which is different from each Action's duration. Note that this is an ESTIMATE. An action may block the queue indefinitely if it wishes.
        /// </summary>
        /// <returns>The total "time depth" of the queue, or how long it would take to play in seconds, if no more actions were added. </returns>
        private float GetQueueTimeDepth()
        {
            if (_mQueue.Count == 0) { return 0; }

            float totalTime = 0;
            foreach (var action in _mQueue)
            {
                var info = action.Config;
                float actionTime = info.BlockingMode == BlockingModeType.OnlyDuringExecTime ? info.ExecTimeSeconds :
                                    info.BlockingMode == BlockingModeType.EntireDuration ? info.DurationSeconds :
                                    throw new System.Exception($"Unrecognized blocking mode: {info.BlockingMode}");
                totalTime += actionTime;
            }

            return totalTime - _mQueue[0].TimeRunning;
        }

        public void CollisionEntered(Collision collision)
        {
            if (_mQueue.Count > 0)
            {
                _mQueue[0].CollisionEntered(_mServerCharacter, collision);
            }
        }

        /// <summary>
        /// Gives all active Actions a chance to alter a gameplay variable.
        /// </summary>
        /// <remarks>
        /// Note that this handles both positive alterations (commonly called "buffs")
        /// AND negative ones ("debuffs").
        /// </remarks>
        /// <param name="buffType">Which gameplay variable is being calculated</param>
        /// <returns>The final ("buffed") value of the variable</returns>
        public float GetBuffedValue(Action.BuffableValue buffType)
        {
            float buffedValue = Action.GetUnbuffedValue(buffType);
            if (_mQueue.Count > 0)
            {
                _mQueue[0].BuffValue(buffType, ref buffedValue);
            }
            foreach (var action in _mNonBlockingActions)
            {
                action.BuffValue(buffType, ref buffedValue);
            }
            return buffedValue;
        }

        /// <summary>
        /// Tells all active Actions that a particular gameplay event happened, such as being hit,
        /// getting healed, dying, etc. Actions can change their behavior as a result.
        /// </summary>
        /// <param name="activityThatOccurred">The type of event that has occurred</param>
        public virtual void OnGameplayActivity(Action.GameplayActivity activityThatOccurred)
        {
            if (_mQueue.Count > 0)
            {
                _mQueue[0].OnGameplayActivity(_mServerCharacter, activityThatOccurred);
            }
            foreach (var action in _mNonBlockingActions)
            {
                action.OnGameplayActivity(_mServerCharacter, activityThatOccurred);
            }
        }


        /// <summary>
        /// Cancels the first instance of the given ActionLogic that is currently running, or all instances if cancelAll is set to true.
        /// Searches actively running actions first, then looks at the head action in the queue.
        /// </summary>
        /// <param name="logic">The ActionLogic to cancel</param>
        /// <param name="cancelAll">If true will cancel all instances; if false will just cancel the first running instance.</param>
        /// <param name="exceptThis">If set, will skip this action (useful for actions canceling other instances of themselves).</param>
        public void CancelRunningActionsByLogic(ActionLogic logic, bool cancelAll, Action exceptThis = null)
        {
            for (int i = _mNonBlockingActions.Count - 1; i >= 0; --i)
            {
                var action = _mNonBlockingActions[i];
                if (action.Config.Logic == logic && action != exceptThis)
                {
                    action.Cancel(_mServerCharacter);
                    _mNonBlockingActions.RemoveAt(i);
                    TryReturnAction(action);
                    if (!cancelAll) { return; }
                }
            }

            if (_mQueue.Count > 0)
            {
                var action = _mQueue[0];
                if (action.Config.Logic == logic && action != exceptThis)
                {
                    action.Cancel(_mServerCharacter);
                    _mQueue.RemoveAt(0);
                    TryReturnAction(action);
                }
            }
        }
    }
}

