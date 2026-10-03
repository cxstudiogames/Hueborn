// =====================================================================
//  GuideUI.cs - butonul "?" (langa SETTINGS): ghidul jocului pe sectiuni.
//  Toate explicatiile stau aici, ca restul ferestrelor sa ramana simple.
// =====================================================================
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class GuideUI : Window
    {
        int sec;

        struct Sec { public string title; public int icon; public Color col; public string text; }

        static Sec[] secs;

        static Sec[] Sections()
        {
            if (secs != null) return secs;
            secs = new[]
            {
                new Sec { title = "BASICS", icon = 26, col = new Color(1f, 0.85f, 0.3f), text =
                    "<size=32><color=#ffd84d><b>Become the strongest hero on the server!</b></color></size>\n\n" +
                    "<color=#ff9a7a><b>FARM & LOOT</b></color> - slay monsters on " + GameData.Maps.Length + " colorful maps, beat the bosses and grab weapons, helmets, armor and rings. Every map has 6 unique items per category (2 Common, 2 Rare, 1 Legendary, 1 <color=#ff5dfb>MYTHIC</color>) - collect them all in the INDEX.\n" +
                    "<color=#ffb347><b>FORGE</b></color> - push your favorite items up to <b>+" + ItemDB.MaxPlus + "</b>. Risky, but every level makes you hit harder.\n" +
                    "<color=#ff9ae6><b>PETS</b></color> - hatch eggs, collect cute pets and take them into battle for bonus damage.\n" +
                    "<color=#c8b0ff><b>HEROES & CLASSES</b></color> - 12 heroes with their own skills, 3 Awakenings and " + Player.MaxRebirth + " Rebirths for permanent power.\n" +
                    "<color=#7dff8a><b>TITLES & RANKS</b></color> - over 60 titles to unlock. The harder the title, the more legendary it looks above your head.\n" +
                    "<color=#7fd8ff><b>LEADERBOARDS</b></color> - climb to the <b>TOP</b> by power, level and wealth - everyone sees your name in the hub.\n" +
                    "<color=#55f0b8><b>TRADE</b></color> - open a booth in the Trade Plaza, sell your best loot for <b>TOKENS</b> and get rich.\n" +
                    "<color=#ffd84d><b>QUESTS & FRIENDS</b></color> - quests give potions, shards and materials; add friends and chat with them.\n\n" +
                    "<color=#b8c4d6>Start in <b>Meadow Fields</b> (the hub), take a quest from the NPC with the yellow \"!\" and go hunting!</color>" },
                new Sec { title = "CONTROLS", icon = 28, col = new Color(0.7f, 0.85f, 1f), text = Device.Mobile ?
                    "<b>Left joystick</b> - move (push it all the way to run)\n" +
                    "<b>FIGHT</b> button - attack          <b>JUMP</b> button - jump\n" +
                    "<b>Tap a skill</b> (above the HP bar) - use it\n" +
                    "<b>Drag on the screen</b> - rotate the camera          <b>Pinch</b> - zoom\n" +
                    "<b>Tap</b> an item / NPC / portal prompt - interact\n" +
                    "<b>Potions</b> - INVENTORY > MATERIALS > USE\n\n" +
                    "Tap your portrait (top left) to rename your hero, change heroes and see your stats." :
                    "<b>W A S D / arrows</b> - move          <b>SHIFT</b> - sprint          <b>SPACE</b> - jump\n" +
                    "<b>Left click / F</b> - attack          <b>1 / 2 / 3</b> - hero skills\n" +
                    "<b>E</b> - interact / pick up          <b>Q</b> - heal potion\n" +
                    "<b>I / TAB</b> - inventory          <b>R</b> - auto swing\n" +
                    "<b>Hold RIGHT click + move mouse</b> - rotate camera          <b>Mouse wheel</b> - zoom\n" +
                    "<b>ENTER or /</b> - chat          <b>M</b> - mute          <b>ESC</b> - close windows\n\n" +
                    "Boost potions (XP / DMG / COINS) are used from INVENTORY > MATERIALS.\n" +
                    "Click your portrait (top left) to rename your hero, change heroes and see your stats." },
                new Sec { title = "PROGRESSION", icon = 22, col = new Color(0.55f, 1f, 0.6f), text = ProgressText() },
                new Sec { title = "HEROES & SKILLS", icon = 115, col = new Color(0.75f, 0.6f, 1f), text = SkillsText() },
                new Sec { title = "POWER & TITLES", icon = 22, col = new Color(1f, 0.55f, 0.15f), text =
                    "<b>POWER</b> (the flame above your head) shows how strong you are. It grows with your level, items, forge level, pets, class, rebirths and skill points.\n\n" +
                    "Each map has a recommended level - stronger maps drop better loot.\n\n" +
                    "<b>Titles</b> unlock over time (power, level, class, forge, pets, quests, bosses, playtime, trading, coins, leaderboards). " +
                    "Click your title in the INVENTORY to choose which one is shown. AUTO = the title for your power.\n\n" +
                    "<b>Skill points</b> (STATS button): +" + Player.PointsPerLevel + " every level. You can reset them for <color=#7dff8a>FREE once every 24 hours</color>." },
                new Sec { title = "LOOT & MAGNET", icon = 24, col = new Color(0.55f, 0.8f, 1f), text =
                    "Coins are collected automatically.\n\n" +
                    "New items go to your INVENTORY - they are <b>not equipped automatically</b>. Equip them yourself (EQUIP or EQUIP BEST). Every slot is optional - you can unequip anything, even your weapon.\n\n" +
                    "Items and ore stay on the ground for <b>10 seconds</b> - be quick! Walk to them and " + (Device.Mobile ? "tap the prompt with the item's name" : "press <b>E</b>") + " to pick them up.\n\n" +
                    "With the <b>MAGNET PASS</b> all loot flies to you automatically.\n\n" +
                    "Bosses and monsters also drop <b>Forge Stones</b> and, rarely, <b>Guardian Cloaks</b>." },
                new Sec { title = "ITEM INDEX", icon = 24, col = new Color(1f, 0.85f, 0.3f), text =
                    "The <b>ITEM INDEX</b> is the big book in <b>Meadow Fields</b>, on its own little path to the right of the main road (across from the quest NPC).\n\n" +
                    "It shows all <b>" + ItemCatalog.Count + " items</b> (weapons, helmets, armor, rings) and all <b>" + (GameData.MapPets.Length * ItemDB.UsedRarities.Length) + " pets</b> in the game: one row per map, from Common to <color=#ff5dfb>MYTHIC</color>.\n\n" +
                    "Everything you find (loot, hatch or buy) appears in color with its name - the rest is a dark shadow with \"???\".\n\n" +
                    "Found items and pets <b>stay in the INDEX forever</b>, even if you sell, trade or release them. Can you complete it?" },
                new Sec { title = "QUESTS", icon = 20, col = new Color(1f, 0.82f, 0.3f), text =
                    "Every map has a quest NPC near the spawn (yellow \"!\").\n\n" +
                    "You do ONE quest at a time. It completes automatically when you reach the goal and then starts again - talk to the quest NPC to switch it.\n\n" +
                    "Rewards: coins, XP, boost potions, Forge Stones, Guardian Cloaks and shards.\n\n" +
                    "<b>TRACK</b> shows an arrow to the nearest target. <b>HIDE</b> hides the tracker." },
                new Sec { title = "POTIONS", icon = 36, col = new Color(0.4f, 0.9f, 1f), text =
                    "Boost potions come from quests (and from trading).\n\n" +
                    "<b>XP Potion:</b> XP x2 for 5 minutes\n<b>Damage Potion:</b> damage x1.5 for 5 minutes\n<b>Coin Potion:</b> coins x1.5 for 5 minutes\n\n" +
                    "Drink them from INVENTORY > <b>MATERIALS</b> (select the potion, press USE). Active potions show at the bottom right with the time left.\n\n" +
                    "Using more potions adds time (up to 60 minutes)." },
                new Sec { title = "FORGE", icon = 16, col = new Color(1f, 0.6f, 0.25f), text =
                    "Open the FORGE and choose any item to upgrade it (+1 each time). Every level makes the item stronger.\n\n" +
                    "<b>Meadow Fields:</b> +0 to +5\n<b>Sunny Desert:</b> +5 to +10\n<b>Lava Volcano:</b> +10 to +15 (max)\n\n" +
                    "Chance: +1 = 100%, +2 = 80%, then lower and lower.\nCost: coins, ore, Forge Stones (and shards from +10).\n\n" +
                    "<b>A failed forge drops the item by 1 level.</b> Turn on <b>PROTECT</b> to keep the level - it uses 1 Guardian Cloak on every try (success or fail).\n\n" +
                    "<b>MAX FORGE</b> keeps forging until the forge limit or until you run out of materials." },
                new Sec { title = "PETS & EGGS", icon = 23, col = new Color(1f, 0.6f, 0.95f), text =
                    "Every map has its own egg near the spawn. Better maps = stronger pets. Eggs unlock at the map's level.\n\n" +
                    "Pets fight next to you and give bonus damage (rarer = stronger).\n\n" +
                    "You can equip 2 pets, or 4 with the <b>+2 PET SLOTS</b> pass. Manage them in the PETS window." },
                new Sec { title = "AWAKENING", icon = 26, col = new Color(0.8f, 0.55f, 1f), text =
                    "Awakening happens at the <b>AWAKENING SHRINE</b> in " + GameData.Maps[GameData.Maps.Length - 1].name + " (left of the spawn circle). " +
                    "It opens when you unlock that map (level " + GameData.AwakenLevels[0] + ").\n\n" +
                    "<b>Awakening I</b> (Lv " + GameData.AwakenLevels[0] + "): choose a class - Warrior, Assassin, Guardian or Berserker.\n" +
                    "<b>Awakening II</b> (Lv " + GameData.AwakenLevels[1] + ") and <b>III</b> (Lv " + GameData.AwakenLevels[2] + "): your class becomes stronger.\n\n" +
                    "<color=#ff9a7a><b>Choose carefully!</b></color> You can change your class later with <b>CHANGE CLASS</b>, but it costs a lot of coins and shards " +
                    "(and more every time). Your Awakening level stays.\n\n" +
                    "You can also change your hero from the CHARACTER window." },
                new Sec { title = "REBIRTH", icon = 9, col = new Color(1f, 0.45f, 0.9f), text =
                    "The Rebirth Shrine is next to the boss arena in Meadow Fields.\n\n" +
                    "Rebirth resets your level, coins and ore but gives a permanent bonus: +25% damage and +10% coins each time. You keep your items and pets.\n\n" +
                    "There are <b>" + Player.MaxRebirth + " rebirths</b> in total, each needs a higher level: " + string.Join(" / ", System.Array.ConvertAll(Player.RebirthLevels, x => "Lv " + x)) + "." },
                new Sec { title = "TRADING", icon = 21, col = new Color(0.55f, 0.95f, 0.75f), text =
                    "The <b>TRADE PLAZA</b> is where players trade. Everything is paid in <b>TOKENS</b> (bought in the SHOP).\n\n" +
                    "Claim a free booth and put up to <b>" + TradeDB.MaxListings + "</b> items, pets or materials on it at <b>your own price</b>. No haggling: whoever wants it, buys it.\n\n" +
                    "Your booth sells <b>only while you stay in that market</b>. When you leave (another map, another market or you log out) the booth becomes <b>unclaimed</b> - your listings stay with you and go back on sale on your next booth.\n\n" +
                    "Use the <b>TRADING</b> button to pick a market (max " + TradeDB.MaxPlayers + " traders each), QUICK JOIN one with free spots or add friends." },
                new Sec { title = "PASSES", icon = 10, col = new Color(0.45f, 1f, 0.55f), text =
                    "<b>MAGNET:</b> picks up all loot for you from far away.\n" +
                    "<b>AUTO SWING:</b> attacks nearby monsters automatically (button on the right, green = ON).\n" +
                    "<b>TELEPORT:</b> travel to any unlocked map from the TELEPORT button.\n" +
                    "<b>+2 PET SLOTS:</b> equip 4 pets instead of 2.\n" +
                    "<b>PRIVATE SERVER:</b> your own Trade Plaza server - invite up to " + (TradeDB.MaxPlayers - 1) + " friends (max 100 friends)." },
                new Sec { title = "CHAT & FRIENDS", icon = 33, col = new Color(0.75f, 0.85f, 1f), text =
                    "<b>GLOBAL</b> chat: everyone. <b>SERVER</b> chat: only players on your server. <b>FRIENDS</b>: private messages.\n\n" +
                    "What you write in SERVER chat also pops up above your head.\n\n" +
                    "<b>FRIENDS</b> button (left): search players by name, add them, press MESSAGE to write to them.\n\n" +
                    "Be nice: insults, swearing, links and spam are blocked automatically. Sending too many messages pauses your chat for a few seconds." },
            };
            return secs;
        }

        static string ProgressText()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>REWARDS</b> (gift button, top left): 10 gifts for staying online (5 min ... 2 hours, resets when you leave the game) and a gift every day for 7 days - missing a day never resets it.\n\n");
            sb.Append("<b>Your journey</b> (about how long it takes, playing actively):\n");
            string[] when = { "start", "~1-2 hours", "~5 hours", "~13 hours" };
            for (int i = 0; i < GameData.Maps.Length; i++)
                sb.Append("  <color=#ffd84d>Lv ").Append(GameData.Maps[i].req).Append("</color>  ").Append(GameData.Maps[i].name).Append("  <color=#9aa6c0>(").Append(when[Mathf.Min(i, when.Length - 1)]).Append(")</color>\n");
            sb.Append("  <color=#ffd84d>Lv ").Append(GameData.TradeLevel).Append("</color>  Trade Plaza  <color=#9aa6c0>(~45 minutes)</color>\n");
            sb.Append("  <color=#ffd84d>Lv ").Append(GameData.MaxLevel).Append("</color>  max level  <color=#9aa6c0>(~40 hours)</color>, then ").Append(Player.MaxRebirth).Append(" Rebirths for permanent power\n\n");
            sb.Append("<b>Pets:</b> your first egg is <color=#7dff8a>FREE</color> (Meadow Fields), the second one costs just a few minutes of farming.\n\n");
            sb.Append("<b>Currencies</b>\n");
            sb.Append("  <color=#ffd84d>COINS</color> - from monsters, quests and selling items. Forge, eggs, awakening.\n");
            sb.Append("  <color=#8fd8ff>SHARDS</color> - rare: bosses, quests, every 5 levels. Forge +10 and up, awakening.\n");
            sb.Append("  <color=#55f0b8>TOKENS</color> - trading money: earn them by selling on your booth, spend them on other booths.\n\n");
            sb.Append("<b>Quests</b> give the most potions, shards, Forge Stones and Guardian Cloaks - monsters drop them only rarely.\n");
            sb.Append("<b>Limits:</b> level ").Append(GameData.MaxLevel).Append(", POWER ").Append(Fmt.Num(GameData.PowerCap)).Append(". New levels and maps come with updates!");
            return sb.ToString();
        }

        static string SkillsText()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append((Device.Mobile ? "Every hero has <b>3 skills</b> of its own - tap them above the HP bar." : "Every hero has <b>3 skills</b> of its own: keys <b>1</b>, <b>2</b> and <b>3</b> (or click them above the HP bar).") + " Change your hero in CHARACTER to get other skills. Skills hit harder as your damage grows.\n\n");
            for (int h = 0; h < SkillDB.HeroSkills.Length; h++)
            {
                var sk = SkillDB.HeroSkills[h];
                sb.Append("<b>").Append(SkillDB.HeroTitle(h)).Append(":</b>  ");
                for (int i = 0; i < sk.Length; i++)
                {
                    var d = SkillDB.Get(sk[i]);
                    if (i > 0) sb.Append("  -  ");
                    sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(Color.Lerp(d.col, Color.white, 0.3f))).Append(">").Append(d.name).Append("</color>");
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public static GuideUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("GuideUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<GuideUI>();
            w.Init(parent, g, "GuideDim", "GUIDE", new Vector2(1500, 860), "Blue");
            return w;
        }

        float listPos = 1f;   // pozitia listei de capitole (ramane la fel cand alegi alt capitol)

        protected override void Build()
        {
            var all = Sections();
            sec = Mathf.Clamp(sec, 0, all.Length - 1);
            var left = Card(content, new Vector2(24, -16), new Vector2(330, 768), new Color(0.3f, 0.3f, 0.32f));
            // lista de capitole: derulanta (cu bara de scroll), ca sa incapa toate butoanele
            var vp = UIKit.Rect(left, "List", TL, TL, new Vector2(8, -8), new Vector2(276, 752));
            var vpi = vp.gameObject.AddComponent<Image>(); vpi.color = new Color(0, 0, 0, 0.001f);
            vp.gameObject.AddComponent<RectMask2D>();
            var list = UIKit.Rect(vp, "Content", TL, TL, Vector2.zero, new Vector2(276, all.Length * 50 + 4));
            var sr = vp.gameObject.AddComponent<ScrollRect>();
            sr.content = list; sr.viewport = vp;
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.inertia = true; sr.decelerationRate = 0.12f; sr.scrollSensitivity = 40f;
            for (int i = 0; i < all.Length; i++)
            {
                int k = i;
                var b = UIKit.Button(list, "S" + i, all[i].title, sec == i ? "Yellow" : "Grey", TC, TC, new Vector2(0, -2 - i * 50), new Vector2(268, 45), () => { sec = k; listPos = sr.verticalNormalizedPosition; Rebuild(); }, 21);
                UIKit.Fit(b.GetComponentInChildren<Text>(), 12);
            }
            var track = UIKit.Rect(left, "Track", TL, TL, new Vector2(292, -10), new Vector2(28, 748));
            UIKit.Img(track, "flat_Grey", new Color(0.08f, 0.09f, 0.12f, 0.6f), 8f, 2f);
            var sbar = track.gameObject.AddComponent<Scrollbar>();
            sbar.direction = Scrollbar.Direction.BottomToTop;
            var area = UIKit.Stretch(track, "Area", 2f);
            var handle = UIKit.Rect(area, "Handle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one; handle.offsetMin = handle.offsetMax = Vector2.zero;
            var hImg = UIKit.Img(handle, "flat_Grey", new Color(1f, 0.85f, 0.4f, 0.9f), 8f, 2f);
            sbar.handleRect = handle; sbar.targetGraphic = hImg;
            sr.verticalScrollbar = sbar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            Canvas.ForceUpdateCanvases();
            sr.verticalNormalizedPosition = listPos;
            var s = all[sec];
            var right = Card(content, new Vector2(370, -16), new Vector2(1106, 768), new Color(0.2f, 0.2f, 0.22f));
            var ic = UIKit.Rect(right, "Ic", TL, TL, new Vector2(30, -24), new Vector2(76, 76)).gameObject.AddComponent<Image>();
            ic.sprite = ItemIcons.Get(s.icon); ItemIcons.Art(ic); ic.color = s.col; ic.raycastTarget = false;
            Label(right, s.title, 46, s.col, new Vector2(124, -30), new Vector2(900, 64));
            var t = Label(right, s.text, 27, Color.white, new Vector2(34, -126), new Vector2(1040, 620), TextAnchor.UpperLeft);
            t.lineSpacing = 1.1f;
            UIKit.Fit(t, 16);
        }
    }
}
