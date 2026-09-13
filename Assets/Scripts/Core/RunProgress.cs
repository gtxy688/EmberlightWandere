using System;
using System.Collections.Generic;

namespace Emberlight
{
    public sealed class RunProgress
    {
        public int Score { get; private set; }
        public int Pending { get; private set; }

        public int ExtraShots { get; private set; }
        public float AttackSpeedBonus { get; private set; }
        public float DamageBonus { get; private set; }
        public int Orbits { get; private set; }
        public float TrailPower { get; private set; }
        public float PickupBonus { get; private set; }
        public float MoveSpeedBonus { get; private set; }
        public float BonusMaxHealth { get; private set; }
        public float WavePower { get; private set; }

        /// <summary>Ember pickup: score only (no XP). PickupBonus may raise efficiency.</summary>
        public void AddScore(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            int gained = amount;
            if (PickupBonus > 0f)
            {
                // Mild score efficiency from Id5; radius is applied in combat.
                float mul = 1f + PickupBonus * 0.5f;
                gained = (int)(amount * mul + 0.5f);
                if (gained < amount) gained = amount;
            }
            Score += gained;
        }

        /// <summary>Wave-clear opens one forced 3-choice card (Pending temp=1).</summary>
        public void OfferChoices()
        {
            Pending = 1;
        }

        public EmberOffer[] Choices(Random random, int wave)
        {
            var pool = new List<int>();
            for (int i = 0; i < 9; i++) pool.Add(i);
            var result = new List<EmberOffer>();
            while (pool.Count > 0 && result.Count < 3)
            {
                int p = random.Next(pool.Count);
                int id = pool[p];
                pool.RemoveAt(p);
                result.Add(new EmberOffer(id, EmberRarityUtil.Roll(random, wave)));
            }
            if (result.Count == 0)
                result.Add(new EmberOffer(9, EmberRarityUtil.Roll(random, wave)));
            return result.ToArray();
        }

        public bool Choose(EmberOffer offer)
        {
            if (Pending == 0 || offer.Id < 0 || offer.Id > 9) return false;
            if (offer.Id == 9)
            {
                Pending--;
                return true; // heal is applied by EmberGame
            }

            float mag = EmberRarityUtil.Magnitude(offer.Rarity);
            int count = EmberRarityUtil.CountBonus(offer.Rarity);
            switch (offer.Id)
            {
                case 0: ExtraShots += count; break;
                case 1: AttackSpeedBonus += mag; break;
                case 2: DamageBonus += mag; break;
                case 3: Orbits += count; break;
                case 4: TrailPower += mag; break;
                case 5: PickupBonus += mag * 2f; break;
                case 6: MoveSpeedBonus += mag; break;
                case 7: BonusMaxHealth += 100f * mag; break;
                case 8: WavePower += mag; break;
                default: return false;
            }
            Pending--;
            return true;
        }

        public float HealAmount(EmberRarity rarity)
        {
            return 20f + 10f * (int)rarity; // 20 / 30 / 40 / 50
        }
    }
}
