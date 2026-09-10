using System;
using UnityEngine;

namespace TheTasteReviver
{
    public static class ThemeFontProvider
    {
        private static Font cachedFont;

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
    }
}
