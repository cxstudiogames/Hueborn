// =====================================================================
//  PlayerProgress.cs - statistici, XP, inventar/echipament, forja,
//  pets, rebirth, potiuni si salvare.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public partial class Player
    {
        public SaveData D;
        /// <summary>Cati pets pot fi echipati: 2 normal, 4 cu Game Pass.</summary>
        public int MaxPets { get { return D != null && D.passGame ? GameData.PetsEquippedPass : GameData.PetsEquipped; } }
        public const int BaseBag = 40;
        /// <summary>Spatiile din inventar: 40 + cele cumparate din SHOP (+20 / +40 / +100).</summary>
        public int BagSize { get { return BaseBag + (D.bag20 ? 20 : 0) + (D.bag40 ? 40 : 0) + (D.bag100 ? 100 : 0); } }
        public static readonly float[] PetMul = { 1f, 1.5f, 2.2f, 3.2f, 4.6f, 6.5f };
        readonly List<GameObject> petObjs = new List<GameObject>();
        float rebirthAskT = -10f;

        // ---------------------------------------------------------------
        //  Statistici derivate
        // ---------------------------------------------------------------
        public int Level { get { return D.level; } }
        /// <summary>Curba de XP gandita pentru ~24h de joc pana la nivelul 80 (vezi PROGRESS.md).</summary>
        public int XpNeed { get { return Mathf.RoundToInt(20f * Mathf.Pow(D.level, 2.3f) + 30f); } }
        public bool MaxedLevel { get { return D.level >= GameData.MaxLevel; } }
        /// <summary>Bonusul de damage al unui pet: Common de pe prima harta +2%, Mythic din Lava +25%
        /// (maxim ~+100% cu 4 pets - pets ajuta, dar nu bat itemele).</summary>
        static readonly float[] PetPowerBase = { 0.02f, 0.025f, 0.032f, 0.045f, 0.055f, 0.0714f };
        public static float PetPower(PetData pd) { return PetPowerBase[Mathf.Clamp(pd.rarity, 0, 5)] * GameData.PetMapMul[Mathf.Clamp(pd.map, 0, GameData.PetMapMul.Length - 1)]; }
        public float PetBonus { get { float b = 0f; foreach (var pd in D.pets) if (pd.equipped) b += PetPower(pd); return b; } }
        public int PetsEquippedCount { get { int n = 0; foreach (var pd in D.pets) if (pd.equipped) n++; return n; } }

        // ---------------- seturi de iteme ----------------
        /// <summary>Cate piese din setul "set" ai echipate.</summary>
        public float RebirthMul { get { return 1f + 0.25f * D.rebirth; } }
        public GameData.ClassDef Class { get { return GameData.ClassAt(D.awakenClass, D.awakenTier); } }
        public float Damage
        {
            get
            {
                float w = ItemDB.Power(Equipped(Slot.Weapon));
                return (6f + D.level * 2f + w) * RebirthMul * (1f + PetBonus) * (1f + 0.03f * D.statDmg) * Class.dmgMul * ItemDB.KindDamage(Equipped(Slot.Weapon)) * DmgBoost;
            }
        }
        public float MaxHp { get { return (100f + 20f * (D.level - 1) + 12f * D.statHp + ItemDB.Power(Equipped(Slot.Helmet)) + ItemDB.Power(Equipped(Slot.Armor))) * Class.hpMul; } }
        public float Defense { get { return Mathf.Clamp(ItemDB.Defense(Equipped(Slot.Armor)) + 0.008f * D.statDef + Class.defAdd, 0f, 0.8f); } }
        // ---------------- POWER: tot jocul se masoara in putere ----------------
        /// <summary>Puterea totala (nivel, iteme, forja, pets, clasa, rebirth, skill points). Fara potiunile temporare.</summary>
        public long Power
        {
            get
            {
                if (D == null) return 0;
                float dmg = Damage / Mathf.Max(0.01f, DmgBoost);
                float p = dmg * 9f + MaxHp * 0.7f + Defense * 600f + CritChance * 350f + D.level * 6f;
                return System.Math.Min(GameData.PowerCap, (long)Mathf.Round(p));   // limita de putere (creste la update-uri)
            }
        }
        public static readonly string[] PowerTitles = { "NEWBIE", "ROOKIE", "FIGHTER", "WARRIOR", "VETERAN", "ELITE", "CHAMPION", "HERO", "LEGEND", "MYTHIC", "GODLIKE" };
        public static readonly long[] PowerSteps = { 0, 600, 2000, 6000, 15000, 40000, 100000, 250000, 600000, 1500000, 4000000 };
        public static readonly Color[] PowerTitleCols =
        {
            new Color(0.8f, 0.8f, 0.85f), new Color(0.55f, 0.95f, 0.5f), new Color(0.4f, 0.8f, 1f), new Color(0.5f, 0.6f, 1f),
            new Color(0.8f, 0.5f, 1f), new Color(1f, 0.5f, 0.85f), new Color(1f, 0.75f, 0.25f), new Color(1f, 0.55f, 0.2f),
            new Color(1f, 0.35f, 0.3f), new Color(0.3f, 1f, 0.9f), new Color(1f, 0.95f, 0.5f)
        };
        public static int TitleIndex(long power)
        {
            int t = 0;
            for (int i = 0; i < PowerSteps.Length; i++) if (power >= PowerSteps[i]) t = i;
            return t;
        }
        public static string PowerTitle(long power) { return PowerTitles[TitleIndex(power)]; }
        public static Color PowerTitleCol(long power) { return PowerTitleCols[TitleIndex(power)]; }

        public float CritChance { get { return Mathf.Min(0.75f, 0.1f + ItemDB.CritBonus(Equipped(Slot.Ring)) + Class.critAdd); } }

        // ---------------- skill points (3 per level: HP / DMG / DEF) ----------------
        public const int PointsPerLevel = 3;
        public int PointsTotal { get { return (D.level - 1) * PointsPerLevel; } }
        public int PointsFree { get { return Mathf.Max(0, PointsTotal - D.statHp - D.statDmg - D.statDef); } }

        /// <summary>0 = HP, 1 = DMG, 2 = DEF</summary>
        public bool AddPoint(int stat, int amount = 1)
        {
            amount = Mathf.Min(amount, PointsFree);
            if (amount <= 0) { Sfx.Play("Error", 0.4f); return false; }
            float hpPct = hp / Mathf.Max(1f, MaxHp);
            if (stat == 0) D.statHp += amount; else if (stat == 1) D.statDmg += amount; else D.statDef += amount;
            hp = Mathf.Clamp(MaxHp * hpPct, 1f, MaxHp);
            Sfx.Play("Equip", 0.5f, 1.2f);
            return true;
        }

        // ---------------- resetarea skill points: GRATIS o data la 24 de ore ----------------
        public static readonly System.TimeSpan StatResetCooldown = System.TimeSpan.FromHours(24);
        public System.TimeSpan StatResetLeft
        {
            get
            {
                var left = new System.DateTime(System.Math.Max(0, D.lastStatReset), System.DateTimeKind.Utc) + StatResetCooldown - System.DateTime.UtcNow;
                return left.Ticks > 0 ? left : System.TimeSpan.Zero;
            }
        }
        public bool CanResetStats { get { return StatResetLeft.Ticks <= 0; } }

        /// <summary>Resetare gratuita (1 pe zi). Intoarce false daca mai trebuie asteptat.</summary>
        public bool FreeResetPoints()
        {
            if (!CanResetStats) { var l = StatResetLeft; Game.I.Say("Free reset available in " + (int)l.TotalHours + "h " + l.Minutes + "m.", 2.5f); Sfx.Play("Error", 0.5f); return false; }
            if (D.statHp + D.statDmg + D.statDef == 0) { Game.I.Say("You have no points to reset.", 2f); Sfx.Play("Error", 0.4f); return false; }
            D.lastStatReset = System.DateTime.UtcNow.Ticks;
            ResetPoints();
            Game.I.hud.Toast("Skill points reset! Next free reset in 24 hours.", new Color(0.6f, 1f, 0.6f));
            return true;
        }

        public void ResetPoints()
        {
            D.statHp = D.statDmg = D.statDef = 0;
            hp = Mathf.Min(hp, MaxHp);
            Sfx.Play("Close", 0.6f);
            SaveNow();
        }

        // ---------------- awakening ----------------
        /// <summary>Urmatoarea trezire (1..3). cls conteaza doar la prima (alegerea clasei, definitiva).</summary>
        public bool Awaken(int cls)
        {
            if (ChoosingNewClass) return PickNewClass(cls);   // dupa CHANGE CLASS: alegi doar clasa, treapta ramane
            int next = D.awakenTier + 1;
            if (next > GameData.MaxAwaken) { Game.I.Say("You already reached the final awakening!", 2.5f); Sfx.Play("Error", 0.5f); return false; }
            if (D.awakenClass == 0)
            {
                if (cls <= 0 || cls >= GameData.Classes.Length) return false;
            }
            else cls = D.awakenClass;   // clasa nu se mai schimba
            int lv = GameData.AwakenLevels[next - 1];
            long coins = GameData.AwakenCoins[next - 1], shards = GameData.AwakenShards[next - 1];
            if (D.level < lv) { Game.I.Say("Awakening " + GameData.TierRoman[next] + " unlocks at level " + lv + "!", 2.5f); Sfx.Play("Error", 0.5f); return false; }
            if (D.coins < coins || D.shards < shards) { Game.I.Say("Awakening " + GameData.TierRoman[next] + " costs " + Fmt.Num(coins) + " coins + " + shards + " shards!", 2.5f); Sfx.Play("Error", 0.5f); return false; }
            D.coins -= coins; D.shards -= shards;
            float hpPct = hp / Mathf.Max(1f, MaxHp);
            D.awakenClass = cls;
            D.awakenTier = next;
            hp = Mathf.Clamp(MaxHp * hpPct, 1f, MaxHp);
            var c = Class;
            Game.I.hud.BigText("AWAKENING " + GameData.TierRoman[next] + ": " + c.name.ToUpper() + "!", c.col);
            Fx.Pillar(transform.position, c.col, 18f + next * 4f, 1.6f);
            Fx.Ring(transform.position, c.col, 5f + next, 60);
            Sfx.Play("JingleRebirth", 0.8f, 1.1f, 0f);
            SaveNow();
            return true;
        }
        // ---------------- schimbarea clasei (dupa awakening) - costa mult, pretul creste ----------------
        public long ClassResetShards { get { return 150L * D.awakenTier * (1 + D.classResets); } }
        public long ClassResetCoins { get { return System.Math.Max(100000L, GameData.AwakenCoins[Mathf.Clamp(D.awakenTier - 1, 0, 2)] * 3) * (1 + D.classResets); } }
        /// <summary>Ai platit resetarea si acum alegi o clasa noua (treapta de awakening ramane).</summary>
        public bool ChoosingNewClass { get { return D.awakenTier > 0 && D.awakenClass == 0; } }

        public bool ResetClass()
        {
            if (D.awakenTier <= 0 || D.awakenClass == 0) return false;
            long sh = ClassResetShards, co = ClassResetCoins;
            if (D.shards < sh || D.coins < co) { Game.I.Say("Changing your class costs " + Fmt.Num(co) + " coins + " + Fmt.Num(sh) + " shards!", 3f); Sfx.Play("Error", 0.6f); return false; }
            D.shards -= sh; D.coins -= co;
            D.classResets++;
            float hpPct = hp / Mathf.Max(1f, MaxHp);
            D.awakenClass = 0;   // treapta ramane - alegi doar clasa noua
            hp = Mathf.Clamp(MaxHp * hpPct, 1f, MaxHp);
            Game.I.hud.BigText("CHOOSE YOUR NEW CLASS", new Color(1f, 0.8f, 0.3f));
            Sfx.Play("JingleFail", 0.5f, 1.2f, 0f);
            SaveNow();
            return true;
        }

        public bool PickNewClass(int cls)
        {
            if (!ChoosingNewClass || cls <= 0 || cls >= GameData.Classes.Length) return false;
            float hpPct = hp / Mathf.Max(1f, MaxHp);
            D.awakenClass = cls;
            hp = Mathf.Clamp(MaxHp * hpPct, 1f, MaxHp);
            var c = Class;
            Game.I.hud.BigText("NEW CLASS: " + c.name.ToUpper() + "!", c.col);
            Fx.Pillar(transform.position, c.col, 20f, 1.6f);
            Sfx.Play("JingleRebirth", 0.8f, 1.1f, 0f);
            SaveNow();
            return true;
        }

        public float CoinMul { get { return (1f + ItemDB.CoinBonus(Equipped(Slot.Ring)) + 0.1f * D.rebirth) * CoinBoost; } }
        public float AttackCooldown { get { return 0.42f / (Class.atkSpeed * ItemDB.KindAttackSpeed(Equipped(Slot.Weapon))); } }
        /// <summary>Maxim 5 rebirth-uri, fiecare cere un nivel tot mai mare (progresiv).</summary>
        public const int MaxRebirth = 5;
        public static readonly int[] RebirthLevels = { 50, 65, 80, 90, 100 };   // toate <= nivelul maxim (100)
        public int RebirthReq { get { return RebirthLevels[Mathf.Clamp(D.rebirth, 0, RebirthLevels.Length - 1)]; } }
        public bool RebirthMaxed { get { return D.rebirth >= MaxRebirth; } }
        public int PotionCost { get { return 20 + D.level * 4; } }

        // ---------------------------------------------------------------
        //  Salvare
        // ---------------------------------------------------------------
        void LoadProgress()
        {
            D = SaveSystem.Load();
        }

        public void SaveNow()
        {
            if (AdminTools.BlockSave) return;
            if (Game.I != null) D.mapIndex = Game.I.mapIndex;
            D.nextItemId = ItemDB.NextId;
            SaveSystem.Save(D);
        }

        // ---------------------------------------------------------------
        //  XP / monede
        // ---------------------------------------------------------------
        public void GainXp(int n)
        {
            if (MaxedLevel) { D.xp = 0; return; }   // nivelul maxim (creste la update-uri)
            D.xp += n;
            bool up = false;
            while (D.xp >= XpNeed && D.level < GameData.MaxLevel)
            {
                D.xp -= XpNeed;
                D.level++;
                up = true;
            }
            if (up)
            {
                hp = MaxHp;
                Game.I.hud.LevelUp(D.level, PointsPerLevel);
                if (D.awakenTier < GameData.MaxAwaken && D.level == GameData.AwakenLevels[D.awakenTier]) Game.I.hud.Toast("AWAKENING " + GameData.TierRoman[D.awakenTier + 1] + " unlocked! Go to the AWAKENING SHRINE (" + AwakeningUI.ShrineMapName + ").", new Color(1f, 0.8f, 0.3f));
                LevelUpFx.Play(transform);
                Sfx.Play("JingleLevel", 0.7f, 1f, 0f);
                if (Sfx.I != null) Sfx.I.Duck(1.5f);
                PlayEmote("emote-yes");
                foreach (var m in GameData.Maps)
                    if (m.req == D.level) Game.I.Say("You unlocked " + m.name + "! Find its portal in the Teleport Corner.", 5f);
                if (D.level == GameData.TradeLevel) Game.I.hud.Toast("TRADE PLAZA unlocked! Sell your loot for TOKENS (TRADING button).", new Color(0.55f, 0.95f, 0.75f), ItemIcons.Get(21));
                if (D.level >= GameData.MaxLevel) { D.xp = 0; Game.I.hud.Toast("MAX LEVEL " + GameData.MaxLevel + "! Rebirth for permanent power.", new Color(1f, 0.8f, 0.3f)); }
                // la fiecare 5 niveluri: +3 shards
                while (D.lastShardLevel + 5 <= D.level) { D.lastShardLevel += 5; AddShards(3, "Level " + D.lastShardLevel + " reward"); }
                SaveNow();
            }
        }

        public void AddCoins(long n)
        {
            D.coins += n;
        }

        /// <summary>Shards = moneda rara (boss, niveluri, cadouri, General Shop).</summary>
        public void AddShards(long n, string why)
        {
            if (n <= 0) return;
            D.shards += n;
            Game.I.hud.Toast("+" + n + " shards  (" + why + ")", new Color(0.6f, 0.85f, 1f));
            Sfx.Play("ItemRare", 0.5f, 1.3f);
        }

        // ---------------------------------------------------------------
        //  Inventar & echipament
        // ---------------------------------------------------------------
        public Item Equipped(Slot s)
        {
            if (D == null) return null;
            int id = D.equipped[(int)s];
            if (id == 0) return null;
            var items = D.items;                      // bucla simpla: Find(x => ...) aloca o lambda la fiecare apel
            for (int i = 0; i < items.Count; i++) { var it = items[i]; if (it != null && it.id == id) return it; }
            return null;
        }

        public bool IsEquipped(Item it) { return it != null && D.equipped[it.slot] == it.id; }

        public int BagCount
        {
            get { int n = 0; foreach (var it in D.items) if (!IsEquipped(it)) n++; return n; }
        }

        /// <summary>Adauga un item; daca inventarul e plin, il vinde automat.</summary>
        /// <summary>INDEX: itemul din catalog e marcat ca descoperit (chiar daca rucsacul e plin).</summary>
        public void Discover(Item it)
        {
            if (it == null || D == null) return;
            ItemDB.ToCatalog(it);
            if (D.seenCat == null) D.seenCat = new System.Collections.Generic.List<int>();
            if (it.cat > 0 && !D.seenCat.Contains(it.cat)) D.seenCat.Add(it.cat);
        }

        public bool AddItem(Item it)
        {
            Discover(it);
            if (BagCount >= BagSize)
            {
                int v = ItemDB.SellValue(it);
                D.coins += v;
                Game.I.hud.Toast("Inventory full! " + it.name + " sold (+" + Fmt.Num(v) + ")", ItemDB.Col(it.rarity));
                return false;
            }
            D.items.Add(it);
            // itemele NU se mai echipeaza singure: le echipezi tu din INVENTORY (EQUIP sau EQUIP BEST)
            return true;
        }

        /// <summary>Scor comparativ pentru auto-equip / sortare.</summary>
        public static float Score(Item it)
        {
            if (it == null) return 0f;
            switch (it.S)
            {
                case Slot.Ring: return ItemDB.CritBonus(it) * 1000f + ItemDB.CoinBonus(it) * 100f;
                case Slot.Armor: return ItemDB.Power(it) + ItemDB.Defense(it) * 500f;
                default: return ItemDB.Power(it);
            }
        }

        public void Equip(Item it, bool silent = false)
        {
            if (it == null) return;
            float hpPct = hp / Mathf.Max(1f, MaxHp);
            D.equipped[it.slot] = it.id;
            hp = Mathf.Clamp(MaxHp * hpPct, 1f, MaxHp);
            RefreshSword();   // arma, coiful si armura se vad pe erou
            if (!silent) Sfx.Play("Equip", 0.7f);
        }

        public void Unequip(Slot s)
        {
            // (arma se poate scoate si ea - poti sta fara nimic echipat, ex. in Trade Plaza)
            if (D.equipped[(int)s] != 0 && BagCount >= BagSize) { Game.I.Say("Your inventory is full!", 2f); Sfx.Play("Error", 0.5f); return; }
            D.equipped[(int)s] = 0;
            hp = Mathf.Min(hp, MaxHp);
            RefreshSword();
            Sfx.Play("Equip", 0.6f);
        }

        public void Sell(Item it)
        {
            if (it == null || IsEquipped(it)) return;
            int v = ItemDB.SellValue(it);
            D.items.Remove(it);
            D.coins += v;
            Sfx.Play("Sell", 0.7f);
            Game.I.hud.Toast("+" + Fmt.Num(v) + " coins", new Color(1f, 0.9f, 0.3f));
        }

        /// <summary>Vinde itemele selectate in inventar (fara cele echipate / blocate).</summary>
        public int SellMany(System.Collections.Generic.List<Item> list)
        {
            long total = 0; int n = 0;
            foreach (var it in list)
            {
                if (it == null || IsEquipped(it) || it.locked || !D.items.Contains(it)) continue;
                total += ItemDB.SellValue(it);
                D.items.Remove(it);
                n++;
            }
            if (n > 0)
            {
                D.coins += total;
                Sfx.Play("Sell", 0.8f);
                Game.I.hud.Toast("Sold " + n + (n == 1 ? " item" : " items") + " (+" + Fmt.Num(total) + " coins)", new Color(1f, 0.9f, 0.3f));
                Game.I.hud.PulseCoins();
            }
            else Sfx.Play("Error", 0.5f);
            SaveNow();
            return n;
        }

        /// <summary>Vinde tot ce nu e echipat/blocat si are raritatea <= maxRarity.</summary>
        public int SellAll(int maxRarity)
        {
            long total = 0; int n = 0;
            for (int i = D.items.Count - 1; i >= 0; i--)
            {
                var it = D.items[i];
                if (IsEquipped(it) || it.locked || it.rarity > maxRarity) continue;
                total += ItemDB.SellValue(it);
                D.items.RemoveAt(i);
                n++;
            }
            if (n > 0)
            {
                D.coins += total;
                Sfx.Play("Sell", 0.8f);
                Game.I.hud.Toast("Sold " + n + " items (+" + Fmt.Num(total) + ")", new Color(1f, 0.9f, 0.3f));
            }
            else Sfx.Play("Error", 0.5f);
            SaveNow();
            return n;
        }

        /// <summary>Echipeaza automat cele mai bune iteme din inventar.</summary>
        public void EquipBest()
        {
            for (int s = 0; s < 4; s++)
            {
                Item best = Equipped((Slot)s);
                foreach (var it in D.items)
                    if (it.slot == s && Score(it) > Score(best)) best = it;
                if (best != null) Equip(best, true);
            }
            Sfx.Play("Equip", 0.7f);
        }

        // ---------------------------------------------------------------
        //  Forja (imbunatateste arma echipata)
        // ---------------------------------------------------------------
        /// <summary>Ce nivele de forja permite forja de pe harta asta: Meadow 0..5, Desert 5..10, Lava 10..15 (max).</summary>
        public static void ForgeRange(int map, out int min, out int max)
        {
            if (map == 1) { min = 5; max = 10; }
            else if (map == 3) { min = 10; max = ItemDB.MaxPlus; }
            else { min = 0; max = 5; }
        }

        public static string ForgeMapName(int plus)
        {
            if (plus < 5) return GameData.Maps[0].name;
            if (plus < 10) return GameData.Maps[1].name;
            return GameData.Maps[3].name;
        }

        /// <summary>null = poti forja itemul aici, altfel motivul pentru care nu.</summary>
        public string ForgeBlock(Item it, int map)
        {
            if (it == null) return "Select an item";
            if (it.plus >= ItemDB.MaxPlus) return "MAX LEVEL (+" + ItemDB.MaxPlus + ")";
            int mn, mx; ForgeRange(map, out mn, out mx);
            if (it.plus < mn) return "Needs +" + mn + " first (forge it in " + ForgeMapName(it.plus) + ")";
            if (it.plus >= mx) return "Go to the " + ForgeMapName(it.plus) + " forge for +" + (it.plus + 1);
            return null;
        }

        public string ForgeLabel() { return "[E] Open the FORGE"; }

        /// <summary>Forjeaza itemul ales. Intoarce: 1 = reusit, 0 = esuat, -1 = n-ai putut incerca.</summary>
        public int ForgeItem(Item w, int map, bool protect, Vector3 fxPos, bool quiet = false)
        {
            if (ForgeBlock(w, map) != null) { Sfx.Play("Error", 0.5f); return -1; }
            int c = ItemDB.ForgeCoins(w), o = ItemDB.ForgeOre(w), sh = ItemDB.ForgeShards(w), st = ItemDB.ForgeStones(w);
            bool risky = w.plus >= ItemDB.ForgeRiskFrom;   // sub +5 o forja esuata nu strica nimic
            int cl = protect && risky ? ItemDB.ForgeCloaks(w) : 0;
            if (D.coins < c || D.ore < o || D.shards < sh || D.forgeStones < st || D.cloaks < cl)
            {
                string miss = D.coins < c ? "coins" : D.ore < o ? "ore" : D.forgeStones < st ? "Forge Stones (monsters & quests drop them)" : D.cloaks < cl ? "Guardian Cloaks" : "shards";
                if (!quiet) Game.I.Say("Not enough " + miss + "!", 2.5f);
                Sfx.Play("Error", 0.6f);
                return -1;
            }
            D.coins -= c; D.ore -= o; D.shards -= sh; D.forgeStones -= st;
            if (cl > 0) { D.cloaks -= cl; Fx.Ring(fxPos, TradeDB.MatCols[TradeDB.MatCloak], 3f, 40); }   // cloak-ul se consuma la fiecare incercare (reusita sau nu)
            Sfx.Play("ForgeHit", 0.8f);
            Fx.Fountain(fxPos + Vector3.up * 1.4f, new Color(1f, 0.6f, 0.15f), 20);
            int result;
            if (Random.Range(0, 100) < ItemDB.ForgeChance(w))
            {
                w.plus++;
                D.bestPlus = Mathf.Max(D.bestPlus, w.plus);
                if (IsEquipped(w)) RefreshSword();
                Fx.Sparkle(fxPos + Vector3.up * 1.6f, ItemDB.Col(w.rarity), 16, 1f);
                Sfx.Play("JingleForge", 0.6f, 1f, 0f);
                result = 1;
            }
            else
            {
                // o forja esuata pierde materialele; nivelul scade cu 1 daca nu e protejata
                if (risky && w.plus > 0 && !protect)
                {
                    w.plus--;
                    if (IsEquipped(w)) RefreshSword();
                }
                Sfx.Play("JingleFail", 0.5f, 1f, 0f);
                result = 0;
            }
            SaveNow();
            return result;
        }

        // ---------------------------------------------------------------
        //  Pets: fiecare harta are oul ei; 2 echipati (4 cu Game Pass)
        // ---------------------------------------------------------------
        public long EggCost(int map)
        {
            map = Mathf.Clamp(map, 0, GameData.EggBaseCost.Length - 1);
            int total = 0; foreach (var n in D.eggsHatched) total += n;
            if (total == 0 && map == 0) return 0;   // primul ou din joc e gratis (primul pet in cateva minute)
            double c = GameData.EggBaseCost[map] * System.Math.Pow(1.35, D.eggsHatched[map]);
            return (long)System.Math.Min(c, 1e17);   // fara overflow dupa foarte multe oua
        }

        public string EggLabel(int map)
        {
            var m = GameData.Maps[Mathf.Clamp(map, 0, GameData.Maps.Length - 1)];
            if (D.level < m.req) return m.name + " Egg: unlocks at level " + m.req;
            if (D.pets.Count >= GameData.PetStorage) return "Your pet storage is full (" + GameData.PetStorage + ") - release a pet in the PETS window";
            long ec = EggCost(map);
            return "[E] Hatch a " + m.name + " egg (" + (ec == 0 ? "FREE - your first pet!" : Fmt.Num(ec) + " coins") + ")";
        }

        public void Hatch(int map, Vector3 eggPos)
        {
            map = Mathf.Clamp(map, 0, GameData.MapPets.Length - 1);
            if (D.level < GameData.Maps[map].req) { Game.I.Say("The " + GameData.Maps[map].name + " egg unlocks at level " + GameData.Maps[map].req + "!", 2.5f); Sfx.Play("Error", 0.6f); return; }
            if (D.pets.Count >= GameData.PetStorage) { Game.I.Say("Pet storage full! Release a pet first (PETS window).", 2.5f); Sfx.Play("Error", 0.6f); return; }
            long cost = EggCost(map);
            if (D.coins < cost) { Game.I.Say("Not enough coins for this egg! (" + Fmt.Num(cost) + ")", 2f); Sfx.Play("Error", 0.6f); return; }
            D.coins -= cost;
            D.eggsHatched[map]++;
            int r = ItemDB.RollRarity(0.15f + 0.05f * D.rebirth);
            var pd = new PetData { rarity = r, model = GameData.MapPets[map][r], map = map };
            // se echipeaza singur daca e loc sau daca e mai bun decat cel mai slab echipat
            if (PetsEquippedCount < MaxPets) pd.equipped = true;
            else
            {
                PetData worst = null;
                foreach (var x in D.pets) if (x.equipped && (worst == null || PetPower(x) < PetPower(worst))) worst = x;
                if (worst != null && PetPower(pd) > PetPower(worst)) { worst.equipped = false; pd.equipped = true; }
            }
            D.pets.Add(pd);
            RebuildPets();
            Game.I.hud.hatch.Play(pd);   // animatia cu oul care se sparge
            Fx.Fountain(eggPos + Vector3.up * 2f, ItemDB.Col(r), 40);
            Fx.Ring(eggPos, ItemDB.Col(r), 3f);
            SaveNow();
        }

        public bool TogglePet(PetData pd)
        {
            if (pd == null) return false;
            if (!pd.equipped && PetsEquippedCount >= MaxPets)
            {
                Game.I.Say("You can equip only " + MaxPets + " pets" + (D.passGame ? "" : " (4 with the GAME PASS)") + "!", 2.5f);
                Sfx.Play("Error", 0.5f);
                return false;
            }
            pd.equipped = !pd.equipped;
            RebuildPets();
            Sfx.Play("Equip", 0.6f, 1.2f);
            SaveNow();
            return true;
        }

        public void ReleasePet(PetData pd)
        {
            if (pd == null || !D.pets.Contains(pd)) return;
            D.pets.Remove(pd);
            RebuildPets();
            Sfx.Play("Close", 0.6f);
            SaveNow();
        }

        /// <summary>Daca ai pierdut Game Pass-ul sau ai prea multi echipati, pastram doar cei mai buni.</summary>
        void FixEquippedPets()
        {
            while (PetsEquippedCount > MaxPets)
            {
                PetData worst = null;
                foreach (var x in D.pets) if (x.equipped && (worst == null || PetPower(x) < PetPower(worst))) worst = x;
                worst.equipped = false;
            }
        }

        /// <summary>INDEX: toti pets pe care ii ai (sau i-ai avut) sunt marcati ca descoperiti.</summary>
        public void DiscoverPets()
        {
            if (D == null) return;
            if (D.seenPets == null) D.seenPets = new System.Collections.Generic.List<string>();
            foreach (var pd in D.pets)
            {
                string k = pd.map + ":" + ItemDB.FixRarity(pd.rarity);
                if (!D.seenPets.Contains(k)) D.seenPets.Add(k);
            }
        }

        public void RebuildPets()
        {
            DiscoverPets();
            FixEquippedPets();
            foreach (var o in petObjs) if (o != null) Destroy(o);
            petObjs.Clear();
            int i = 0;
            foreach (var pd in D.pets)
            {
                if (!pd.equipped) continue;
                var go = new GameObject("Pet " + GameData.PetName(pd.model));
                go.transform.SetParent(SceneGroups.Get("Pets"), false);
                go.transform.position = transform.position + new Vector3(Mathf.Cos(i), 0, Mathf.Sin(i)) * 2.5f;
                var pet = go.AddComponent<Pet>();
                pet.Init(this, i, pd.rarity, pd.model);
                petObjs.Add(go);
                i++;
            }
        }

        // ---------------------------------------------------------------
        //  Rebirth
        // ---------------------------------------------------------------
        public string RebirthLabel()
        {
            if (RebirthMaxed) return "MAX REBIRTH reached (" + MaxRebirth + "/" + MaxRebirth + ")";
            if (D.level < RebirthReq) return "Rebirth " + (D.rebirth + 1) + "/" + MaxRebirth + ": requires level " + RebirthReq + "  (+25% damage, +10% coins, permanent)";
            return "[E] REBIRTH " + (D.rebirth + 1) + "/" + MaxRebirth + "  (+25% DMG, +10% coins; resets level, coins and ore)";
        }

        public void DoRebirth()
        {
            if (RebirthMaxed) { Game.I.Say("You already have the maximum of " + MaxRebirth + " rebirths!", 3f); Sfx.Play("Error", 0.6f); return; }
            if (D.level < RebirthReq) { Game.I.Say("You need level " + RebirthReq + " to Rebirth!", 3f); Sfx.Play("Error", 0.6f); return; }
            if (Time.time - rebirthAskT > 3f)
            {
                rebirthAskT = Time.time;
                Game.I.Say((Device.Mobile ? "Tap again" : "Press E again") + " to confirm Rebirth! (you keep your items)", 3f);
                return;
            }
            D.rebirth++;
            D.level = 1; D.xp = 0; D.coins = 0; D.ore = 0;
            D.lastShardLevel = 0;   // recompensele de nivel se primesc din nou
            D.statHp = D.statDmg = D.statDef = 0;   // skill points-urile se recastiga
            hp = MaxHp;
            Game.I.Travel(0, true);
            Game.I.hud.BigText("REBIRTH #" + D.rebirth + "!", new Color(1f, 0.4f, 0.9f));
            Game.I.Say("REBIRTH #" + D.rebirth + "! Permanent damage +" + (D.rebirth * 25) + "%", 5f);
            Fx.Ring(transform.position, new Color(1f, 0.4f, 0.9f), 6f, 60);
            Sfx.Play("JingleRebirth", 0.8f, 1f, 0f);
            SaveNow();
        }

        // ---------------------------------------------------------------
        //  Potiune
        // ---------------------------------------------------------------
        public void Potion()
        {
            if (Device.Mobile) return;   // pe telefon nu exista heal: singura scapare e sa fugi
            if (hp >= MaxHp) return;
            if (D.coins < PotionCost) { Game.I.Say("Not enough coins for a potion (" + PotionCost + ")", 2f); Sfx.Play("Error", 0.5f); return; }
            D.coins -= PotionCost;
            hp = Mathf.Min(MaxHp, hp + MaxHp * 0.5f);
            Game.I.hud.Pop(transform.position + Vector3.up * 2.6f, "+HP", new Color(0.4f, 1f, 0.4f), 1.2f);
            Fx.Sparkle(transform.position + Vector3.up, new Color(0.4f, 1f, 0.5f), 12, 0.8f);
            Sfx.Play("Potion", 0.7f);
        }

        // ---------------------------------------------------------------
        //  Erou (schimbare personaj)
        // ---------------------------------------------------------------
        public void NextHero() { ChangeHero(1); }
    }
}
