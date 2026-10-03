// =====================================================================
//  GameData.cs - definitii de harti si inamici (usor de echilibrat aici)
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public enum Shape { Slime, Cube, Golem }

    public class EnemyDef
    {
        public string name;
        public string model;          // cale in Resources (fara extensie) sau null => primitive
        public float height = 1.6f;   // inaltimea modelului (unitati)
        public Shape shape;           // fallback daca modelul lipseste
        public Color col;
        public float hpMul = 1f, dmgMul = 1f, xpMul = 1f, coinMul = 1f;
        public float speed = 3.5f;
        public bool humanoid;         // are animatii attack-melee / die
        public float yaw;             // corectie rotatie model
        public float aggro;           // raza in care te observa (0 = automat dupa viteza: 7..11 m)
        public float Aggro { get { return aggro > 0f ? aggro : Mathf.Clamp(5.5f + speed * 1.1f, 7f, 11f); } }
    }

    public class MapDef
    {
        public string name;
        public Vector3 origin;
        public Color ground, ground2, border, oreCol, sky, fog;
        public int hp, dmg, xp, coins, req, tier;
        public EnemyDef[] enemies;    // tipuri normale (primul e cel mai comun)
        public EnemyDef boss;
        public string step;           // categoria de sunet pentru pasi
        public Vector3 Spawn { get { return origin + new Vector3(0, 0.1f, -46f); } }
        /// <summary>Centrul arenei boss-ului (raza GameData.ArenaRadius).</summary>
        public Vector3 Arena { get { return origin + new Vector3(0, 0, 48f); } }
    }

    public static class GameData
    {
        public const string CHAR = "Models/Characters/";
        public const string DUN = "Models/Dungeon/";
        public const string ARENA = "Models/Arena/";
        public const string GRAVE = "Models/Graveyard/";
        public const string PETS = "Models/Pets/";
        public const string HOL = "Models/Holiday/";
        public const string TOWN = "Models/Town/";
        public const string NAT = "Models/Nature/";

        public static MapDef[] Maps;

        // ---- LIMITE (se pot creste la update-uri: nivele noi, harti noi) ----
        public const int MaxLevel = 100;
        public const long PowerCap = 10000000;          // 10M
        public const int TradeLevel = 10;               // Trade Plaza se deblocheaza la nivelul 10

        /// <summary>Monede castigate pe minut farmand pe harta "map" (estimare: ~18 monstri / minut).</summary>
        public static float CoinsPerMinute(int map) { return Maps[Mathf.Clamp(map, 0, Maps.Length - 1)].coins * 1.25f * 18f; }

        /// <summary>Cea mai buna harta deblocata la nivelul "level".</summary>
        public static int BestMap(int level) { int b = 0; for (int i = 0; i < Maps.Length; i++) if (level >= Maps[i].req) b = i; return b; }

        /// <summary>Pachetele de monede din SHOP cresc odata cu tine: 30 min / 2 ore / 8 ore de farm pe cea mai buna harta a ta.</summary>
        public static long CoinPack(int level, int i)
        {
            float[] minutes = { 30f, 120f, 480f };
            double v = CoinsPerMinute(BestMap(level)) * minutes[Mathf.Clamp(i, 0, 2)];
            double mag = System.Math.Pow(10, System.Math.Floor(System.Math.Log10(System.Math.Max(10, v))) - 1);
            return (long)(System.Math.Round(v / mag) * mag);
        }
        public const float ArenaRadius = 13f;

        // ---- zonele monstrilor: fiecare tip are locul lui pe fiecare harta (mai usor de gasit pentru questuri si farm)
        //  0 (cel mai comun) = stanga, 1 = dreapta, 2 (cel mai puternic) = in fata arenei boss-ului
        public static readonly int[] ZoneCount = { 9, 7, 5 };
        static readonly Vector4[] Zones =   // xMin, xMax, zMin, zMax (fata de originea hartii)
        {
            new Vector4(-54f, -20f, -20f, 30f),
            new Vector4(20f, 54f, -20f, 30f),
            new Vector4(-17f, 17f, 6f, 30f),
        };
        public static Vector3 ZoneCenter(int map, int kind)
        {
            var z = Zones[Mathf.Clamp(kind, 0, Zones.Length - 1)];
            return Maps[map].origin + new Vector3((z.x + z.y) * 0.5f, 0, (z.z + z.w) * 0.5f);
        }
        public static Vector3 ZonePoint(int map, int kind)
        {
            var z = Zones[Mathf.Clamp(kind, 0, Zones.Length - 1)];
            return Maps[map].origin + new Vector3(Random.Range(z.x, z.y), 0, Random.Range(z.z, z.w));
        }

        public static bool InArena(int map, Vector3 p)
        {
            if (Maps == null || map < 0 || map >= Maps.Length) return false;
            Vector3 d = p - Maps[map].Arena; d.y = 0;
            return d.magnitude < ArenaRadius + 0.6f;
        }

        public static void Init()
        {
            Maps = new[]
            {
                new MapDef {
                    name = "Meadow Fields", origin = new Vector3(0, 0, 0), tier = 0,
                    ground = new Color(0.42f, 0.84f, 0.36f), ground2 = new Color(0.52f, 0.9f, 0.4f),
                    border = new Color(0.2f, 0.6f, 0.25f), oreCol = new Color(0.95f, 0.55f, 0.3f),
                    sky = new Color(0.55f, 0.82f, 1f), fog = new Color(0.7f, 0.88f, 1f),
                    hp = 30, dmg = 4, xp = 20, coins = 6, req = 1, step = "StepGrass",
                    enemies = new[] {
                        new EnemyDef { name = "Slime", model = null, shape = Shape.Slime, col = new Color(1f, 0.45f, 0.8f), height = 1.5f },
                        new EnemyDef { name = "Wild Boar", model = PETS + "animal-hog", col = new Color(1f, 0.6f, 0.65f), height = 1.5f, hpMul = 1.6f, dmgMul = 1.3f, xpMul = 1.6f, coinMul = 1.6f, speed = 4.2f },
                        new EnemyDef { name = "Angry Bee", model = PETS + "animal-bee", col = new Color(1f, 0.85f, 0.2f), height = 1.2f, hpMul = 0.8f, dmgMul = 1.2f, xpMul = 1.2f, coinMul = 1.2f, speed = 5f },
                    },
                    boss = new EnemyDef { name = "Orc Warlord", model = DUN + "character-orc", col = new Color(0.45f, 0.8f, 0.3f), height = 2.2f, humanoid = true, shape = Shape.Golem, speed = 3.2f },
                },
                new MapDef {
                    name = "Sunny Desert", origin = new Vector3(300, 0, 0), tier = 1,
                    ground = new Color(1f, 0.84f, 0.48f), ground2 = new Color(1f, 0.77f, 0.4f),
                    border = new Color(0.85f, 0.6f, 0.25f), oreCol = new Color(0.35f, 1f, 0.6f),
                    sky = new Color(0.6f, 0.85f, 1f), fog = new Color(1f, 0.9f, 0.75f),
                    hp = 180, dmg = 14, xp = 110, coins = 35, req = 15, step = "StepSand",
                    enemies = new[] {
                        new EnemyDef { name = "Sand Crab", model = PETS + "animal-crab", col = new Color(1f, 0.45f, 0.25f), height = 1.1f, shape = Shape.Cube, speed = 3.8f },
                        new EnemyDef { name = "Skeleton", model = GRAVE + "character-skeleton", col = new Color(0.95f, 0.95f, 0.9f), height = 2f, humanoid = true, hpMul = 1.5f, dmgMul = 1.3f, xpMul = 1.5f, coinMul = 1.5f, speed = 3.4f },
                        new EnemyDef { name = "Mummy Zombie", model = GRAVE + "character-zombie", col = new Color(0.6f, 0.85f, 0.45f), height = 2f, humanoid = true, hpMul = 2f, dmgMul = 1.1f, xpMul = 1.8f, coinMul = 1.8f, speed = 2.6f },
                    },
                    boss = new EnemyDef { name = "Sand Lion King", model = PETS + "animal-lion", col = new Color(1f, 0.7f, 0.2f), height = 2.2f, shape = Shape.Cube, speed = 3.4f },
                },
                new MapDef {
                    name = "Frozen Peaks", origin = new Vector3(600, 0, 0), tier = 2,
                    ground = new Color(0.8f, 0.93f, 1f), ground2 = new Color(0.9f, 0.97f, 1f),
                    border = new Color(0.4f, 0.7f, 1f), oreCol = new Color(0.55f, 0.85f, 1f),
                    sky = new Color(0.62f, 0.78f, 1f), fog = new Color(0.85f, 0.92f, 1f),
                    hp = 1100, dmg = 45, xp = 260, coins = 160, req = 35, step = "StepSnow",
                    enemies = new[] {
                        new EnemyDef { name = "Ice Penguin", model = PETS + "animal-penguin", col = new Color(0.4f, 0.65f, 1f), height = 1.4f, shape = Shape.Golem, speed = 3.6f },
                        new EnemyDef { name = "Frost Ghost", model = GRAVE + "character-ghost", col = new Color(0.75f, 0.9f, 1f), height = 2f, humanoid = true, hpMul = 1.3f, dmgMul = 1.4f, xpMul = 1.5f, coinMul = 1.5f, speed = 4f },
                        new EnemyDef { name = "Ice Vampire", model = GRAVE + "character-vampire", col = new Color(0.6f, 0.4f, 1f), height = 2.1f, humanoid = true, hpMul = 2.2f, dmgMul = 1.5f, xpMul = 2f, coinMul = 2f, speed = 3.5f },
                    },
                    boss = new EnemyDef { name = "Frost Titan", model = PETS + "animal-polar", col = new Color(0.8f, 0.9f, 1f), height = 2.2f, shape = Shape.Golem, speed = 3f },
                },
                new MapDef {
                    name = "Lava Volcano", origin = new Vector3(900, 0, 0), tier = 3,
                    ground = new Color(0.36f, 0.27f, 0.26f), ground2 = new Color(0.46f, 0.3f, 0.24f),
                    border = new Color(0.55f, 0.18f, 0.1f), oreCol = new Color(1f, 0.45f, 0.1f),
                    sky = new Color(0.55f, 0.35f, 0.32f), fog = new Color(0.62f, 0.38f, 0.3f),
                    hp = 7000, dmg = 140, xp = 420, coins = 600, req = 60, step = "StepStone",
                    enemies = new[] {
                        new EnemyDef { name = "Magma Slime", model = null, shape = Shape.Slime, col = new Color(1f, 0.4f, 0.1f), height = 1.6f },
                        new EnemyDef { name = "Ember Fox", model = PETS + "animal-fox", col = new Color(1f, 0.5f, 0.2f), height = 1.4f, hpMul = 1.3f, dmgMul = 1.3f, xpMul = 1.4f, coinMul = 1.4f, speed = 4.8f },
                        new EnemyDef { name = "Ash Walker", model = GRAVE + "character-zombie", col = new Color(0.5f, 0.45f, 0.45f), height = 2.1f, humanoid = true, hpMul = 2.2f, dmgMul = 1.5f, xpMul = 2f, coinMul = 2f, speed = 2.8f },
                    },
                    boss = new EnemyDef { name = "Molten Mammoth", model = PETS + "animal-elephant", col = new Color(1f, 0.35f, 0.1f), height = 3f, shape = Shape.Golem, speed = 2.8f },
                },
            };
        }

        // ---------------- pets ----------------
        /// <summary>Fiecare harta are oul ei, cu 6 pets (index = raritatea: Common..Mythic).</summary>
        public static readonly string[][] MapPets =
        {
            new[] { "animal-chick", "animal-bunny", "animal-pig", "animal-cow", "animal-dog", "animal-cat" },           // Meadow
            new[] { "animal-caterpillar", "animal-beaver", "animal-parrot", "animal-monkey", "animal-giraffe", "animal-lion" }, // Desert
            new[] { "animal-fish", "animal-koala", "animal-deer", "animal-penguin", "animal-panda", "animal-polar" },   // Frozen
            new[] { "animal-hog", "animal-bee", "animal-fox", "animal-crab", "animal-tiger", "animal-elephant" },      // Lava
        };
        /// <summary>Compatibilitate (salvari vechi): pets pe raritate.</summary>
        public static readonly string[][] PetModels =
        {
            new[] { "animal-chick" }, new[] { "animal-bunny" }, new[] { "animal-pig" },
            new[] { "animal-cow" }, new[] { "animal-dog" }, new[] { "animal-cat" },
        };
        /// <summary>Cat de puternici sunt pets de pe fiecare harta.</summary>
        public static readonly float[] PetMapMul = { 1f, 1.6f, 2.4f, 3.5f };   // (rebalansat: pets erau prea puternici)
        /// <summary>Pretul de baza al oului pe fiecare harta (creste cu 35% la fiecare ou deschis acolo).</summary>
        public static readonly long[] EggBaseCost = { 1200, 30000, 450000, 4500000 };   // primul ou din joc e GRATIS
        public const int PetStorage = 16;
        public const int PetsEquipped = 2, PetsEquippedPass = 4;

        public static string PetName(string model)
        {
            string n = model.Replace("animal-", "");
            return char.ToUpperInvariant(n[0]) + n.Substring(1);
        }

        // ---------------- awakening (clase) ----------------
        /// <summary>Versiunea lumii. O cresc la fiecare schimbare a hartilor, ca editorul sa reconstruiasca "=== WORLD ===" in scena.</summary>
        public const int WorldVersion = 43;   // v35: munti + sol larg dupa marginea hartilor, INDEX in Meadow; v25: arena cu stanci, cactusi noi, fara bannere/cosuri de foc; v24: frunze reparate; v23: lumea noua cu "Stylized Nature MegaKit"
        /// <summary>Puterea recomandata pentru fiecare harta (afisata la portaluri si in TELEPORT).</summary>
        public static readonly long[] RecPower = { 0, 900, 3000, 11000 };
        public const int AwakenLevel = 20;
        /// <summary>Plaza foloseste melodia/particulele cu acest id (hartile sunt 0..3).</summary>
        public const int PlazaZone = 9;
        public const int ClassChangeCost = 2500;

        public class ClassDef
        {
            public string name, tag, desc;
            public Color col;
            public float hpMul = 1f, dmgMul = 1f, defAdd, critAdd, atkSpeed = 1f;
        }

        /// <summary>Index 0 = fara clasa. Tag-ul apare deasupra jucatorului, ex. (A) 32 Ionutcx.</summary>
        public static readonly ClassDef[] Classes =
        {
            new ClassDef { name = "Adventurer", tag = "", desc = "Not awakened yet.", col = new Color(0.8f, 0.8f, 0.85f) },
            new ClassDef { name = "Warrior", tag = "W", desc = "+25% HP, +10% DMG", col = new Color(1f, 0.62f, 0.25f), hpMul = 1.25f, dmgMul = 1.1f },
            new ClassDef { name = "Assassin", tag = "A", desc = "+12% CRIT, +25% attack speed", col = new Color(1f, 0.35f, 0.5f), critAdd = 0.12f, atkSpeed = 1.25f },
            new ClassDef { name = "Guardian", tag = "G", desc = "+15% DEF, +20% HP", col = new Color(0.35f, 0.7f, 1f), defAdd = 0.15f, hpMul = 1.2f },
            new ClassDef { name = "Berserker", tag = "B", desc = "+35% DMG, -10% DEF", col = new Color(0.95f, 0.25f, 0.25f), dmgMul = 1.35f, defAdd = -0.1f },
        };

        // ---------------- awakening progresiv: max 3 treziri ----------------
        // Awakening I: alegi clasa (o data, nu se mai schimba) - versiunea "ok".
        // Awakening II: clasa devine "decenta". Awakening III: clasa devine "buna" (cea mai puternica).
        public const int MaxAwaken = 3;
        public static readonly int[] AwakenLevels = { 60, 75, 90 };   // awakening doar dupa ce deblochezi ultima harta (altarul e acolo)
        public static readonly long[] AwakenCoins = { 0, 150000, 3000000 };
        public static readonly long[] AwakenShards = { 0, 25, 100 };
        public static readonly float[] AwakenPower = { 1f, 1.8f, 2.8f };
        public static readonly string[] TierRoman = { "", "I", "II", "III" };
        public static readonly string[][] ClassTierNames =
        {
            new[] { "Adventurer", "Adventurer", "Adventurer" },
            new[] { "Warrior", "Warlord", "Titan" },
            new[] { "Assassin", "Shadowblade", "Phantom" },
            new[] { "Guardian", "Paladin", "Bastion" },
            new[] { "Berserker", "Ravager", "Bloodlord" },
        };

        static readonly Dictionary<int, ClassDef> tierCache = new Dictionary<int, ClassDef>();

        /// <summary>Clasa "cls" la treapta "tier" (1..3): bonusurile cresc cu AwakenPower.</summary>
        public static ClassDef ClassAt(int cls, int tier)
        {
            cls = Mathf.Clamp(cls, 0, Classes.Length - 1);
            if (cls == 0 || tier <= 1) return Classes[cls];
            tier = Mathf.Clamp(tier, 1, MaxAwaken);
            int key = cls * 10 + tier;
            ClassDef d;
            if (tierCache.TryGetValue(key, out d)) return d;
            var b = Classes[cls];
            float k = AwakenPower[tier - 1];
            d = new ClassDef
            {
                name = ClassTierNames[cls][tier - 1], tag = ClassTierNames[cls][tier - 1].Substring(0, 1), col = Color.Lerp(b.col, Color.white, 0.12f * (tier - 1)),
                hpMul = 1f + (b.hpMul - 1f) * k, dmgMul = 1f + (b.dmgMul - 1f) * k,
                defAdd = b.defAdd > 0f ? b.defAdd * k : b.defAdd, critAdd = b.critAdd * k, atkSpeed = 1f + (b.atkSpeed - 1f) * k,
            };
            d.desc = ClassDesc(d);
            tierCache[key] = d;
            return d;
        }

        /// <summary>Insigna clasei langa nume: litera clasei pe treapta curenta, tot mai "legendara".
        /// I = [A]   II = «S» (mai mare, bold)   III = «« P »» (auriu, cel mai mare)</summary>
        public static string ClassTagRich(int cls, int tier, int fontSize)
        {
            if (cls <= 0) return "";
            tier = Mathf.Clamp(tier, 1, MaxAwaken);
            var c = ClassAt(cls, tier);
            string col = ColorUtility.ToHtmlStringRGB(c.col);
            switch (tier)
            {
                case 1: return "<color=#" + col + ">[" + c.tag + "]</color> ";
                case 2: return "<size=" + Mathf.RoundToInt(fontSize * 1.18f) + "><b><color=#" + col + ">«" + c.tag + "»</color></b></size> ";
                default: return "<size=" + Mathf.RoundToInt(fontSize * 1.32f) + "><b><color=#ffd84d>«</color><color=#" + col + ">" + c.tag + "</color><color=#ffd84d>»</color></b></size> ";
            }
        }

        public static string ClassDesc(ClassDef d)
        {
            var l = new List<string>();
            if (d.hpMul != 1f) l.Add((d.hpMul > 1f ? "+" : "") + Mathf.RoundToInt((d.hpMul - 1f) * 100) + "% HP");
            if (d.dmgMul != 1f) l.Add("+" + Mathf.RoundToInt((d.dmgMul - 1f) * 100) + "% DMG");
            if (d.defAdd != 0f) l.Add((d.defAdd > 0 ? "+" : "") + Mathf.RoundToInt(d.defAdd * 100) + "% DEF");
            if (d.critAdd != 0f) l.Add("+" + Mathf.RoundToInt(d.critAdd * 100) + "% CRIT");
            if (d.atkSpeed != 1f) l.Add("+" + Mathf.RoundToInt((d.atkSpeed - 1f) * 100) + "% attack speed");
            return string.Join(", ", l.ToArray());
        }

        /// <summary>Personajul jucatorului (Mini Characters).</summary>
        public static readonly string[] Heroes =
        {
            "character-male-a", "character-female-a", "character-male-b", "character-female-b",
            "character-male-c", "character-female-c", "character-male-d", "character-female-d",
            "character-male-e", "character-female-e", "character-male-f", "character-female-f",
        };
    }
}
