using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace MatchThree.Core
{
    public static class UIHelper
    {
        private static Font _font;

        public static Font DefaultFont
        {
            get
            {
                if (_font == null)
                    _font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                return _font;
            }
        }

        public static Canvas CreateCanvas(string name = "Canvas")
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static Text CreateText(Transform parent, string content, int fontSize, Color colour, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = colour;
            text.alignment = anchor;
            text.font = DefaultFont;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;

            return text;
        }

        public static Button CreateButton(Transform parent, string label, int fontSize, UnityAction onClick)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.35f);

            var button = go.AddComponent<Button>();
            var colours = button.colors;
            colours.highlightedColor = new Color(0.35f, 0.35f, 0.5f);
            colours.pressedColor = new Color(0.15f, 0.15f, 0.25f);
            button.colors = colours;
            button.onClick.AddListener(onClick);

            // Label
            var text = CreateText(go.transform, label, fontSize, Color.white);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return button;
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;
        }
    }
}
