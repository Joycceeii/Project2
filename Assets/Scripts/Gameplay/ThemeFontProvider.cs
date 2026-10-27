using System;
using TMPro;
using UnityEngine;

namespace TheTasteReviver
{
    public static class ThemeFontProvider
    {
        private static Font cachedFont;
        private static TMP_FontAsset cachedTmpFont;
        private static TMP_FontAsset cachedStoryTmpFont;

        public static Font GetFont(int size = 16)
        {
            if (cachedFont != null)
            {
                return cachedFont;
            }

            cachedFont = Resources.Load<Font>("Fonts/ModernFarmhouse");
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

            Font modernFarmhouse = Resources.Load<Font>("Fonts/ModernFarmhouse");
            if (modernFarmhouse != null)
            {
                cachedTmpFont = TMP_FontAsset.CreateFontAsset(modernFarmhouse);
                if (cachedTmpFont != null)
                {
                    cachedTmpFont.name = "Modern Farmhouse Runtime Font";
                    cachedTmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    return cachedTmpFont;
                }
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

        public static TMP_FontAsset GetStoryTmpFont(int size = 16)
        {
            if (cachedStoryTmpFont != null)
            {
                return cachedStoryTmpFont;
            }

            Font dancingScript = Resources.Load<Font>("Fonts/DancingScript-VariableFont_wght");
            if (dancingScript != null)
            {
                cachedStoryTmpFont = TMP_FontAsset.CreateFontAsset(dancingScript);
                if (cachedStoryTmpFont != null)
                {
                    cachedStoryTmpFont.name = "Dancing Script Story Runtime Font";
                    cachedStoryTmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                }
            }

            return cachedStoryTmpFont;
        }
    }
}
