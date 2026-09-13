using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Emberlight
{
    public static class EmberFonts
    {
        // Encoding-safe glyph list (unicode escapes).
        public const string GlyphSet = "\u0020\u0025\u0028\u0029\u002b\u002d\u002f\u0030\u0031\u0032\u0033\u0034\u0035\u0036\u0037\u0038\u0039\u003a\u0046\u004c\u005b\u005d\u0061\u0064\u0065\u0066\u0067\u0069\u006c\u006e\u006f\u0074\u00b7\u2026\u2192\u3001\u3002\u3010\u3011\u4e00\u4e0a\u4e0b\u4e4b\u4eae\u4ee5\u4efb\u4f10\u4f11\u4f24\u4f59\u4f5c\u505c\u5149\u5165\u518d\u51b2\u51fb\u524d\u52a0\u52a8\u5347\u534e\u536b\u5373\u53cc\u53d1\u53d6\u5408\u5468\u547d\u56de\u56f4\u5730\u57df\u589e\u590d\u5916\u591c\u5927\u59cb\u5b58\u5b88\u5bb3\u5c04\u5c4f\u5e55\u5e76\u5e87\u5ea6\u5f00\u5f20\u5f3a\u5f53\u5f55\u5f71\u5f84\u6012\u6062\u606f\u610f\u6210\u6218\u6240\u6269\u62a4\u62d6\u62e9\u62fe\u6301\u6309\u63d0\u653b\u653e\u6563\u6570\u6597\u6625\u6653\u6682\u6696\u6697\u6700\u671f\u6740\u679a\u6b21\u6b47\u6b65\u6bcf\u6d6a\u706b\u706f\u70b9\u70bd\u70e7\u70ec\u70ed\u7130\u7184\u71c3\u7206\u72c2\u730e\u73af\u7403\u751f\u7554\u7559\u75be\u767d\u76c8\u77f3\u7834\u795d\u798f\u79cd\u79d2\u79fb\u7a0d\u7acb\u7b49\u7ea7\u7eaa\u7ed5\u7ed9\u7ee7\u7eed\u7efd\u8005\u800c\u81f3\u8272\u8303\u884c\u8865\u8b66\u8ba1\u8d74\u8def\u8e0f\u8f7b\u8ffd\u9000\u9009\u901f\u9020\u91ca\u91cf\u91d1\u94bb\u94dc\u94f6\u950b\u952e\u957f\u9650\u9752\u9879\u9884\u9897\u989d\u9ad8\u9ec4\uff08\uff09\uff0c\uff1a";

        public static TMP_FontAsset CreateChinese()
        {
            var source = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if (source == null)
            {
                Debug.LogError("Missing font Resources/Fonts/NotoSansCJKsc-Regular");
                return null;
            }
            var font = TMP_FontAsset.CreateFontAsset(
                source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            if (font == null)
            {
                Debug.LogError("TMP_FontAsset.CreateFontAsset failed");
                return null;
            }
            font.name = "NotoSansCJKsc-Regular Dynamic";
            string missing;
            font.TryAddCharacters(GlyphSet, out missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning("TMP missing glyphs: " + missing.Length);
            return font;
        }
    }
}
