using Kobold.GameManagement;
using UnityEngine;

namespace Kobold.Core
{
    /// <summary>
    /// Base class for singleton MonoBehaviours that properly handles Unity lifecycle
    /// </summary>
    /// <typeparam name="T">The type of the singleton</typeparam>
    public abstract class KoboldSingletonBehaviour<T> : MonoBehaviour where T : KoboldSingletonBehaviour<T>
    {
        private static T _instance;
        private static bool _applicationIsQuitting = false;
        private static readonly object Lock = new object();

        /// <summary>
        /// Gets the singleton instance. Returns null during application quit.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    return null;
                }

                lock (Lock)
                {
                    if (_instance == null && Application.isPlaying)
                    {
                        _instance = FindFirstObjectByType<T>();
                        
                        if (_instance == null)
                        {
                            Debug.LogWarning($"[{typeof(T).Name}] No instance found. Instance will be null.");
                        }
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Checks if an instance exists without creating one
        /// </summary>
        public static bool HasInstance => _instance != null && !_applicationIsQuitting;

        /// <summary>
        /// Gets the instance without creating one if it doesn't exist
        /// </summary>
        public static T InstanceIfExists => _applicationIsQuitting ? null : _instance;

        /// <summary>
        /// Reset static state when domain reloads (entering play mode)
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
            _applicationIsQuitting = false;
        }

        protected virtual void Awake()
        {
            // Check if we should persist
            if (!KoboldPersistentObjectManager.RegisterPersistentObject(this))
            {
                Destroy(gameObject);
                return;
            }

            lock (Lock)
            {
                if (_instance == null)
                {
                    _instance = this as T;
                    DontDestroyOnLoad(gameObject);
                    OnAwakeSingleton();
                }
                else if (_instance != this)
                {
                    Debug.LogWarning($"[{typeof(T).Name}] Duplicate instance found. Destroying duplicate on {gameObject.name}");
                    Destroy(gameObject);
                }
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                lock (Lock)
                {
                    _instance = null;
                    if (!_applicationIsQuitting)
                    {
                        OnDestroySingleton();
                    }
                }
            }
        }

        /// <summary>
        /// Called when the singleton is successfully initialized
        /// </summary>
        protected virtual void OnAwakeSingleton() { }

        /// <summary>
        /// Called when the singleton is destroyed (not during application quit)
        /// </summary>
        protected virtual void OnDestroySingleton() { }
    }
}