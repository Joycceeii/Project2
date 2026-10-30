using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public sealed class IngredientTraitsPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject backdrop;
        [SerializeField] private GameObject expandedPanel;
        [SerializeField] private TMP_Text traitLabel;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button backdropButton;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;

        public bool IsExpanded { get; private set; }
        public TMP_Text TraitLabel => traitLabel;
        public Button ToggleButton => toggleButton;

        private void Awake()
        {
            BindButtons();
            SetExpanded(false);
        }

        public void SetContent(string text, bool visible, bool expandInitially = false)
        {
            if (traitLabel != null)
            {
                traitLabel.text = text ?? string.Empty;
            }

            if (toggleButton != null)
            {
                toggleButton.gameObject.SetActive(visible);
            }

            SetExpanded(visible && expandInitially);
        }

        public void Expand()
        {
            SetExpanded(true);
        }

        public void Collapse()
        {
            SetExpanded(false);
        }

        public void SetExpanded(bool expanded)
        {
            IsExpanded = expanded && traitLabel != null && !string.IsNullOrWhiteSpace(traitLabel.text);
            if (IsExpanded)
            {
                // Raise the prefab root, not just its children, above the ratio panel.
                transform.SetAsLastSibling();
            }
            if (backdrop != null)
            {
                backdrop.SetActive(IsExpanded);
                if (IsExpanded)
                {
                    backdrop.transform.SetAsLastSibling();
                }
            }

            if (expandedPanel != null)
            {
                expandedPanel.SetActive(IsExpanded);
                if (IsExpanded)
                {
                    expandedPanel.transform.SetAsLastSibling();
                }
            }

            if (toggleButton != null)
            {
                toggleButton.gameObject.SetActive(!IsExpanded && traitLabel != null && !string.IsNullOrWhiteSpace(traitLabel.text));
            }

            if (IsExpanded)
            {
                RefreshScrollContent();
            }
        }

        public void Configure(
            GameObject backdropObject,
            GameObject panel,
            TMP_Text label,
            Button toggle,
            Button backdropCloseButton,
            ScrollRect scrolling,
            RectTransform scrollingViewport)
        {
            backdrop = backdropObject;
            expandedPanel = panel;
            traitLabel = label;
            toggleButton = toggle;
            backdropButton = backdropCloseButton;
            scrollRect = scrolling;
            viewport = scrollingViewport;
            BindButtons();
        }

        private void BindButtons()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveAllListeners();
                toggleButton.onClick.AddListener(Expand);
            }

            if (backdropButton != null)
            {
                backdropButton.onClick.RemoveAllListeners();
                backdropButton.onClick.AddListener(Collapse);
            }
        }

        private void RefreshScrollContent()
        {
            if (traitLabel == null || viewport == null)
            {
                return;
            }

            RectTransform content = traitLabel.rectTransform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float height = Mathf.Max(viewport.rect.height, traitLabel.preferredHeight + 12f);
            content.sizeDelta = new Vector2(0f, height);
            content.anchoredPosition = Vector2.zero;
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }
    }
}
