using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kobold.UI.Theming
{
    [CreateAssetMenu(fileName = "UITheme", menuName = "Kobold/UI/Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Theme Identity")]
        public string themeName = "Default";
        public string themeDescription;
        public Sprite themePreviewImage;
        
        [Header("Style Sheets")]
        public StyleSheet mainStyleSheet;
        public StyleSheet[] additionalStyleSheets;
        
        [Header("Colors")]
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.gray;
        public Color accentColor = Color.cyan;
        public Color backgroundColor = Color.black;
        public Color textColor = Color.white;
        public Color errorColor = Color.red;
        public Color successColor = Color.green;
        public Color warningColor = Color.yellow;
        
        [Header("Fonts")]
        public Font primaryFont;
        public Font secondaryFont;
        public int baseFontSize = 14;
        
        [Header("Icons & Images")]
        public Texture2D[] themeIcons;
        public Sprite[] themeSprites;
        
        [Header("Audio")]
        public AudioClip uiClickSound;
        public AudioClip uiHoverSound;
        public AudioClip uiErrorSound;
        
        // CSS Custom Properties that will be injected
        public Dictionary<string, string> GetCssVariables()
        {
            return new Dictionary<string, string>
            {
                { "--primary-color", ColorToHex(primaryColor) },
                { "--secondary-color", ColorToHex(secondaryColor) },
                { "--accent-color", ColorToHex(accentColor) },
                { "--background-color", ColorToHex(backgroundColor) },
                { "--text-color", ColorToHex(textColor) },
                { "--error-color", ColorToHex(errorColor) },
                { "--success-color", ColorToHex(successColor) },
                { "--warning-color", ColorToHex(warningColor) },
                { "--base-font-size", $"{baseFontSize}px" }
            };
        }
        
        private string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
        }
    }
}