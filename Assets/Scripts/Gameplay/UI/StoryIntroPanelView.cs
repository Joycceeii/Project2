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

        private void Awake()
        {
            BindButton();
        }

        public void Show(Texture2D texture, string text, string buttonText, Action onContinue)
        {
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
            BindButton();
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
