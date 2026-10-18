using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace TheTasteReviver
{
    public sealed class IngredientTooltipView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Vector2 cursorOffset = new Vector2(24f, -22f);

        private RectTransform tooltipRect;
        private UnityEngine.Object owner;

        private void Awake()
        {
            tooltipRect = transform as RectTransform;
        }

        private void LateUpdate()
        {
            if (gameObject.activeSelf)
            {
                PositionAtCursor();
            }
        }

        public void Show(IReadOnlyList<IngredientData> ingredients, UnityEngine.Object requestedOwner)
        {
            List<IngredientData> valid = ingredients != null
                ? ingredients.Where(item => item != null).Distinct().ToList()
                : new List<IngredientData>();
            if (valid.Count == 0)
            {
                return;
            }

            owner = requestedOwner;
            if (label != null)
            {
                label.text = BuildText(valid);
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            PositionAtCursor();
        }

        public void Hide(UnityEngine.Object requestedOwner = null)
        {
            if (requestedOwner != null && owner != null && requestedOwner != owner)
            {
                return;
            }

            owner = null;
            gameObject.SetActive(false);
        }

        public void Configure(TMP_Text tooltipLabel, Vector2 offset)
        {
            label = tooltipLabel;
            cursorOffset = offset;
            tooltipRect = transform as RectTransform;
        }

        private void PositionAtCursor()
        {
            if (tooltipRect == null)
            {
                tooltipRect = transform as RectTransform;
            }

            RectTransform parentRect = tooltipRect != null ? tooltipRect.parent as RectTransform : null;
            if (tooltipRect == null || parentRect == null)
            {
                return;
            }

            Camera eventCamera = null;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                eventCamera = canvas.worldCamera;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, eventCamera, out Vector2 localPoint))
            {
                return;
            }

            Vector2 position = localPoint + cursorOffset;
            Rect bounds = parentRect.rect;
            Vector2 size = tooltipRect.rect.size;
            position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax - size.x);
            position.y = Mathf.Clamp(position.y, bounds.yMin + size.y, bounds.yMax);
            tooltipRect.anchoredPosition = position;
        }

        private static string BuildText(IReadOnlyList<IngredientData> ingredients)
        {
            if (ingredients.Count > 1)
            {
                return "Prepared mixture\n" + string.Join(" + ", ingredients.Select(BuildName));
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < ingredients.Count; i++)
            {
                IngredientData ingredient = ingredients[i];
                if (i > 0)
                {
                    builder.AppendLine().AppendLine();
                }

                builder.Append(BuildName(ingredient));
                if (!string.IsNullOrWhiteSpace(ingredient.aromaType))
                {
                    builder.AppendLine().Append("Aroma: ").Append(ingredient.aromaType);
                }

                if (!string.IsNullOrWhiteSpace(ingredient.initialDescription))
                {
                    builder.AppendLine().Append(ingredient.initialDescription);
                }
            }

            return builder.ToString();
        }

        private static string BuildName(IngredientData ingredient)
        {
            string displayName = ingredient != null ? ingredient.DisplayName : string.Empty;
            return "<b>" + displayName + "</b>";
        }
    }
}
