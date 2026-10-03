using System;
using UnityEngine;
using UnityEngine.UI;

namespace TheTasteReviver
{
    public enum PanelBackgroundKind
    {
        Default,
        CrayonOrange,
        ColoredPencilRed,
        Graphite,
        DryBrushBrown,
        MilitaryGreen,
        OliveGreen,
        SlateBlue,
        OchreYellow,
        PowderBlue,
        PaleYellow,
        PaleCream
    }

    public static class PanelBackgroundStyle
    {
        private const string DefaultResourcePath = "UI/PanelGraphite";
        private static readonly Sprite[] cachedSprites = new Sprite[12];

        public static void Apply(Image image, float alpha = 1f)
        {
            Apply(image, PanelBackgroundKind.Default, alpha);
        }

        public static void Apply(Image image, PanelBackgroundKind kind, float alpha = 1f)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetSprite(kind);
            if (sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
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

            if (string.Equals(name, "Ingredient Traits Expanded Panel", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Ingredient Traits Panel", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return name.IndexOf("Panel", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Background", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Sprite GetSprite(PanelBackgroundKind kind)
        {
            int index = (int)kind;
            if (cachedSprites[index] == null)
            {
                cachedSprites[index] = Resources.Load<Sprite>(GetResourcePath(kind));
            }

            return cachedSprites[index];
        }

        private static string GetResourcePath(PanelBackgroundKind kind)
        {
            switch (kind)
            {
                case PanelBackgroundKind.CrayonOrange:
                    return "UI/PanelCrayonOrange";
                case PanelBackgroundKind.ColoredPencilRed:
                    return "UI/PanelColoredPencilRed";
                case PanelBackgroundKind.Graphite:
                    return "UI/PanelGraphite";
                case PanelBackgroundKind.DryBrushBrown:
                    return "UI/PanelDryBrushBrown";
                case PanelBackgroundKind.MilitaryGreen:
                    return "UI/PanelMilitaryGreen";
                case PanelBackgroundKind.OliveGreen:
                    return "UI/PanelOliveGreen";
                case PanelBackgroundKind.SlateBlue:
                    return "UI/PanelSlateBlue";
                case PanelBackgroundKind.OchreYellow:
                    return "UI/PanelOchreYellow";
                case PanelBackgroundKind.PowderBlue:
                    return "UI/PanelPowderBlue";
                case PanelBackgroundKind.PaleYellow:
                    return "UI/PanelPaleYellow";
                case PanelBackgroundKind.PaleCream:
                    return "UI/PanelPaleCream";
                default:
                    return DefaultResourcePath;
            }
        }
    }
}
