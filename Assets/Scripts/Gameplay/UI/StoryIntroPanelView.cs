using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public sealed class StoryIntroPanelView : MonoBehaviour
    {
        [SerializeField] private Image backdrop;
        [SerializeField] private Image storyImage;
        [SerializeField] private TMP_Text storyText;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueButtonLabel;

        private Action continueAction;
        private Sprite runtimeSprite;
        private bool defaultTextPresentationCaptured;
        private Vector2 defaultTextSize;
        private Vector2 defaultTextPosition;
        private Quaternion defaultTextRotation;
        private float defaultTextFontSize;
        private Color defaultTextColor;
        private TextAlignmentOptions defaultTextAlignment;

        public TMP_Text EditorStoryText => storyText;

        public void EditorPreview(Texture2D texture, string text)
        {
            if (storyImage != null)
            {
                if (runtimeSprite != null)
                {
                    DestroyImmediate(runtimeSprite);
                }

                runtimeSprite = texture != null
                    ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f)
                    : null;
                storyImage.sprite = runtimeSprite;
                storyImage.gameObject.SetActive(texture != null);
            }

            if (storyText != null)
            {
                storyText.text = text ?? string.Empty;
            }

            gameObject.SetActive(true);
        }

        private void Awake()
        {
            BindButton();
        }

        public void Show(Texture2D texture, string text, string buttonText, Action onContinue)
        {
            CaptureDefaultTextPresentation();
            ResetTextPresentation();
            continueAction = onContinue;
            if (storyText != null)
            {
                storyText.text = text ?? string.Empty;
            }

            if (continueButtonLabel != null)
            {
                continueButtonLabel.text = buttonText ?? "Continue";
            }

            if (storyImage != null)
            {
                if (runtimeSprite != null)
                {
                    Destroy(runtimeSprite);
                }

                runtimeSprite = texture != null
                    ? Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f)
                    : null;
                storyImage.sprite = runtimeSprite;
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void ApplyTextPresentation(
            Vector2 size,
            Vector2 position,
            float rotation,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            if (storyText == null)
            {
                return;
            }

            RectTransform textRect = storyText.rectTransform;
            textRect.sizeDelta = size;
            textRect.anchoredPosition = position;
            textRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            storyText.fontSize = fontSize;
            storyText.color = color;
            storyText.alignment = alignment;
        }

        public void ResetTextPresentation()
        {
            if (storyText == null || !defaultTextPresentationCaptured)
            {
                return;
            }

            RectTransform textRect = storyText.rectTransform;
            textRect.sizeDelta = defaultTextSize;
            textRect.anchoredPosition = defaultTextPosition;
            textRect.localRotation = defaultTextRotation;
            storyText.fontSize = defaultTextFontSize;
            storyText.color = defaultTextColor;
            storyText.alignment = defaultTextAlignment;
        }

        public void Hide()
        {
            continueAction = null;
            gameObject.SetActive(false);
        }

        public void Configure(Image backdropImage, Image image, TMP_Text body, Button continueControl, TMP_Text buttonLabel)
        {
            backdrop = backdropImage;
            storyImage = image;
            storyText = body;
            continueButton = continueControl;
            continueButtonLabel = buttonLabel;
            CaptureDefaultTextPresentation();
            BindButton();
        }

        private void CaptureDefaultTextPresentation()
        {
            if (defaultTextPresentationCaptured || storyText == null)
            {
                return;
            }

            RectTransform textRect = storyText.rectTransform;
            defaultTextSize = textRect.sizeDelta;
            defaultTextPosition = textRect.anchoredPosition;
            defaultTextRotation = textRect.localRotation;
            defaultTextFontSize = storyText.fontSize;
            defaultTextColor = storyText.color;
            defaultTextAlignment = storyText.alignment;
            defaultTextPresentationCaptured = true;
        }

        private void BindButton()
        {
            if (continueButton == null)
            {
                return;
            }

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => continueAction?.Invoke());
        }
    }
}
