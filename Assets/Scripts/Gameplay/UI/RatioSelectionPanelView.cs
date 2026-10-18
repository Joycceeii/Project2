using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public sealed class RatioSelectionPanelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button[] optionButtons = new Button[4];

        private Action<RatioLevel> pendingSelection;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            // The prefab is authored closed. Do not close it here: Awake may run
            // during the first Show call when an inactive prefab is activated.
        }

        public void Show(IngredientData ingredient, IReadOnlyList<RatioLevel> options, Action<RatioLevel> onSelected)
        {
            if (options == null || options.Count == 0)
            {
                return;
            }

            pendingSelection = onSelected;
            if (titleLabel != null)
            {
                titleLabel.text = "Choose ratio for " + (ingredient != null ? ingredient.DisplayName : "Ingredient");
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                Button button = optionButtons[i];
                if (button == null)
                {
                    continue;
                }

                bool hasOption = i < options.Count;
                button.gameObject.SetActive(hasOption);
                button.onClick.RemoveAllListeners();
                if (!hasOption)
                {
                    continue;
                }

                RatioLevel selected = options[i];
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = UIManager.GetRatioDisplayName(selected);
                }

                button.onClick.AddListener(() => Select(selected));
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            pendingSelection = null;
            gameObject.SetActive(false);
        }

        public void Configure(TMP_Text title, Button[] buttons)
        {
            titleLabel = title;
            optionButtons = buttons ?? Array.Empty<Button>();
        }

        private void Select(RatioLevel ratio)
        {
            Action<RatioLevel> callback = pendingSelection;
            pendingSelection = null;
            gameObject.SetActive(false);
            callback?.Invoke(ratio);
        }
    }
}
