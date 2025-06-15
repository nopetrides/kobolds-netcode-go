using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.GameplayObjects.AnimationCallbacks;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.GameplayObjects.Character
{
    /// <summary>
    /// Responsible for storing of all of the pieces of a character, and swapping the pieces out en masse when asked to.
    /// </summary>
    public class CharacterSwap : MonoBehaviour
    {
        [System.Serializable]
        public class CharacterModelSet
        {
            public GameObject ears;
            public GameObject head;
            public GameObject mouth;
            public GameObject hair;
            public GameObject eyes;
            public GameObject torso;
            public GameObject gearRightHand;
            public GameObject gearLeftHand;
            public GameObject handRight;
            public GameObject handLeft;
            public GameObject shoulderRight;
            public GameObject shoulderLeft;
            public GameObject handSocket;
            public AnimatorTriggeredSpecialFX specialFx;
            public AnimatorOverrideController animatorOverrides; // references a separate stand-alone object in the project
            private List<Renderer> _mCachedRenderers;

            public void SetFullActive(bool isActive)
            {
                ears.SetActive(isActive);
                head.SetActive(isActive);
                mouth.SetActive(isActive);
                hair.SetActive(isActive);
                eyes.SetActive(isActive);
                torso.SetActive(isActive);
                gearLeftHand.SetActive(isActive);
                gearRightHand.SetActive(isActive);
                handRight.SetActive(isActive);
                handLeft.SetActive(isActive);
                shoulderRight.SetActive(isActive);
                shoulderLeft.SetActive(isActive);
            }

            public List<Renderer> GetAllBodyParts()
            {
                if (_mCachedRenderers == null)
                {
                    _mCachedRenderers = new List<Renderer>();
                    AddRenderer(ref _mCachedRenderers, ears);
                    AddRenderer(ref _mCachedRenderers, head);
                    AddRenderer(ref _mCachedRenderers, mouth);
                    AddRenderer(ref _mCachedRenderers, hair);
                    AddRenderer(ref _mCachedRenderers, torso);
                    AddRenderer(ref _mCachedRenderers, gearRightHand);
                    AddRenderer(ref _mCachedRenderers, gearLeftHand);
                    AddRenderer(ref _mCachedRenderers, handRight);
                    AddRenderer(ref _mCachedRenderers, handLeft);
                    AddRenderer(ref _mCachedRenderers, shoulderRight);
                    AddRenderer(ref _mCachedRenderers, shoulderLeft);
                }
                return _mCachedRenderers;
            }

            private void AddRenderer(ref List<Renderer> rendererList, GameObject bodypartGo)
            {
                if (!bodypartGo) { return; }
                var bodyPartRenderer = bodypartGo.GetComponent<Renderer>();
                if (!bodyPartRenderer) { return; }
                rendererList.Add(bodyPartRenderer);
            }

        }

        [SerializeField]
        CharacterModelSet m_CharacterModel;

        public CharacterModelSet CharacterModel => m_CharacterModel;

        /// <summary>
        /// Reference to our shared-characters' animator.
        /// Can be null, but if so, animator overrides are not supported!
        /// </summary>
        [SerializeField]
        private Animator m_Animator;

        /// <summary>
        /// Reference to the original controller in our Animator.
        /// We switch back to this whenever we don't have an Override.
        /// </summary>
        private RuntimeAnimatorController _mOriginalController;

        [SerializeField]
        [Tooltip("Special Material we plug in when the local player is \"stealthy\"")]
        private Material m_StealthySelfMaterial;

        [SerializeField]
        [Tooltip("Special Material we plug in when another player is \"stealthy\"")]
        private Material m_StealthyOtherMaterial;

        public enum SpecialMaterialMode
        {
            None,
            StealthySelf,
            StealthyOther,
        }

        /// <summary>
        /// When we swap all our Materials out for a special material,
        /// we keep the old references here, so we can swap them back.
        /// </summary>
        private Dictionary<Renderer, Material> _mOriginalMaterials = new Dictionary<Renderer, Material>();

        ClientCharacter _mClientCharacter;

        void Awake()
        {
            _mClientCharacter = GetComponentInParent<ClientCharacter>();
            m_Animator = _mClientCharacter.OurAnimator;
            _mOriginalController = m_Animator.runtimeAnimatorController;
        }

        private void OnDisable()
        {
            // It's important that the original Materials that we pulled out of the renderers are put back.
            // Otherwise nothing will Destroy() them and they will leak! (Alternatively we could manually
            // Destroy() these in our OnDestroy(), but in this case it makes more sense just to put them back.)
            ClearOverrideMaterial();
        }

        /// <summary>
        /// Swap the visuals of the character to the index passed in.
        /// </summary>
        /// <param name="specialMaterialMode">Special Material to apply to all body parts</param>
        public void SwapToModel(SpecialMaterialMode specialMaterialMode = SpecialMaterialMode.None)
        {
            ClearOverrideMaterial();

            if (m_CharacterModel.specialFx)
            {
                m_CharacterModel.specialFx.enabled = true;
            }

            if (m_Animator)
            {
                // plug in the correct animator override... or plug the original non - overridden version back in!
                if (m_CharacterModel.animatorOverrides)
                {
                    m_Animator.runtimeAnimatorController = m_CharacterModel.animatorOverrides;
                }
                else
                {
                    m_Animator.runtimeAnimatorController = _mOriginalController;
                }
            }

            // lastly, now that we're all assembled, apply any override material.
            switch (specialMaterialMode)
            {
                case SpecialMaterialMode.StealthySelf:
                    SetOverrideMaterial(m_StealthySelfMaterial);
                    break;
                case SpecialMaterialMode.StealthyOther:
                    SetOverrideMaterial(m_StealthyOtherMaterial);
                    break;
            }
        }

        private void ClearOverrideMaterial()
        {
            foreach (var entry in _mOriginalMaterials)
            {
                if (entry.Key)
                {
                    entry.Key.material = entry.Value;
                }
            }
            _mOriginalMaterials.Clear();
        }

        private void SetOverrideMaterial(Material overrideMaterial)
        {
            ClearOverrideMaterial(); // just sanity-checking; this should already have been called!
            foreach (var bodyPart in m_CharacterModel.GetAllBodyParts())
            {
                if (bodyPart)
                {
                    _mOriginalMaterials[bodyPart] = bodyPart.material;
                    bodyPart.material = overrideMaterial;
                }
            }
        }
    }
}
