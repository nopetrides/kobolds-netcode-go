#if !UNITY_6000_0_OR_NEWER
using Linework.Common.Attributes;
#endif
using System;
using System.Collections.Generic;
using Linework.Common.Utils;
using UnityEditor;
using UnityEngine;

namespace Linework.EdgeDetection
{
    [CreateAssetMenu(fileName = "Edge Detection Settings", menuName = "Linework/Edge Detection Settings")]
    [Icon("Packages/dev.ameye.linework/Editor/Common/Icons/d_EdgeDetection.png")]
    public class EdgeDetectionSettings : ScriptableObject
    {
        internal Action OnSettingsChanged;
        
        [SerializeField] private InjectionPoint injectionPoint = InjectionPoint.AfterRenderingPostProcessing;
        [SerializeField] private bool showInSceneView = true;
        [SerializeField] private DebugView debugView;
        public bool debugSectionsRaw;

        public DiscontinuityInput discontinuityInput = DiscontinuityInput.Depth | DiscontinuityInput.Normals | DiscontinuityInput.Luminance | DiscontinuityInput.Sections;
        [Range(0.0f, 1.0f)] public float depthSensitivity = 1.0f;
        [Range(0.0f, 1.0f)] public float depthDistanceModulation = 0.4f;
        [Range(0.0f, 1.0f)] public float grazingAngleMaskPower = 0.2f;
        [Range(1.0f, 30.0f)] public float grazingAngleMaskHardness = 1.0f;
        [Range(0.0f, 1.0f)] public float normalSensitivity = 0.4f;
        [Range(0.0f, 1.0f)] public float luminanceSensitivity = 0.3f;
        public bool objectId = true;
        public bool particles = false;
        public SectionMapInput sectionMapInput = SectionMapInput.None;
        public Texture2D sectionTexture;
        public UVSet sectionTextureUvSet;
        public Channel sectionTextureChannel;
        public Channel vertexColorChannel;

        // Outline.
        public Kernel kernel = Kernel.RobertsCross;
        [Range(0, 15)] public int outlineThickness = 3;
        public bool scaleWithResolution;
        public Resolution referenceResolution;
        public float customResolution;
        [ColorUsage(true, true)] public Color backgroundColor = Color.clear;
        [ColorUsage(true, true)] public Color outlineColor = Color.black;
        public bool overrideColorInShadow;
        [ColorUsage(true, true)] public Color outlineColorShadow = Color.white;
        [ColorUsage(true, true)] public Color fillColor = Color.black;
        public bool fadeByDistance;
        [ColorUsage(true, true)] public Color distanceFadeColor = Color.clear;
        [Range(0.0f, 200.0f)] public float distanceFadeStart = 100.0f;
        [Range(0.1f, 20.0f)] public float distanceFadeDistance = 10.0f;
        public bool fadeByHeight;
        [ColorUsage(true, true)] public Color heightFadeColor = Color.clear;
        [Range(0.0f, 2.0f)] public float heightFadeStart = 1.0f;
        [Range(0.01f, 2.0f)] public float heightFadeDistance = 0.5f;
        public BlendingMode blendMode;
        
        // Section map.
        public SectionMapPrecision sectionMapPrecision = SectionMapPrecision._16bit;
        [Range(0, 256)] public int sectionMapClearValue = 1;
        public List<SectionPass> additionalSectionPasses = new();
#if UNITY_6000_0_OR_NEWER
        public RenderingLayerMask SectionRenderingLayer = RenderingLayerMask.defaultRenderingLayerMask;
#else
        [RenderingLayerMask]
        public uint SectionRenderingLayer = 1;
#endif
#if UNITY_6000_0_OR_NEWER
        public RenderingLayerMask SectionMaskRenderingLayer = 0;
#else
        [RenderingLayerMask]
        public uint SectionMaskRenderingLayer = 0;
#endif
        public MaskInfluence maskInfluence = MaskInfluence.Sections | MaskInfluence.Depth | MaskInfluence.Normals | MaskInfluence.Luminance;
        
        public InjectionPoint InjectionPoint => injectionPoint;
        public bool ShowInSceneView => showInSceneView;
        public DebugView DebugView => debugView;

        public bool showSectionMapSection;
        public bool showDiscontinuitySection;
        public bool showOutlineSection;

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;
            OnSettingsChanged?.Invoke();
#endif
        }

        private void OnDestroy()
        {
            OnSettingsChanged = null;
        }
        
#if UNITY_EDITOR
        private class OnDestroyProcessor: AssetModificationProcessor
        {
            private static readonly Type Type = typeof(EdgeDetectionSettings);
            private const string FileEnding = ".asset";

            public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions _)
            {
                if (!path.EndsWith(FileEnding))
                    return AssetDeleteResult.DidNotDelete;

                var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (assetType == null || assetType != Type && !assetType.IsSubclassOf(Type)) return AssetDeleteResult.DidNotDelete;
                var asset = AssetDatabase.LoadAssetAtPath<EdgeDetectionSettings>(path);
                asset.OnDestroy();

                return AssetDeleteResult.DidNotDelete;
            }
        }
#endif
    }
}