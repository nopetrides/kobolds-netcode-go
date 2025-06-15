using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.BossRoom.Utils.Editor
{
    public class NetworkOverlay : MonoBehaviour
    {
        public static NetworkOverlay Instance { get; private set; }

        [SerializeField]
        GameObject m_DebugCanvasPrefab;

        Transform _mVerticalLayoutTransform;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }

        public void AddTextToUI(string gameObjectName, string defaultText, out TextMeshProUGUI textComponent)
        {
            var rootGo = new GameObject(gameObjectName);
            textComponent = rootGo.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 28;
            textComponent.text = defaultText;
            textComponent.horizontalAlignment = HorizontalAlignmentOptions.Left;
            textComponent.verticalAlignment = VerticalAlignmentOptions.Middle;
            textComponent.raycastTarget = false;
            textComponent.autoSizeTextContainer = true;

            var rectTransform = rootGo.GetComponent<RectTransform>();
            AddToUI(rectTransform);
        }

        public void AddToUI(RectTransform displayTransform)
        {
            if (_mVerticalLayoutTransform == null)
            {
                CreateDebugCanvas();
            }

            displayTransform.sizeDelta = new Vector2(100f, 24f);
            displayTransform.SetParent(_mVerticalLayoutTransform);
            displayTransform.SetAsFirstSibling();
            displayTransform.localScale = Vector3.one;
        }

        void CreateDebugCanvas()
        {
            var canvas = Instantiate(m_DebugCanvasPrefab, transform);
            _mVerticalLayoutTransform = canvas.GetComponentInChildren<VerticalLayoutGroup>().transform;
        }
    }
}
