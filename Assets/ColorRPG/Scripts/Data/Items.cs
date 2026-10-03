// =====================================================================
//  Items.cs - echipament (arme, coifuri, armuri, inele) cu raritate
// =====================================================================
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ColorRPG
{
    public enum Slot { Weapon = 0, Helmet = 1, Armor = 2, Ring = 3 }

    [Serializable]
    public class Item
    {
        public int id;
        public int slot;       // (int)Slot
        public int rarity;     // 0..5
        public int tier;       // 0..3 (harta de unde a picat)
        public int set;        // (vechi) seturile au fost scoase - ramane 0
        public int cat;        // indexul din ItemCatalog + 1 (0 = item vechi, se converteste la incarcare)
        public int plus;       // nivel de forjare
        public float roll;     // 0.85..1.2 variatie de statistici
        public string name;
        public bool locked;    // nu se vinde cu "Sell all"

        public Slot S { get { return (Slot)slot; } }
        public int SetIndex { get { return set - 1; } }   // -1 = fara set
    }

    public static class ItemDB
    {
        public const int MaxPlus = 15;

        public static readonly string[] RarityNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
        public static readonly Color[] RarityCols =
        {
            new Color(0.82f, 0.84f, 0.9f), new Color(0.35f, 0.95f, 0.4f), new Color(0.3f, 0.62f, 1f),
            new Color(0.78f, 0.38f, 1f), new Color(1f, 0.72f, 0.12f), new Color(1f, 0.25f, 0.45f)
        };
        static readonly float[] RarityMul = { 1f, 1.5f, 2.3f, 3.5f, 5.5f, 9f };
        static readonly float[] TierMul = { 1f, 4f, 16f, 64f };
        public const int MaxTier = 3;

        public static readonly string[] SlotNames = { "Weapon", "Helmet", "Armor", "Ring" };
        static readonly string[] RarityWord = { "", "Fine", "Shiny", "Mystic", "Golden", "Divine" };

        // ---------------------------------------------------------------
        //  Tipuri de arme (fiecare arata si se simte altfel)
        // ---------------------------------------------------------------
        public enum WeaponKind { Sword, Axe, Hammer, Spear, Saber, Greatsword }
        public static readonly string[] WeaponKindNames = { "Sword", "Axe", "Hammer", "Spear", "Saber", "Greatsword" };
        //                                              Sword  Axe   Hammer Spear Saber Greatsword
        static readonly float[] KindDmg   = { 1.00f, 1.12f, 1.30f, 1.00f, 0.92f, 1.22f };
        static readonly float[] KindSpeed = { 1.00f, 0.92f, 0.80f, 0.95f, 1.18f, 0.85f };
        static readonly float[] KindRange = { 0.0f, 0.0f, 0.2f, 1.4f, 0.0f, 0.7f };

        /// <summary>Tipul armei dupa nume (merge si pentru itemele vechi: Blade = Greatsword, Cleaver = Axe).</summary>
        public static WeaponKind KindOf(Item it)
        {
            ItemCatalog.Def d;
            if (ItemCatalog.TryGet(it, out d) && d.slot == 0) return (WeaponKind)d.baseIdx;
            if (it == null || it.name == null) return WeaponKind.Sword;
            string n = it.name;
            if (n.Contains("Greatsword") || n.Contains("Blade")) return WeaponKind.Greatsword;
            if (n.Contains("Axe") || n.Contains("Cleaver")) return WeaponKind.Axe;
            if (n.Contains("Hammer")) return WeaponKind.Hammer;
            if (n.Contains("Spear")) return WeaponKind.Spear;
            if (n.Contains("Saber")) return WeaponKind.Saber;
            return WeaponKind.Sword;
        }
        public static float KindDamage(Item it) { return it == null ? 1f : KindDmg[(int)KindOf(it)]; }
        public static float KindAttackSpeed(Item it) { return it == null ? 1f : KindSpeed[(int)KindOf(it)]; }
        public static float KindReach(Item it) { return it == null ? 0f : KindRange[(int)KindOf(it)]; }
        public static string KindText(Item it)
        {
            int k = (int)KindOf(it);
            string t = WeaponKindNames[k];
            var parts = new System.Collections.Generic.List<string>();
            if (KindDmg[k] != 1f) parts.Add((KindDmg[k] > 1f ? "+" : "") + Mathf.RoundToInt((KindDmg[k] - 1f) * 100) + "% dmg");
            if (KindSpeed[k] != 1f) parts.Add((KindSpeed[k] > 1f ? "+" : "") + Mathf.RoundToInt((KindSpeed[k] - 1f) * 100) + "% speed");
            if (KindRange[k] > 0f) parts.Add("+" + KindRange[k].ToString("0.#") + "m reach");
            return parts.Count > 0 ? t + " (" + string.Join(", ", parts) + ")" : t + " (balanced)";
        }

        public static Color Col(int r) { return RarityCols[Mathf.Clamp(r, 0, RarityCols.Length - 1)]; }

        /// <summary>Culoare "curcubeu" animata pentru Mythic, altfel culoarea raritatii.</summary>
        public static Color AnimCol(int r)
        {
            if (r < 5) return Col(r);
            return Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * 0.25f, 1f), 0.6f, 1f);
        }

        // ---------------------------------------------------------------
        //  Statistici
        // ---------------------------------------------------------------
        public static float Base(Item it)
        {
            float b = it.S == Slot.Weapon ? 5f : it.S == Slot.Helmet ? 14f : it.S == Slot.Armor ? 26f : 1f;
            ItemCatalog.Def d;
            float cm = ItemCatalog.TryGet(it, out d) ? d.mul : 1f;
            return b * TierMul[Mathf.Clamp(it.tier, 0, MaxTier)] * RarityMul[Mathf.Clamp(it.rarity, 0, 5)] * it.roll * cm;
        }

        /// <summary>Weapon: damage. Helmet/Armor: HP bonus.</summary>
        public static float Power(Item it)
        {
            if (it == null) return 0f;
            return Base(it) * (1f + 0.25f * it.plus);
        }

        /// <summary>Reducere de damage (0..1) de la armura.</summary>
        public static float Defense(Item it)
        {
            if (it == null || it.S != Slot.Armor) return 0f;
            return Mathf.Min(0.6f, 0.04f + it.rarity * 0.05f + it.tier * 0.06f + it.plus * 0.01f);
        }

        public static float CritBonus(Item it)
        {
            if (it == null || it.S != Slot.Ring) return 0f;
            return 0.02f + it.rarity * 0.025f + it.plus * 0.004f;
        }

        public static float CoinBonus(Item it)
        {
            if (it == null || it.S != Slot.Ring) return 0f;
            return (0.1f + it.rarity * 0.12f + it.tier * 0.1f) * (1f + it.plus * 0.05f);
        }

        public static string StatText(Item it)
        {
            if (it == null) return "";
            switch (it.S)
            {
                case Slot.Weapon: return "+" + Fmt.Num(Power(it)) + " DMG";
                case Slot.Helmet: return "+" + Fmt.Num(Power(it)) + " HP";
                case Slot.Armor: return "+" + Fmt.Num(Power(it)) + " HP   " + Mathf.RoundToInt(Defense(it) * 100) + "% DEF";
                default: return "+" + Mathf.RoundToInt(CritBonus(it) * 100) + "% CRIT   +" + Mathf.RoundToInt(CoinBonus(it) * 100) + "% COINS";
            }
        }

        public static string FullName(Item it)
        {
            if (it == null) return "";
            return it.name + (it.plus > 0 ? " +" + it.plus : "");
        }

        public static int SellValue(Item it)
        {
            float v = 8f * TierMul[Mathf.Clamp(it.tier, 0, MaxTier)] * Mathf.Pow(2.4f, it.rarity) * (1f + it.plus * 0.3f);
            return Mathf.Max(1, Mathf.RoundToInt(v));
        }

        // ---------------------------------------------------------------
        //  Forja
        // ---------------------------------------------------------------
        public static int ForgeCoins(Item it) { return Mathf.RoundToInt((40 + it.plus * it.plus * 25) * TierMul[Mathf.Clamp(it.tier, 0, MaxTier)] * (1f + it.rarity * 0.5f)); }
        public static int ForgeOre(Item it) { return 3 + it.plus * 2 + it.tier * 4; }
        /// <summary>De la +10 in sus forja cere si shards (moneda rara).</summary>
        public static int ForgeShards(Item it) { return it.plus >= 10 ? (it.plus - 9) * 2 : 0; }
        /// <summary>Forge Stones necesare (material care pica de la monstri / questuri).</summary>
        public static int ForgeStones(Item it) { return it.plus < 2 ? 0 : 1 + it.plus / 2 + it.tier; }
        /// <summary>De la +5 in sus, o forja esuata scade nivelul cu 1 (daca nu ai Guardian Cloak).</summary>
        public const int ForgeRiskFrom = 5;
        /// <summary>Sansa (%) de reusita pentru urmatorul nivel: +1 = 100%, +2 = 80%, apoi scade treptat.</summary>
        public static readonly int[] ForgeChances = { 100, 80, 70, 62, 55, 48, 42, 37, 33, 29, 26, 23, 20, 17, 15 };
        public static int ForgeChance(Item it) { return it == null ? 0 : ForgeChances[Mathf.Clamp(it.plus, 0, ForgeChances.Length - 1)]; }
        /// <summary>Guardian Cloaks necesare ca o forja esuata sa nu scada nivelul.</summary>
        public static int ForgeCloaks(Item it) { return it == null ? 0 : 1; }   // 1 Guardian Cloak la fiecare incercare protejata

        // ---------------------------------------------------------------
        //  Generare
        // ---------------------------------------------------------------
        static int nextId = 1;
        public static void SetNextId(int n) { nextId = Mathf.Max(nextId, n); }
        public static int NextId { get { return nextId; } }
        public static int NextIdTake() { return nextId++; }

        /// <summary>Raritatile din joc: Common, Rare, Legendary, Mythic (Uncommon -> Rare, Epic -> Legendary).
        /// Indicii raman 0..5 (culori / nume / puteri), dar 1 si 3 nu mai apar.</summary>
        public static int FixRarity(int r) { r = Mathf.Clamp(r, 0, 5); return r == 1 ? 2 : r == 3 ? 4 : r; }
        public static readonly int[] UsedRarities = { 0, 2, 4, 5 };

        /// <summary>Migrare: raritatea noua + cuvantul de raritate din nume (Fine -> Shiny, Mystic -> Golden).</summary>
        public static void FixItemRarity(Item it)
        {
            if (it == null) return;
            int o = Mathf.Clamp(it.rarity, 0, 5), n = FixRarity(o);
            if (n == o) { it.rarity = o; return; }
            it.rarity = n;
            if (it.name != null && RarityWord[o].Length > 0 && it.name.StartsWith(RarityWord[o] + " "))
                it.name = RarityWord[n] + it.name.Substring(RarityWord[o].Length);
        }

        /// <summary>Item aleator din catalog: categoria, raritatea si harta date.</summary>
        public static Item Make(Slot s, int rarity, int tier)
        {
            int idx = ItemCatalog.Pick((int)s, Mathf.Clamp(tier, 0, MaxTier), rarity);
            return FromCatalog(idx);
        }

        /// <summary>Exact itemul din catalog (index 0..95).</summary>
        public static Item FromCatalog(int idx, int plus = 0)
        {
            idx = Mathf.Clamp(idx, 0, ItemCatalog.Count - 1);
            var d = ItemCatalog.All[idx];
            var it = new Item();
            it.id = nextId++;
            it.slot = d.slot;
            it.rarity = d.rarity;
            it.tier = d.map;
            it.cat = idx + 1;
            it.roll = Random.Range(0.92f, 1.08f);
            it.name = d.name;
            it.plus = Mathf.Clamp(plus, 0, MaxPlus);
            return it;
        }

        /// <summary>Converteste un item vechi (sau cu seturi) in itemul echivalent din catalog: aceeasi harta,
        /// categorie si raritate; pastreaza id-ul, forja si lacatul.</summary>
        public static void ToCatalog(Item it)
        {
            if (it == null) return;
            it.set = 0;
            if (it.cat > 0 && it.cat <= ItemCatalog.Count) { var d0 = ItemCatalog.All[it.cat - 1]; it.name = d0.name; it.rarity = d0.rarity; it.tier = d0.map; it.slot = d0.slot; return; }
            it.rarity = FixRarity(it.rarity);
            int idx = ItemCatalog.Pick(Mathf.Clamp(it.slot, 0, 3), Mathf.Clamp(it.tier, 0, MaxTier), it.rarity, Mathf.Abs(it.id));
            if (idx < 0) return;
            var d = ItemCatalog.All[idx];
            it.cat = idx + 1; it.name = d.name; it.rarity = d.rarity; it.tier = d.map; it.slot = d.slot;
            it.roll = Mathf.Clamp(it.roll, 0.92f, 1.08f);
        }

        /// <summary>Raritate aleatoare. luck > 0 muta sansele spre rar (boss, pets, rebirth).</summary>
        public static int RollRarity(float luck, int minR = 0)
        {
            float[] w = { 60f, 26f, 10f, 3.2f, 0.7f, 0.1f };
            for (int i = 1; i < w.Length; i++) w[i] *= 1f + luck * i * 0.6f;
            for (int i = 0; i < minR; i++) w[i] = 0f;
            // 4 raritati: sansa de Uncommon trece la Common, cea de Epic la Rare (jocul ramane la fel de greu);
            // daca Common e exclus (minR = 1, boss), Uncommon devine Rare
            if (minR >= 1) w[2] += w[1]; else w[0] += w[1];
            w[1] = 0f;
            w[2] += w[3]; w[3] = 0f;
            float sum = 0; foreach (var x in w) sum += x;
            float r = Random.value * sum;
            for (int i = 0; i < w.Length; i++) { r -= w[i]; if (r <= 0) return i; }
            return 0;
        }

        /// <summary>Item care pica pe harta "tier" (setChance a ramas doar pentru compatibilitate - seturile au fost scoase).</summary>
        public static Item RandomDrop(int tier, float luck, int minR = 0, float setChance = 0f)
        {
            float p = Random.value;
            Slot s = p < 0.4f ? Slot.Weapon : p < 0.6f ? Slot.Helmet : p < 0.82f ? Slot.Armor : Slot.Ring;
            return Make(s, RollRarity(luck, minR), tier);
        }

        public static Item Starter()
        {
            var it = FromCatalog(ItemCatalog.IndexOf(0, 0, 0));
            it.roll = 1f;
            return it;
        }
    }

    /// <summary>Formatare numere mari (1.2K, 3.4M ...).</summary>
    public static class Fmt
    {
        static readonly string[] suf = { "", "K", "M", "B", "T", "Qa", "Qi" };
        public static string Num(double v)
        {
            if (v < 1000) return Mathf.RoundToInt((float)v).ToString();
            int i = 0;
            while (v >= 1000 && i < suf.Length - 1) { v /= 1000; i++; }
            return (v >= 100 ? v.ToString("0") : v >= 10 ? v.ToString("0.0") : v.ToString("0.00")) + suf[i];
        }
    }
}
