using UnityEngine;

namespace Emberlight
{
    /// <summary>Shared doodle atlas. Cell order is top-to-bottom, left-to-right.</summary>
    public static class EmberCardIcons
    {
        static readonly Sprite[] icons = new Sprite[24];
        static Texture2D atlas;

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

        public static Sprite Healing { get { return Cell(23); } }

        static Sprite Cell(int cell)
        {
            if (icons[cell] != null) return icons[cell];
            if (atlas == null) atlas = Resources.Load<Texture2D>("Art/Cards/doodle-icons");
            if (atlas == null) return EmberArt.Flame;
            int column = cell % 4, row = cell / 4;
            // Inset avoids sampling neighbouring cells with bilinear filtering.
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
