using System;
using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public static class PanelBackgroundStyle
    {
        private const string ResourcePath = "UI/PanelBackground";
        private static Sprite cachedSprite;

        public static void Apply(Image image, float alpha = 1f)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetSprite();
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        }

        public static void ApplyToNamedPanels(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image == null || !ShouldStyle(image.gameObject.name))
                {
                    continue;
                }

                Apply(image);
            }
        }

        private static bool ShouldStyle(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            return name.IndexOf("Panel", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Background", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Sprite GetSprite()
        {
            if (cachedSprite == null)
            {
                cachedSprite = Resources.Load<Sprite>(ResourcePath);
            }

            return cachedSprite;
        }
    }
}
