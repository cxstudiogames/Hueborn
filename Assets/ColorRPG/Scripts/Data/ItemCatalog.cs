// =====================================================================
//  ItemCatalog.cs - TOATE itemele din joc (96): pe fiecare harta, pentru
//  fiecare categorie (arma, coif, armura, inel) exista 6 iteme unice:
//  2 Common, 2 Rare, 1 Legendary, 1 Mythic. Fiecare are numele, forma si
//  culoarea lui; cu cat e mai rar, cu atat arata mai special (Common simplu,
//  Rare alta forma, Legendary coroana / aripi / mantie, Mythic cel mai regal).
//  Itemul retine doar indexul din catalog (Item.cat = index + 1).
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public static class ItemCatalog
    {
        public struct Def
        {
            public int slot, map, rarity;
            public string name;
            public int baseIdx;   // arma: WeaponKind; coif: 0 Helm 1 Cap 2 Hood 3 Crown; armura: 0 Plate 1 Mail 2 Tunic 3 Vest; inel: 0 Ring 1 Band 2 Loop 3 Signet
            public int style;     // 0..3 (forma in cadrul tipului, vezi GearVisuals)
            public Color accent;  // culoarea proprie (panglici, stofa, pietre)
            public string model;  // arme: modelul 3D (Resources/Models/Weapons)
            public float mul;     // al doilea item din aceeasi raritate e putin mai bun
        }

        public const int PerMap = 6;   // pe categorie
        public static readonly int[] RarityOfPos = { 0, 0, 2, 2, 4, 5 };

        static Color C(string hex) { Color c; ColorUtility.TryParseHtmlString("#" + hex, out c); return c; }

        static Def W(int map, int pos, string name, ItemDB.WeaponKind kind, string model, string acc)
        { return new Def { slot = 0, map = map, rarity = RarityOfPos[pos], name = name, baseIdx = (int)kind, style = 0, accent = C(acc), model = model, mul = pos % 2 == 1 && pos < 4 ? 1.12f : 1f }; }
        static Def G(int slot, int map, int pos, string name, int b, int st, string acc)
        { return new Def { slot = slot, map = map, rarity = RarityOfPos[pos], name = name, baseIdx = b, style = st, accent = C(acc), mul = pos % 2 == 1 && pos < 4 ? 1.12f : 1f }; }

        const ItemDB.WeaponKind Sw = ItemDB.WeaponKind.Sword, Ax = ItemDB.WeaponKind.Axe, Hm = ItemDB.WeaponKind.Hammer,
                                Sp = ItemDB.WeaponKind.Spear, Sb = ItemDB.WeaponKind.Saber, Gs = ItemDB.WeaponKind.Greatsword;

        public static readonly Def[] All =
        {
            // ======================= ARME =======================
            // Meadow
            W(0, 0, "Wooden Sword",        Sw, "KK_sword_A",     "8a5a33"),
            W(0, 1, "Woodcutter Axe",      Ax, "KK_axe_A",       "6f8f3a"),
            W(0, 2, "Thornleaf Saber",     Sb, "KK_dagger_A",    "4caf50"),
            W(0, 3, "Oakroot Hammer",      Hm, "KK_hammer_A",    "7cb342"),
            W(0, 4, "Verdant Greatsword",  Gs, "KK_sword_B",     "ffd54f"),
            W(0, 5, "Bloomreaper Scythe",  Sp, "Q_Scythe",       "ff6fa8"),
            // Desert
            W(1, 0, "Sandstone Sword",     Sw, "Q_Sword",        "d7a35b"),
            W(1, 1, "Dune Dagger",         Sb, "Q_Dagger",       "c58b4a"),
            W(1, 2, "Scorpion Axe",        Ax, "Q_Axe_Small",    "26a69a"),
            W(1, 3, "Sunspear",            Sp, "Q_Spear",        "ffb300"),
            W(1, 4, "Pharaoh Claymore",    Gs, "Q_Claymore",     "29b6f6"),
            W(1, 5, "Solar Blade",         Sw, "Q_Sword_Golden", "ffca28"),
            // Frozen Peaks
            W(2, 0, "Frost Sword",         Sw, "KK_sword_C",     "90caf9"),
            W(2, 1, "Icepick Hammer",      Hm, "KK_hammer_C",    "b3e5fc"),
            W(2, 2, "Glacier Axe",         Ax, "KK_axe_B",       "4fc3f7"),
            W(2, 3, "Frostfang Saber",     Sb, "KK_sword_E",     "80deea"),
            W(2, 4, "Winter Halberd",      Sp, "KK_halberd",     "ffd54f"),
            W(2, 5, "Aurora Greatsword",   Gs, "KK_sword_D",     "b388ff"),
            // Lava
            W(3, 0, "Magma Dagger",        Sb, "KK_dagger_B",    "ff7043"),
            W(3, 1, "Ember Hammer",        Hm, "Q_Hammer_Small", "ff8a65"),
            W(3, 2, "Obsidian Sword",      Sw, "Q_Sword_2",      "8e24aa"),
            W(3, 3, "Cinder Axe",          Ax, "Q_Axe",          "ff5722"),
            W(3, 4, "Molten Double Axe",   Ax, "Q_Axe_Double",   "ffab00"),
            W(3, 5, "Phoenix Greatsword",  Gs, "Q_Sword_Big",    "ff1744"),

            // ======================= COIFURI =======================
            G(1, 0, 0, "Leather Cap",        1, 0, "8d6e63"),
            G(1, 0, 1, "Woolen Beanie",      1, 3, "66bb6a"),
            G(1, 0, 2, "Iron Knight Helm",   0, 0, "43a047"),
            G(1, 0, 3, "Ranger Hat",         1, 2, "2e7d32"),
            G(1, 0, 4, "Laurel Crown",       3, 2, "9ccc65"),
            G(1, 0, 5, "Meadow King Crown",  3, 3, "e91e63"),

            G(1, 1, 0, "Sand Bandana",       1, 1, "d4a056"),
            G(1, 1, 1, "Nomad Hood",         2, 0, "c9a26b"),
            G(1, 1, 2, "Bronze Plumed Helm", 0, 3, "e65100"),
            G(1, 1, 3, "Desert Wizard Hat",  2, 1, "00897b"),
            G(1, 1, 4, "Sun Diadem",         3, 1, "ffb300"),
            G(1, 1, 5, "Pharaoh Crown",      3, 0, "1e88e5"),

            G(1, 2, 0, "Fur Beanie",         1, 3, "90a4ae"),
            G(1, 2, 1, "Snow Hood",          2, 0, "b0bec5"),
            G(1, 2, 2, "Viking Helm",        0, 1, "5c6bc0"),
            G(1, 2, 3, "Frost Wizard Hat",   2, 1, "4fc3f7"),
            G(1, 2, 4, "Winged Helm",        0, 2, "fff59d"),
            G(1, 2, 5, "Frost King Crown",   3, 3, "7c4dff"),

            G(1, 3, 0, "Ash Mask Hood",      2, 3, "5d4037"),
            G(1, 3, 1, "Charred Helm",       0, 0, "6d4c41"),
            G(1, 3, 2, "Demon Hood",         2, 2, "c62828"),
            G(1, 3, 3, "Obsidian Viking Helm", 0, 1, "ff7043"),
            G(1, 3, 4, "Inferno Plume Helm", 0, 3, "ff6d00"),
            G(1, 3, 5, "Phoenix Crown",      3, 0, "ff1744"),

            // ======================= ARMURI =======================
            G(2, 0, 0, "Cloth Tunic",        2, 0, "a1887f"),
            G(2, 0, 1, "Leather Vest",       3, 0, "8d6e63"),
            G(2, 0, 2, "Ranger Vest",        3, 1, "43a047"),
            G(2, 0, 3, "Iron Mail",          1, 0, "388e3c"),
            G(2, 0, 4, "Knight Plate",       0, 0, "ffd54f"),
            G(2, 0, 5, "Meadow King Plate",  0, 3, "e91e63"),

            G(2, 1, 0, "Nomad Robe",         2, 1, "d4a056"),
            G(2, 1, 1, "Sand Vest",          3, 2, "c58b4a"),
            G(2, 1, 2, "Bronze Scale Mail",  1, 1, "e65100"),
            G(2, 1, 3, "Sun Mage Robe",      2, 3, "00897b"),
            G(2, 1, 4, "Sunguard Plate",     0, 2, "ffb300"),
            G(2, 1, 5, "Pharaoh Plate",      0, 3, "1e88e5"),

            G(2, 2, 0, "Fur Tunic",          2, 2, "90a4ae"),
            G(2, 2, 1, "Studded Vest",       3, 3, "78909c"),
            G(2, 2, 2, "Fur Mail",           1, 3, "5c6bc0"),
            G(2, 2, 3, "Frost Emblem Mail",  1, 2, "4fc3f7"),
            G(2, 2, 4, "Glacier Plate",      0, 1, "e1f5fe"),
            G(2, 2, 5, "Frost King Plate",   0, 3, "7c4dff"),

            G(2, 3, 0, "Ash Tunic",          2, 0, "6d4c41"),
            G(2, 3, 1, "Charred Vest",       3, 1, "5d4037"),
            G(2, 3, 2, "Obsidian Scale",     1, 1, "8e24aa"),
            G(2, 3, 3, "Ember Robe",         2, 3, "ff5722"),
            G(2, 3, 4, "Spiked Inferno Plate", 0, 1, "ff6d00"),
            G(2, 3, 5, "Phoenix Plate",      0, 3, "ff1744"),

            // ======================= INELE =======================
            G(3, 0, 0, "Copper Band",        1, 0, "b87333"),
            G(3, 0, 1, "Twig Loop",          2, 0, "8d6e63"),
            G(3, 0, 2, "Leaf Ring",          0, 0, "66bb6a"),
            G(3, 0, 3, "Clover Signet",      3, 0, "2e7d32"),
            G(3, 0, 4, "Sunstone Ring",      0, 1, "ffca28"),
            G(3, 0, 5, "Bloom Halo Ring",    0, 3, "ff4081"),

            G(3, 1, 0, "Sand Band",          1, 0, "d4a056"),
            G(3, 1, 1, "Bronze Loop",        2, 2, "e65100"),
            G(3, 1, 2, "Scarab Signet",      3, 0, "00897b"),
            G(3, 1, 3, "Oasis Ring",         0, 2, "26c6da"),
            G(3, 1, 4, "Pharaoh Ring",       0, 1, "1e88e5"),
            G(3, 1, 5, "Solar Halo Ring",    0, 3, "ffb300"),

            G(3, 2, 0, "Frost Band",         1, 0, "90caf9"),
            G(3, 2, 1, "Icicle Loop",        2, 1, "b3e5fc"),
            G(3, 2, 2, "Glacier Ring",       0, 2, "4fc3f7"),
            G(3, 2, 3, "Snow Signet",        3, 0, "5c6bc0"),
            G(3, 2, 4, "Aurora Ring",        0, 1, "b388ff"),
            G(3, 2, 5, "Frost King Halo",    0, 3, "7c4dff"),

            G(3, 3, 0, "Ash Band",           1, 0, "6d4c41"),
            G(3, 3, 1, "Ember Loop",         2, 2, "ff7043"),
            G(3, 3, 2, "Obsidian Signet",    3, 0, "8e24aa"),
            G(3, 3, 3, "Magma Ring",         0, 2, "ff5722"),
            G(3, 3, 4, "Inferno Ring",       0, 1, "ff6d00"),
            G(3, 3, 5, "Phoenix Halo Ring",  0, 3, "ff1744"),
        };

        public static int Count { get { return All.Length; } }

        /// <summary>Definitia itemului (null daca e un item vechi, fara catalog).</summary>
        public static bool TryGet(Item it, out Def d)
        {
            if (it != null && it.cat > 0 && it.cat <= All.Length) { d = All[it.cat - 1]; return true; }
            d = default(Def); return false;
        }

        /// <summary>Indexul in catalog: harta, categoria, pozitia 0..5.</summary>
        public static int IndexOf(int slot, int map, int pos)
        {
            for (int i = 0, k = 0; i < All.Length; i++)
                if (All[i].slot == slot && All[i].map == map) { if (k == pos) return i; k++; }
            return -1;
        }

        /// <summary>Un index aleator din catalog cu categoria / harta / raritatea data.</summary>
        public static int Pick(int slot, int map, int rarity, int seed = -1)
        {
            rarity = ItemDB.FixRarity(rarity);
            int n = 0;
            for (int i = 0; i < All.Length; i++) if (All[i].slot == slot && All[i].map == map && All[i].rarity == rarity) n++;
            if (n == 0) return IndexOf(slot, map, 0);
            int want = seed >= 0 ? seed % n : Random.Range(0, n);
            for (int i = 0; i < All.Length; i++)
                if (All[i].slot == slot && All[i].map == map && All[i].rarity == rarity) { if (want == 0) return i; want--; }
            return -1;
        }
    }
}
