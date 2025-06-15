using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TheraBytes.BetterUi
{
    [HelpURL("https://documentation.therabytes.de/better-ui/GlobalApplier.html")]
    public class GlobalApplier : SingletonScriptableObject<GlobalApplier>
    {
        static string FilePath { get { return "TheraBytes/Resources/GlobalApplier"; } }

        #region Nested Types

        [Serializable]
        public class ButtonClickEvent : UnityEvent<BetterButton> { }
        [Serializable]
        public class ToggleChangedEvent : UnityEvent<BetterToggle, bool> { }

        [Serializable]
        public abstract class OverridePrefab
        {
            public GameObject Prefab;
            public string ComponentTypeName;
            public string SizerPropertyName;
            public string ScreenConfigName;
            public Component Component
            {
                get
                {
                    if (cachedComponent == null && Prefab != null)
                    {
                        cachedComponent = Prefab.GetComponent(ComponentTypeName);
                    }

                    return cachedComponent;
                }
            }

            Component cachedComponent;

        }

        [Serializable]
        public abstract class OverridePrefab<T> : OverridePrefab
        {
            public T LastCalculatedSize;
        }

        [Serializable] public class OverridePrefabFloat : OverridePrefab<float> { }
        [Serializable] public class OverridePrefabVector2Int : OverridePrefab<Vector2Int> { }
        [Serializable] public class OverridePrefabVector2 : OverridePrefab<Vector2> { }
        [Serializable] public class OverridePrefabVector3 : OverridePrefab<Vector3> { }
        [Serializable] public class OverridePrefabVector4 : OverridePrefab<Vector4> { }
        [Serializable] public class OverridePrefabMargin : OverridePrefab<Margin> { }
        [Serializable] public class OverridePrefabPadding : OverridePrefab<Padding> { }

        #endregion

        [SerializeField] ButtonClickEvent onButtonClicked = new ButtonClickEvent();
        [SerializeField] ToggleChangedEvent onToggleChanged = new ToggleChangedEvent();

        [SerializeField] List<OverridePrefabFloat> floatOverrides = new List<OverridePrefabFloat>();
        [SerializeField] List<OverridePrefabVector2Int> vector2IntOverrides = new List<OverridePrefabVector2Int>();
        [SerializeField] List<OverridePrefabVector2> vector2Overrides = new List<OverridePrefabVector2>();
        [SerializeField] List<OverridePrefabVector3> vector3Overrides = new List<OverridePrefabVector3>();
        [SerializeField] List<OverridePrefabVector4> vector4Overrides = new List<OverridePrefabVector4>();
        [SerializeField] List<OverridePrefabMargin> marginOverrides = new List<OverridePrefabMargin>();
        [SerializeField] List<OverridePrefabPadding> paddingOverrides = new List<OverridePrefabPadding>();

        Dictionary<string, List<OverridePrefab>> lookup;
        bool isCalculatingValues = false;
        

        public void UpdateCachedValues()
        {
            if(lookup == null)
            {
                lookup = new Dictionary<string, List<OverridePrefab>>();
            }

            lookup.Clear();

            UpadeCachedValues(floatOverrides);
            UpadeCachedValues(vector2IntOverrides);
            UpadeCachedValues(vector2Overrides);
            UpadeCachedValues(vector3Overrides);
            UpadeCachedValues(vector4Overrides);
            UpadeCachedValues(marginOverrides);
            UpadeCachedValues(paddingOverrides);
        }

        void UpadeCachedValues<T>(IEnumerable<OverridePrefab<T>> list)
        {
            isCalculatingValues = true;

            foreach (var ov in list)
            {
                if(ov.Prefab == null)
                {
                    Debug.LogError($"No Prefab specified.");
                    continue;
                }

                if (ov.Component == null)
                {
                        Debug.LogError($"There is no '{ov.ComponentTypeName}' component on prefab {ov.Prefab?.name ?? "<null>"}. It must be attached to the root level.");
                        continue;
                }

                var prop = ov.Component.GetType().GetProperty(ov.SizerPropertyName);
                if (prop == null)
                {
                    Debug.LogError($"There is no '{ov.SizerPropertyName}' property on component '{ov.ComponentTypeName}'. It must be a public property.");
                    continue;
                }

                var val = prop.GetValue(ov.Component);
                if (!(val is ScreenDependentSize<T> sizer))
                {
                    Debug.LogError($"The '{ov.SizerPropertyName}' property on component '{ov.ComponentTypeName}' has the type '{val?.GetType().Name ?? "<null>"}'. But it must be a subtype of ' {nameof(ScreenDependentSize)}<{typeof(T).Name}>'.");
                    continue;
                }

                ov.LastCalculatedSize = sizer.CalculateSize(ov.Component, ov.SizerPropertyName);

                if(!lookup.ContainsKey(ov.SizerPropertyName))
                {
                    lookup.Add(ov.SizerPropertyName, new List<OverridePrefab>());
                }

                lookup[ov.SizerPropertyName].Add(ov);
            }

            isCalculatingValues = false;
        }

        public bool TryGetValueOverride<T>(Component callingComponent, string propertyName, string screenConfigName, out T result)
        {
            // if values are currently updating we need to ignore the overrides
            // as one of the reference objects would be calling here
            // and 'lookup' might be null, causing infinite loops
            // (not thread safe)
            if (isCalculatingValues) 
            {
                result = default;
                return false;
            }

            if(lookup == null)
            {
                UpdateCachedValues();
            }

            if (!lookup.TryGetValue(propertyName, out var resultList))
            {
                result = default;
                return false;
            }

            foreach(var ov in resultList)
            {
                if(callingComponent.GetType() != callingComponent.GetType())
                    continue;

                bool applyFallback = string.IsNullOrEmpty(ov.ScreenConfigName);
                if (applyFallback != string.IsNullOrEmpty(screenConfigName))
                    continue;

                if (!applyFallback && ov.ScreenConfigName != screenConfigName)
                    continue;

                if (ov is OverridePrefab<T> rightType)
                {
                    result = rightType.LastCalculatedSize;
                    return true;
                }
            }

            result = default;
            return false;
        }

        internal void NotifyButtonClick(BetterButton betterButton)
        {
            onButtonClicked.Invoke(betterButton);
        }

        internal void NotifyToggleChanged(BetterToggle betterToggle, bool isOn)
        {
            onToggleChanged.Invoke(betterToggle, isOn);
        }
    }
}
