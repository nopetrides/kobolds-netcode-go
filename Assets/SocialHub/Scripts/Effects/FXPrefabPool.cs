using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.Multiplayer.Samples.SocialHub.Effects
{
    /// <summary>
    /// Intended to be used as a client-side only pool
    /// </summary>
    class FXPrefabPool : MonoBehaviour
    {
        [SerializeField]
        GameObject m_Prefab;

        static Dictionary<GameObject, FXPrefabPool> _mFxPool = new Dictionary<GameObject, FXPrefabPool>();

        ObjectPool<GameObject> _mPool;

        public static FXPrefabPool GetFxPool(GameObject prefab)
        {
            if (!_mFxPool.ContainsKey(prefab))
            {
                var instance = new GameObject($"{prefab.name}-FxPool");
                var fxPool = instance.AddComponent<FXPrefabPool>();
                fxPool.Initialize(prefab);
                _mFxPool.Add(prefab, fxPool);

                // Move the pool far above the players (i.e. out of sight)
                instance.transform.position = Vector3.up * 5000;
                DontDestroyOnLoad(instance);
            }

            return _mFxPool[prefab];
        }

        void Initialize(GameObject gameObject, int startCapacity = 10, int maxCapacity = 100)
        {
            m_Prefab = gameObject;

            GameObject CreateFunc()
            {
                var pooledInstance = Instantiate(m_Prefab);
                pooledInstance.SetActive(false);
                var fxBase = pooledInstance.GetComponent<BaseFxObject>();
                fxBase.SetFxPool(this);
                fxBase.transform.parent = transform;
                return pooledInstance;
            }

            void OnGet(GameObject obj)
            {
                if (obj)
                {
                    obj.SetActive(true);
                }
            }

            void OnRelease(GameObject obj)
            {
                if (obj)
                {
                    obj.SetActive(false);
                }
            }

            void OnDestroyPoolObject(GameObject obj)
            {
                if (obj)
                {
                    OnDestroyObject(obj);
                    Destroy(obj);
                }
            }

            _mPool = new ObjectPool<GameObject>(createFunc: CreateFunc, actionOnGet: OnGet, actionOnRelease: OnRelease, actionOnDestroy: OnDestroyPoolObject,
                defaultCapacity: startCapacity, maxSize: maxCapacity);
        }

        protected virtual void OnDestroyObject(GameObject obj) { }

        protected virtual void OnGetInstance(GameObject obj) { }

        protected virtual void OnReleaseInstance(GameObject obj) { }

        internal GameObject GetInstance()
        {
            var objInstance = _mPool.Get();
            objInstance.transform.parent = null;
            OnGetInstance(objInstance);
            return objInstance;
        }

        internal void ReleaseInstance(GameObject gameObject)
        {
            gameObject.transform.parent = null;
            OnReleaseInstance(gameObject);
            _mPool.Release(gameObject);
            gameObject.transform.parent = transform;
        }
    }
}
