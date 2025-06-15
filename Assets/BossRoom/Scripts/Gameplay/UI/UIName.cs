using System;
using TMPro;
using Unity.BossRoom.Utils;
using Unity.Netcode;
using UnityEngine;

namespace Unity.BossRoom.Gameplay.UI
{
    /// <summary>
    /// UI object that visually represents an object's name. Visuals are updated when NetworkVariable is modified.
    /// </summary>
    public class UIName : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI m_UINameText;

        NetworkVariable<FixedPlayerName> _mNetworkedNameTag;

        public void Initialize(NetworkVariable<FixedPlayerName> networkedName)
        {
            _mNetworkedNameTag = networkedName;

            m_UINameText.text = networkedName.Value.ToString();
            networkedName.OnValueChanged += NameUpdated;
        }

        void NameUpdated(FixedPlayerName previousValue, FixedPlayerName newValue)
        {
            m_UINameText.text = newValue.ToString();
        }

        void OnDestroy()
        {
            _mNetworkedNameTag.OnValueChanged -= NameUpdated;
        }
    }
}
