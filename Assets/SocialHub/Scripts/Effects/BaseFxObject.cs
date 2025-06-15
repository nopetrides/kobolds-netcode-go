using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Effects
{
    /// <summary>
    /// To be used in conjunction with <see cref="FXPrefabPool"/>, derive your
    /// FX specific component that manages the FX instance from this.
    /// </summary>
    class BaseFxObject : MonoBehaviour
    {
        FXPrefabPool _mFXPrefabPool;

        public void SetFxPool(FXPrefabPool pool)
        {
            _mFXPrefabPool = pool;
        }

        internal void StopFx()
        {
            if (gameObject.activeInHierarchy)
            {
                _mFXPrefabPool.ReleaseInstance(gameObject);
            }
        }
    }
}
