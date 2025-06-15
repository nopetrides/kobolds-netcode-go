using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.BossRoom.Navigation
{
    /// <summary>
    /// This system exists to coordinate path finding and navigation functionality in a scene.
    /// The Unity NavMesh is only used to calculate navigation paths. Moving along those paths is done by this system.
    /// </summary>
    public class NavigationSystem : MonoBehaviour
    {
        public const string NavigationSystemTag = "NavigationSystem";

        /// <summary>
        /// Event that gets invoked when the navigation mesh changed. This happens when dynamic obstacles move or get active
        /// </summary>
        public event System.Action OnNavigationMeshChanged = delegate { };

        /// <summary>
        /// Whether all paths need to be recalculated in the next fixed update.
        /// </summary>
        private bool _mNavMeshChanged;

        public void OnDynamicObstacleDisabled()
        {
            _mNavMeshChanged = true;
        }

        public void OnDynamicObstacleEnabled()
        {
            _mNavMeshChanged = true;
        }

        private void FixedUpdate()
        {
            // This is done in fixed update to make sure that only one expensive global recalculation happens per fixed update.
            if (_mNavMeshChanged)
            {
                OnNavigationMeshChanged.Invoke();
                _mNavMeshChanged = false;
            }
        }

        private void OnValidate()
        {
            Assert.AreEqual(NavigationSystemTag, tag, $"The GameObject of the {nameof(NavigationSystem)} component has to use the {NavigationSystem.NavigationSystemTag} tag!");
        }
    }
}
