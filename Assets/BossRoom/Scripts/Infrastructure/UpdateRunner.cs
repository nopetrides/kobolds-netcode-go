using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.BossRoom.Infrastructure
{
    /// <summary>
    /// Some objects might need to be on a slower update loop than the usual MonoBehaviour Update and without precise timing, e.g. to refresh data from services.
    /// Some might also not want to be coupled to a Unity object at all but still need an update loop.
    /// </summary>
    public class UpdateRunner : MonoBehaviour
    {
        class SubscriberData
        {
            public float Period;
            public float NextCallTime;
            public float LastCallTime;
        }

        readonly Queue<Action> _mPendingHandlers = new Queue<Action>();
        readonly HashSet<Action<float>> _mSubscribers = new HashSet<Action<float>>();
        readonly Dictionary<Action<float>, SubscriberData> _mSubscriberData = new Dictionary<Action<float>, SubscriberData>();

        public void OnDestroy()
        {
            _mPendingHandlers.Clear();
            _mSubscribers.Clear();
            _mSubscriberData.Clear();
        }

        /// <summary>
        /// Subscribe in order to have onUpdate called approximately every period seconds (or every frame, if period <= 0).
        /// Don't assume that onUpdate will be called in any particular order compared to other subscribers.
        /// </summary>
        public void Subscribe(Action<float> onUpdate, float updatePeriod)
        {
            if (onUpdate == null)
            {
                return;
            }

            if (onUpdate.Target == null) // Detect a local function that cannot be Unsubscribed since it could go out of scope.
            {
                Debug.LogError("Can't subscribe to a local function that can go out of scope and can't be unsubscribed from");
                return;
            }

            if (onUpdate.Method.ToString().Contains("<")) // Detect
            {
                Debug.LogError("Can't subscribe with an anonymous function that cannot be Unsubscribed, by checking for a character that can't exist in a declared method name.");
                return;
            }

            if (!_mSubscribers.Contains(onUpdate))
            {
                _mPendingHandlers.Enqueue(() =>
                {
                    if (_mSubscribers.Add(onUpdate))
                    {
                        _mSubscriberData.Add(onUpdate, new SubscriberData() { Period = updatePeriod, NextCallTime = 0, LastCallTime = Time.time });
                    }
                });
            }
        }

        /// <summary>
        /// Safe to call even if onUpdate was not previously Subscribed.
        /// </summary>
        public void Unsubscribe(Action<float> onUpdate)
        {
            _mPendingHandlers.Enqueue(() =>
            {
                _mSubscribers.Remove(onUpdate);
                _mSubscriberData.Remove(onUpdate);
            });
        }

        /// <summary>
        /// Each frame, advance all subscribers. Any that have hit their period should then act, though if they take too long they could be removed.
        /// </summary>
        void Update()
        {
            while (_mPendingHandlers.Count > 0)
            {
                _mPendingHandlers.Dequeue()?.Invoke();
            }

            foreach (var subscriber in _mSubscribers)
            {
                var subscriberData = _mSubscriberData[subscriber];

                if (Time.time >= subscriberData.NextCallTime)
                {
                    subscriber.Invoke(Time.time - subscriberData.LastCallTime);
                    subscriberData.LastCallTime = Time.time;
                    subscriberData.NextCallTime = Time.time + subscriberData.Period;
                }
            }
        }
    }
}
