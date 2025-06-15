using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Unity.BossRoom.Navigation
{
    public sealed class DynamicNavPath : IDisposable
    {
        /// <summary>
        /// The tolerance to decide whether the path needs to be recalculated when the position of a target transform changed.
        /// </summary>
        const float KRepathToleranceSqr = 9f;

        NavMeshAgent _mAgent;

        NavigationSystem _mNavigationSystem;

        /// <summary>
        /// The target position value which was used to calculate the current path.
        /// This get stored to make sure the path gets recalculated if the target
        /// </summary>
        Vector3 _mCurrentPathOriginalTarget;

        /// <summary>
        /// This field caches a NavMesh Path so that we don't have to allocate a new one each time.
        /// </summary>
        NavMeshPath _mNavMeshPath;

        /// <summary>
        /// The remaining path points to follow to reach the target position.
        /// </summary>
        List<Vector3> _mPath;

        /// <summary>
        /// The target position of this path.
        /// </summary>
        Vector3 _mPositionTarget;

        /// <summary>
        /// A moving transform target, the path will readjust when the target moves. If this is non-null, it takes precedence over m_PositionTarget.
        /// </summary>
        Transform _mTransformTarget;

        /// <summary>
        /// Creates a new instance of the <see cref="DynamicNavPath"/>.
        /// </summary>
        /// <param name="agent">The NavMeshAgent of the object which uses this path.</param>
        /// <param name="navigationSystem">The navigation system which updates this path.</param>
        public DynamicNavPath(NavMeshAgent agent, NavigationSystem navigationSystem)
        {
            _mAgent = agent;
            _mPath = new List<Vector3>();
            _mNavMeshPath = new NavMeshPath();
            _mNavigationSystem = navigationSystem;

            navigationSystem.OnNavigationMeshChanged += OnNavMeshChanged;
        }

        Vector3 TargetPosition => _mTransformTarget != null ? _mTransformTarget.position : _mPositionTarget;

        /// <summary>
        /// Set the target of this path to follow a moving transform.
        /// </summary>
        /// <param name="target">The transform to follow.</param>
        public void FollowTransform(Transform target)
        {
            _mTransformTarget = target;
        }

        /// <summary>
        /// Set the target of this path to a static position target.
        /// </summary>
        /// <param name="target">The target position.</param>
        public void SetTargetPosition(Vector3 target)
        {
            // If there is an nav mesh area close to the target use a point inside the nav mesh instead.
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                target = hit.position;
            }

            _mPositionTarget = target;
            _mTransformTarget = null;
            RecalculatePath();
        }

        /// <summary>
        /// Call this to recalculate the path when the navigation mesh or dynamic obstacles changed.
        /// </summary>
        void OnNavMeshChanged()
        {
            RecalculatePath();
        }

        /// <summary>
        /// Clears the path.
        /// </summary>
        public void Clear()
        {
            _mPath.Clear();
        }

        /// <summary>
        /// Gets the movement vector for moving this object while following the path. This function changes the state of the path and should only be called once per tick.
        /// </summary>
        /// <param name="distance">The distance to move.</param>
        /// <returns>Returns the movement vector.</returns>
        public Vector3 MoveAlongPath(float distance)
        {
            if (_mTransformTarget != null)
            {
                OnTargetPositionChanged(TargetPosition);
            }

            if (_mPath.Count == 0)
            {
                return Vector3.zero;
            }

            var currentPredictedPosition = _mAgent.transform.position;
            var remainingDistance = distance;

            while (remainingDistance > 0)
            {
                var toNextPathPoint = _mPath[0] - currentPredictedPosition;

                // If end point is closer then distance to move
                if (toNextPathPoint.sqrMagnitude < remainingDistance * remainingDistance)
                {
                    currentPredictedPosition = _mPath[0];
                    _mPath.RemoveAt(0);
                    remainingDistance -= toNextPathPoint.magnitude;
                }

                // Move towards point
                currentPredictedPosition += toNextPathPoint.normalized * remainingDistance;

                // There is definitely no remaining distance to cover here.
                break;
            }

            return currentPredictedPosition - _mAgent.transform.position;
        }

        void OnTargetPositionChanged(Vector3 newTarget)
        {
            if (_mPath.Count == 0)
            {
                RecalculatePath();
            }

            if ((newTarget - _mCurrentPathOriginalTarget).sqrMagnitude > KRepathToleranceSqr)
            {
                RecalculatePath();
            }
        }

        /// <summary>
        /// Recalculates the cached navigationPath
        /// </summary>
        void RecalculatePath()
        {
            _mCurrentPathOriginalTarget = TargetPosition;
            _mAgent.CalculatePath(TargetPosition, _mNavMeshPath);

            _mPath.Clear();

            var corners = _mNavMeshPath.corners;

            for (int i = 1; i < corners.Length; i++) // Skip the first corner because it is the starting point.
            {
                _mPath.Add(corners[i]);
            }
        }

        public void Dispose()
        {
            _mNavigationSystem.OnNavigationMeshChanged -= OnNavMeshChanged;
        }
    }
}
