// =====================================================================
//  QuestData.cs - questurile de la NPC-ul de la spawn-ul fiecarei harti.
//  Fiecare harta are 4 questuri (cate unul pentru fiecare zona de monstri + boss-ul).
//  Se pot lua din nou dupa ce le termini (repetabile).
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public class QuestDef
    {
        public string title;
        public string target;       // numele inamicului (EnemyDef.name)
        public bool boss;
        public int count;
        // recompense
        public long coins;
        public int xp;
        public int[] potions = { 0, 0, 0 };   // XP / DMG / COINS
        public int stones, cloaks, shards;

        public string Goal { get { return "Kill " + count + " " + target + (boss ? " (boss)" : ""); } }
    }

    public static class QuestDB
    {
        public const int MaxActive = 1;   // o singura misiune odata; se repeta pana o schimbi
        public static readonly string[] NpcNames = { "Elder Rowan", "Nomad Zahir", "Sage Frida", "Warden Ignis" };

        static QuestDef[][] all;

        public static QuestDef[] ForMap(int map)
        {
            if (all == null) Build();
            return all[Mathf.Clamp(map, 0, all.Length - 1)];
        }

        public static QuestDef Get(QuestProgress q) { return ForMap(q.map)[q.index]; }

        static void Build()
        {
            // monstri: [harta] = { zona 1, zona 2, zona 3 (cel mai puternic), boss }
            string[][] mobs =
            {
                new[] { "Slime", "Wild Boar", "Angry Bee", "Orc Warlord" },
                new[] { "Sand Crab", "Skeleton", "Mummy Zombie", "Sand Lion King" },
                new[] { "Ice Penguin", "Frost Ghost", "Ice Vampire", "Frost Titan" },
                new[] { "Magma Slime", "Ember Fox", "Ash Walker", "Molten Mammoth" },
            };
            string[][] titles =
            {
                new[] { "Slime Cleanup", "Boar Trouble", "Bee Swarm", "Topple the Warlord" },
                new[] { "Crab Invasion", "Bones in the Sand", "Wrapped Up", "Dethrone the Lion King" },
                new[] { "Penguin Patrol", "Ghost Hunt", "Cold Blood", "Freeze the Titan" },
                new[] { "Cool the Slimes", "Fox Fire", "Ashes to Ashes", "Tame the Mammoth" },
            };
            // ECONOMIE: questurile sunt sursa principala de potiuni, shards, Forge Stones si Guardian Cloaks
            // (monstrii le dau rar) - vezi PROGRESS.md "Sistemul jocului"
            long[] coinBase = { 500, 7000, 70000, 550000 };
            int[] xpBase = { 300, 2600, 14000, 52000 };
            all = new QuestDef[4][];
            for (int m = 0; m < 4; m++)
            {
                all[m] = new[]
                {
                    new QuestDef { title = titles[m][0], target = mobs[m][0], count = 15, coins = coinBase[m], xp = xpBase[m],
                                   potions = new[] { 1, 0, 1 }, stones = 5 + m * 3, shards = 1 + m },
                    new QuestDef { title = titles[m][1], target = mobs[m][1], count = 12, coins = coinBase[m] * 3 / 2, xp = xpBase[m] * 3 / 2,
                                   potions = new[] { 1, 1, 0 }, stones = 7 + m * 4, shards = 1 + m },
                    new QuestDef { title = titles[m][2], target = mobs[m][2], count = 10, coins = coinBase[m] * 2, xp = xpBase[m] * 2,
                                   potions = new[] { 0, 1, 1 }, stones = 9 + m * 5, cloaks = m >= 1 ? 1 : 0, shards = 2 + m * 2 },
                    new QuestDef { title = titles[m][3], target = mobs[m][3], boss = true, count = 3, coins = coinBase[m] * 5, xp = xpBase[m] * 5,
                                   potions = new[] { 2, 2, 2 }, stones = 15 + m * 6, cloaks = 1 + m / 2, shards = 5 + m * 3 },
                };
            }
        }

        public static string RewardText(QuestDef q)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<color=#ffd84d>").Append(Fmt.Num(q.coins)).Append(" coins</color>  <color=#7fd8ff>").Append(Fmt.Num(q.xp)).Append(" XP</color>");
            if (q.potions[0] > 0) sb.Append("\n<color=#66e6ff>").Append(q.potions[0]).Append("x XP Potion (x2, 5 min)</color>");
            if (q.potions[1] > 0) sb.Append("\n<color=#ff7a6b>").Append(q.potions[1]).Append("x Damage Potion (x1.5, 5 min)</color>");
            if (q.potions[2] > 0) sb.Append("\n<color=#ffd84d>").Append(q.potions[2]).Append("x Coin Potion (x1.5, 5 min)</color>");
            if (q.stones > 0) sb.Append("\n<color=#c8d0f0>").Append(q.stones).Append("x Forge Stone</color>");
            if (q.cloaks > 0) sb.Append("\n<color=#c47dff>").Append(q.cloaks).Append("x Guardian Cloak</color>");
            if (q.shards > 0) sb.Append("\n<color=#8fd8ff>").Append(q.shards).Append(" shards</color>");
            return sb.ToString();
        }
    }
}
