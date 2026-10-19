using UnityEngine;

namespace Emberlight
{
    /// <summary>共享涂鸦图集；格子顺序为自上而下、自左而右。</summary>
    public static class EmberCardIcons
    {
        static readonly Sprite[] icons = new Sprite[24];
        static Texture2D atlas;
        static bool reportedSlice;
        // .meta 保存切片；命名为 <atlas>_<index>，自左上起行主序，index == cell。
        const string Stem = "doodle-icons";

        public static Sprite ForOffer(int id)
        {
            int cell;
            switch (id)
            {
                case RunProgress.WeaponBasic: cell = 0; break;
                case RunProgress.WeaponOrbit: cell = 1; break;
                case RunProgress.WeaponTrail: cell = 2; break;
                case RunProgress.WeaponPierce: cell = 3; break;
                case RunProgress.WeaponBoom: cell = 4; break;
                case RunProgress.WeaponMeteor: cell = 5; break;
                case RunProgress.StatShield: cell = 6; break;
                case RunProgress.StatLuck: cell = 7; break;
                case RunProgress.StatAtk: cell = 8; break;
                case RunProgress.StatAs: cell = 22; break;
                case RunProgress.StatAmp: cell = 9; break;
                case RunProgress.MetaBasic: cell = 10; break;
                case RunProgress.MetaOrbit: cell = 11; break;
                case RunProgress.MetaTrail: cell = 12; break;
                case RunProgress.MetaPierce: cell = 13; break;
                case RunProgress.MetaBoom: cell = 14; break;
                case RunProgress.MetaMeteor: cell = 15; break;
                case RunProgress.ExclBasic: cell = 16; break;
                case RunProgress.ExclOrbit: cell = 17; break;
                case RunProgress.ExclTrail: cell = 18; break;
                case RunProgress.ExclPierce: cell = 19; break;
                case RunProgress.ExclBoom: cell = 20; break;
                case RunProgress.ExclMeteor: cell = 21; break;

                default: return EmberArt.Flame;
            }
            return Cell(cell);
        }

        static Sprite Cell(int cell)
        {
            if (icons[cell] != null) return icons[cell];
            // 子 Sprite 缺失时仍需用原纹理回落切图。
            if (atlas == null) atlas = Resources.Load<Texture2D>("Art/Cards/" + Stem);
            if (atlas != null)
            {
                var cut = Resources.Load<Sprite>("Art/Cards/" + Stem + "_" + cell);
                if (cut != null)
                {
                    icons[cell] = cut;
                    if (!reportedSlice) { reportedSlice = true; Debug.Log("[EmberCardIcons] using sliced atlas sprites (" + Stem + "_*)"); }
                    return icons[cell];
                }
            }
            // 回落：meta 切过之后没重新导入，或根本没有图集。
            if (atlas == null) return EmberArt.Flame;
            int column = cell % 4, row = cell / 4;
            // 内缩一点，避免双线性过滤采到相邻格子。
            float x = column * atlas.width / 4f + 1;
            float y = (5 - row) * atlas.height / 6f + 1;
            icons[cell] = Sprite.Create(atlas,
                new Rect(x, y, atlas.width / 4f - 2, atlas.height / 6f - 2),
                Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            icons[cell].name = "Doodle cell " + cell;
            return icons[cell];
        }
    }
}
