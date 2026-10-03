// =====================================================================
//  PlayerExtras.cs - potiuni de boost (XP x2, DMG x1.5, COINS x1.5 - 5 minute),
//  materiale de forja (Forge Stone, Guardian Cloak), tokens si questuri.
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public partial class Player
    {
        // ---------------------------------------------------------------
        //  Potiuni de boost
        // ---------------------------------------------------------------
        public const float BuffSeconds = 300f;      // 5 minute
        public const float BuffMax = 3600f;         // se aduna pana la 1 ora
        public static readonly string[] PotionNames = { "XP Potion", "Damage Potion", "Coin Potion" };
        public static readonly int[] PotionIcons = { 36, 37, 38 };   // iconitele potiunilor (Resources/UI/IconArt)
        public static readonly string[] PotionShort = { "XP x2", "DMG x1.5", "COINS x1.5" };
        public static readonly Color[] PotionCols = { new Color(0.4f, 0.9f, 1f), new Color(1f, 0.42f, 0.36f), new Color(1f, 0.85f, 0.25f) };

        public bool BuffOn(int i) { return D != null && D.buffs[i] > 0f; }
        public float XpBoost { get { return BuffOn(0) ? 2f : 1f; } }
        public float DmgBoost { get { return (BuffOn(1) ? 1.5f : 1f) * SkillDmgBoost; } }   // Battle Cry inclus (POWER le exclude pe amandoua)
        public float CoinBoost { get { return BuffOn(2) ? 1.5f : 1f; } }

        public bool UsePotion(int i)
        {
            if (i < 0 || i > 2) return false;
            if (D.potions[i] <= 0) { Game.I.Say("You have no " + PotionNames[i] + "! Complete quests to get more.", 2.5f); Sfx.Play("Error", 0.5f); return false; }
            if (D.buffs[i] >= BuffMax - 1f) { Game.I.Say(PotionNames[i] + " is already at the maximum time (60 min).", 2f); Sfx.Play("Error", 0.4f); return false; }
            D.potions[i]--;
            D.buffs[i] = Mathf.Min(BuffMax, D.buffs[i] + BuffSeconds);
            Game.I.hud.BigText(PotionShort[i] + "!", PotionCols[i]);
            Fx.Ring(transform.position, PotionCols[i], 3.5f, 40);
            Fx.Sparkle(transform.position + Vector3.up * 1.5f, PotionCols[i], 16, 1f);
            Sfx.Play("Potion", 0.8f, 1.1f, 0f);
            SaveNow();
            return true;
        }

        void TickBuffs(float dt)
        {
            if (D == null) return;
            for (int i = 0; i < 3; i++)
            {
                if (D.buffs[i] <= 0f) continue;
                D.buffs[i] -= dt;
                if (D.buffs[i] <= 0f)
                {
                    D.buffs[i] = 0f;
                    Game.I.hud.Toast(PotionNames[i] + " wore off.", PotionCols[i]);
                }
            }
        }

        public void AddPotion(int i, int n)
        {
            D.potions[i] = Mathf.Max(0, D.potions[i] + n);
        }

        // ---------------------------------------------------------------
        //  Materiale + tokens
        // ---------------------------------------------------------------
        public void AddStones(int n, bool toast = true)
        {
            if (n <= 0) return;
            D.forgeStones += n;
            if (toast) Game.I.hud.Toast("+" + n + " Forge Stone" + (n > 1 ? "s" : ""), TradeDB.MatCols[TradeDB.MatStones], ItemIcons.Get(16));
        }

        public void AddCloaks(int n)
        {
            if (n <= 0) return;
            D.cloaks += n;
            Game.I.hud.Toast("+" + n + " Guardian Cloak! (protects your forge level)", TradeDB.MatCols[TradeDB.MatCloak], ItemIcons.Get(17));
            Sfx.Play("ItemRare", 0.7f);
        }

        public void AddTokens(long n)
        {
            if (n <= 0) return;
            D.tokens += n;
        }

        /// <summary>Drop de materiale la moartea unui inamic.</summary>
        public void MaterialDrop(Enemy e)
        {
            // ECONOMIE: monstrii dau materiale RAR (forja trebuie sa coste efort); questurile dau mult mai multe
            float stoneChance = e.boss ? 1f : 0.08f;
            if (Random.value < stoneChance) AddStones(e.boss ? Random.Range(3, 7) + e.mapIdx * 2 : 1, e.boss);
            if (e.boss && Random.value < 0.06f) AddCloaks(1);
        }

        // ---------------------------------------------------------------
        //  Questuri
        // ---------------------------------------------------------------
        public QuestProgress FindQuest(int map, int idx)
        {
            foreach (var q in D.quests) if (q.map == map && q.index == idx) return q;
            return null;
        }

        public bool AcceptQuest(int map, int idx)
        {
            if (FindQuest(map, idx) != null) return false;
            // o singura misiune activa: daca ai alta, o schimbi pe aceasta (progresul celei vechi se pierde)
            D.quests.Clear();
            D.quests.Add(new QuestProgress { map = map, index = idx, progress = 0, tracked = true });
            D.trackerHidden = false;
            var def = QuestDB.ForMap(map)[idx];
            Game.I.hud.Toast("Quest accepted: " + def.title + " - " + def.Goal, new Color(1f, 0.85f, 0.35f), ItemIcons.Get(20));
            Sfx.Play("Open", 0.7f, 1.1f);
            SaveNow();
            return true;
        }

        public void AbandonQuest(QuestProgress q)
        {
            if (q == null) return;
            D.quests.Remove(q);
            Sfx.Play("Close", 0.6f);
            SaveNow();
        }

        /// <summary>Apelat cand omori un inamic: creste progresul questurilor potrivite si le termina automat.</summary>
        public void QuestKill(Enemy e)
        {
            if (D.quests.Count == 0) return;
            for (int i = D.quests.Count - 1; i >= 0; i--)
            {
                var q = D.quests[i];
                var def = QuestDB.Get(q);
                if (q.map != e.mapIdx || def.target != e.nm || def.boss != e.boss) continue;
                q.progress++;
                if (q.progress >= def.count) CompleteQuest(q, def);
            }
        }

        /// <summary>ADMIN: termina toate questurile active (cu recompense).</summary>
        public void AdminCompleteQuests()
        {
            foreach (var q in new System.Collections.Generic.List<QuestProgress>(D.quests)) CompleteQuest(q, QuestDB.Get(q));
        }

        void CompleteQuest(QuestProgress q, QuestDef def)
        {
            q.progress = 0;   // aceeasi misiune se reia pana o schimbi
            D.questsDone++;
            long coins = (long)Mathf.Round(def.coins * CoinMul);
            AddCoins(coins);
            GainXp(def.xp);
            for (int k = 0; k < 3; k++) AddPotion(k, def.potions[k]);
            AddStones(def.stones, false);
            if (def.cloaks > 0) D.cloaks += def.cloaks;
            if (def.shards > 0) D.shards += def.shards;
            Game.I.hud.BigText("QUEST COMPLETE!", new Color(1f, 0.85f, 0.3f));
            // scurt: doar titlul si monedele / XP (restul recompenselor se vad in inventar)
            Game.I.hud.Toast(def.title + "  <color=#ffd84d>+" + Fmt.Num(coins) + "</color>  <color=#7fd8ff>+" + Fmt.Num(def.xp) + " XP</color>", new Color(1f, 0.85f, 0.3f), ItemIcons.Get(20));
            Fx.Pillar(transform.position, new Color(1f, 0.85f, 0.3f), 10f, 1f);
            Fx.Fountain(transform.position + Vector3.up, new Color(1f, 0.85f, 0.3f), 30);
            Sfx.Play("JingleLevel", 0.7f, 1.1f, 0f);
            Game.I.hud.PulseCoins();
            SaveNow();
        }

        /// <summary>Cel mai apropiat inamic pentru questurile urmarite (pentru sageata de tracking).</summary>
        public Enemy QuestTarget()
        {
            if (D.trackerHidden || D.quests.Count == 0 || Game.I.InPlaza) return null;
            Enemy best = null; float bd = float.MaxValue;
            Vector3 pp = transform.position;
            foreach (var q in D.quests)
            {
                if (!q.tracked || q.map != Game.I.mapIndex) continue;
                var def = QuestDB.Get(q);
                foreach (var e in Enemy.All)
                {
                    if (e == null || e.Dying || e.mapIdx != q.map || e.nm != def.target || e.boss != def.boss) continue;
                    float d = (e.transform.position - pp).sqrMagnitude;
                    if (d < bd) { bd = d; best = e; }
                }
            }
            return best;
        }
    }
}
