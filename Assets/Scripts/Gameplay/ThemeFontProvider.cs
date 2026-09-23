using System;
using TMPro;
using UnityEngine;

namespace TheTasteReviver
{
    public static class ThemeFontProvider
    {
        private static Font cachedFont;
        private static TMP_FontAsset cachedTmpFont;

        public static Font GetFont(int size = 16)
        {
            if (cachedFont != null)
            {
                return cachedFont;
            }

            try
            {
                cachedFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Palatino Linotype", "Book Antiqua", "Georgia", "KaiTi", "STKaiti", "Microsoft YaHei", "Arial" },
                    size);
            }
            catch (ArgumentException)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return cachedFont;
        }

        public static TMP_FontAsset GetTmpFont(int size = 16)
        {
            if (cachedTmpFont != null)
            {
                return cachedTmpFont;
            }

            cachedTmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (cachedTmpFont != null)
            {
                return cachedTmpFont;
            }

            Font source = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (source != null)
            {
                cachedTmpFont = TMP_FontAsset.CreateFontAsset(source);
                if (cachedTmpFont != null)
                {
                    cachedTmpFont.name = "Taste Reviver Runtime Font";
                    cachedTmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    return cachedTmpFont;
                }
            }

            return cachedTmpFont;
        }
    }
}
