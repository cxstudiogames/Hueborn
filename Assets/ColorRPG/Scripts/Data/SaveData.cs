// =====================================================================
//  SaveData.cs - salvare JSON in PlayerPrefs (cu migrare din salvarea veche)
// =====================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    /// <summary>Un pet din colectie.</summary>
    [Serializable]
    public class PetData
    {
        public int rarity;
        public string model;
        public int map;          // de pe ce harta e oul (pets de pe hartile grele sunt mai puternici)
        public bool equipped;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 3;
        public int level = 1, xp;
        public long coins;
        public int ore;
        public int rebirth;
        public int hero;
        public int mapIndex;
        public List<int> petRarity = new List<int>();
        public List<string> petModel = new List<string>();
        public List<Item> items = new List<Item>();
        public int[] equipped = { 0, 0, 0, 0 };   // id-uri de item (0 = gol)
        public int nextItemId = 1;
        public float playTime;
        public int kills, bossKills;
        public int deaths;                 // de cate ori a murit eroul (statistica pe avatar)
        public int giftIndex;
        public float giftTimer;
        public bool autoAttack;

        // ---- trading / booth-uri (v3)
        public bool boothClaimed;
        public List<BoothListing> booth = new List<BoothListing>();
        public long boothEarnings;
        public int boothSales;
        public long boothTotal;
        public long lastSeen;              // unix time (secunde) - pentru vanzari offline
        public List<NpcOffer> npcStock = new List<NpcOffer>();
        public long npcRestockAt;          // unix time
        public int trades;

        // ---- personaj (v4)
        public string playerName = "";   // jucatorii noi primesc "Hero####" (Sanitize)
        public int statHp, statDmg, statDef;   // skill points investite
        public int awakenClass;                // 0 = neawakened, altfel index in GameData.Classes

        // ---- v5: pets pe harti, shards, pass-uri, servere in Trade Plaza
        public List<PetData> pets = new List<PetData>();
        public int[] eggsHatched = { 0, 0, 0, 0 };   // cate oua ai deschis pe fiecare harta (pretul creste)
        public long shards;                          // moneda rara (boss, level up, cadouri)
        public bool passTeleport;                    // Teleport Any Map
        public bool passGame;                        // Game Pass: 4 pets echipati
        public int serverId;                         // serverul curent din Trade Plaza
        public long lastStatReset;                 // ultima resetare gratuita a skill points (UTC ticks) - 1 pe zi
        public int classResets;                    // de cate ori ai schimbat clasa (pretul creste)
        public int boothServer, boothIndex = -1;     // booth-ul revendicat (server + index 0..9)
        public int lastShardLevel;                   // ultimul nivel la care ai primit shards

        // ---- v6: tokens, potiuni de boost, materiale de forja, questuri
        public long tokens;                          // moneda de trading (se cumpara si cu bani reali)
        public int[] potions = { 0, 0, 0 };          // XP x2 / DMG x1.5 / COINS x1.5 (cate ai)
        public float[] buffs = { 0f, 0f, 0f };       // secunde ramase din fiecare boost
        public int forgeStones;                      // material de forja (pica de la monstri)
        public int cloaks;                           // Guardian Cloak: forja esuata nu scade nivelul
        public List<QuestProgress> quests = new List<QuestProgress>();
        public int questsDone;
        public bool trackerHidden;

        // ---- v7: pass-uri noi, awakening progresiv, forja
        public bool passMagnet;        // strange automat tot loot-ul (altfel doar monedele)
        public bool passAuto;          // AUTO SWING (atac automat)
        public int awakenTier;         // 0..3 (de cate ori te-ai trezit)
        public bool forgeProtect;      // folosesti Guardian Cloak la forja

        // ---- v8: prieteni + server privat
        public List<string> friends = new List<string>();        // max 100 pe tot jocul
        public bool passPrivate;                                 // PRIVATE SERVER pass
        public bool bag20, bag40, bag100;                        // spatii extra in inventar (SHOP)
        public List<string> privateInvites = new List<string>(); // prietenii invitati pe serverul privat

        // ---- v9: titluri
        public List<string> titles = new List<string>();   // id-urile titlurilor deblocate
        public string title = "";                           // titlul ales ("" = automat, dupa putere)
        public int bestPlus;                                // cel mai mare nivel de forja atins vreodata
        // ---- v10: REWARDS (online azi + 7 zile)
        public string rwDay = "";                           // ziua (yyyy-MM-dd) pentru timpul online
        public float rwOnline;                              // secunde jucate azi
        public int rwClaimed;                               // bitii cadourilor online luate azi
        public int rwWeekNext;                              // urmatoarea zi din saptamana (0..6)
        public string rwWeekDate = "";                      // ziua in care ai luat ultimul cadou saptamanal
        // ---- v11: 4 raritati + 8 seturi (migrarea se face o singura data)
        public int setsV;
        public List<int> seenCat = new List<int>();   // itemele din catalog descoperite (INDEX)
        public List<string> seenPets = new List<string>();   // pets descoperiti (INDEX): "harta:raritate"
    }

    /// <summary>Un quest acceptat (harta + index in lista hartii).</summary>
    [Serializable]
    public class QuestProgress
    {
        public int map, index, progress;
        public bool tracked = true;
    }

    public static class SaveSystem
    {
        const string KEY = "crpg_save_v2";

        public static void Save(SaveData d)
        {
            try
            {
                string json = JsonUtility.ToJson(d);
                PlayerPrefs.SetString(KEY, json);
                PlayerPrefs.SetString(KEY + "_time", DateTime.UtcNow.Ticks.ToString());
                PlayerPrefs.Save();
                OnlineAccount.QueueCloudSave(json);   // pe telefon: si in cloud (Google Play), cand esti conectat
            }
            catch (Exception e) { Debug.LogWarning("[ColorRPG] Save failed: " + e.Message); }
        }

        /// <summary>Salvarea locala ca text (pentru cloud).</summary>
        public static string RawJson { get { return PlayerPrefs.GetString(KEY, ""); } }

        /// <summary>Inlocuieste salvarea locala cu cea din cloud (jocul se reporneste dupa).</summary>
        public static bool ReplaceWith(string json)
        {
            try
            {
                var d = JsonUtility.FromJson<SaveData>(json);
                if (d == null) return false;
                PlayerPrefs.SetString(KEY, json);
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception) { return false; }
        }

        public static SaveData Load()
        {
            if (PlayerPrefs.HasKey(KEY))
            {
                try
                {
                    var d = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(KEY));
                    if (d != null) return Sanitize(d);
                }
                catch (Exception e) { Debug.LogWarning("[ColorRPG] Corrupt save, starting fresh: " + e.Message); }
            }
            return Sanitize(Migrate());
        }

        public static void Wipe()
        {
            PlayerPrefs.DeleteKey(KEY);
            PlayerPrefs.Save();
        }

        /// <summary>Converteste salvarea din versiunea initiala (chei crpg_*).</summary>
        static SaveData Migrate()
        {
            var d = new SaveData();
            if (!PlayerPrefs.HasKey("crpg_level")) return d;
            d.level = Mathf.Max(1, PlayerPrefs.GetInt("crpg_level", 1));
            d.xp = PlayerPrefs.GetInt("crpg_xp", 0);
            d.coins = PlayerPrefs.GetInt("crpg_coins", 0);
            d.ore = PlayerPrefs.GetInt("crpg_ore", 0);
            d.rebirth = PlayerPrefs.GetInt("crpg_rebirth", 0);
            int w = Mathf.Clamp(PlayerPrefs.GetInt("crpg_wlevel", 0), 0, 30);
            string s = PlayerPrefs.GetString("crpg_pets", "");
            if (s.Length > 0)
                foreach (var t in s.Split(','))
                {
                    int r;
                    if (int.TryParse(t, out r))
                    {
                        r = Mathf.Clamp(r, 0, 4);
                        d.petRarity.Add(r);
                        var list = GameData.PetModels[r];
                        d.petModel.Add(list[UnityEngine.Random.Range(0, list.Length)]);
                    }
                }
            // sabia veche +N devine o arma cu raritatea corespunzatoare
            var it = ItemDB.Make(Slot.Weapon, Mathf.Min(4, w / 5), 0);
            it.plus = Mathf.Min(ItemDB.MaxPlus, w);
            it.name = "Old Trusty Sword";
            d.items.Add(it);
            d.equipped[0] = it.id;
            d.nextItemId = ItemDB.NextId;
            Debug.Log("[ColorRPG] Old save migrated to v2.");
            return d;
        }

        static SaveData Sanitize(SaveData d)
        {
            if (d.items == null) d.items = new List<Item>();
            if (d.petRarity == null) d.petRarity = new List<int>();
            if (d.petModel == null) d.petModel = new List<string>();
            while (d.petModel.Count < d.petRarity.Count) d.petModel.Add(GameData.PetModels[0][0]);
            if (d.pets == null) d.pets = new List<PetData>();
            // pets din versiunea veche -> colectia noua (primii 2 echipati)
            if (d.petRarity.Count > 0 && d.pets.Count == 0)
            {
                for (int i = 0; i < d.petRarity.Count; i++)
                {
                    int r = Mathf.Clamp(d.petRarity[i], 0, 5);
                    d.pets.Add(new PetData { rarity = r, model = GameData.MapPets[0][r], map = 0, equipped = i < GameData.PetsEquipped });
                }
                d.petRarity.Clear();
                d.petModel.Clear();
            }
            d.pets.RemoveAll(pp => pp == null || string.IsNullOrEmpty(pp.model));
            foreach (var pp in d.pets) { pp.rarity = Mathf.Clamp(pp.rarity, 0, 5); pp.map = Mathf.Clamp(pp.map, 0, GameData.MapPets.Length - 1); }
            if (d.eggsHatched == null || d.eggsHatched.Length < 4)
            {
                var e = new int[4];
                if (d.eggsHatched != null) for (int i = 0; i < d.eggsHatched.Length && i < 4; i++) e[i] = d.eggsHatched[i];
                d.eggsHatched = e;
            }
            d.shards = Math.Max(0, d.shards);
            { int f5 = d.level / 5 * 5; if (d.lastShardLevel < f5) d.lastShardLevel = f5; }   // recompensa de la fiecare 5 niveluri nu se mai sare
            if (d.equipped == null || d.equipped.Length != 4) d.equipped = new[] { 0, 0, 0, 0 };
            if (d.booth == null) d.booth = new List<BoothListing>();
            if (d.npcStock == null) d.npcStock = new List<NpcOffer>();
            // listarile de pets / materiale nu au item (JsonUtility pune un item gol) - le pastram
            d.booth.RemoveAll(b => b == null
                || (b.kind == 0 && (b.item == null || string.IsNullOrEmpty(b.item.name)))
                || (b.kind == 1 && (b.pet == null || string.IsNullOrEmpty(b.pet.model)))
                || (b.kind == 2 && b.amount <= 0)
                || b.kind < 0 || b.kind > 2);
            d.npcStock.RemoveAll(o => o == null || o.item == null || o.item.name == null || o.item.name.Length == 0);
            d.level = Mathf.Clamp(d.level, 1, GameData.MaxLevel);   // nivelul maxim (100)
            d.rebirth = Mathf.Clamp(d.rebirth, 0, Player.MaxRebirth);
            if (string.IsNullOrEmpty(d.playerName)) d.playerName = "Hero" + UnityEngine.Random.Range(1000, 10000);
            d.statHp = Mathf.Max(0, d.statHp); d.statDmg = Mathf.Max(0, d.statDmg); d.statDef = Mathf.Max(0, d.statDef);
            if (d.awakenClass < 0 || d.awakenClass >= GameData.Classes.Length) d.awakenClass = 0;
            // awakening progresiv (v7): cine avea deja o clasa e la treapta I
            if (d.awakenClass > 0 && d.awakenTier <= 0) d.awakenTier = 1;
            if (d.awakenClass == 0 && d.classResets == 0) d.awakenTier = 0;   // dupa CHANGE CLASS treapta ramane pana alegi clasa noua
            if (d.friends == null) d.friends = new List<string>();
            if (d.titles == null) d.titles = new List<string>();
            if (d.title == null) d.title = "";
            if (d.privateInvites == null) d.privateInvites = new List<string>();
            d.privateInvites.RemoveAll(f => !d.friends.Contains(f));
            d.awakenTier = Mathf.Clamp(d.awakenTier, 0, GameData.MaxAwaken);
            d.coins = Math.Max(0, d.coins);
            d.ore = Mathf.Max(0, d.ore);
            // iteme invalide (slot / raritate / tier / forja in afara limitelor) + id-uri duplicate
            d.items.RemoveAll(it => it == null || it.slot < 0 || it.slot > 3 || string.IsNullOrEmpty(it.name));
            // 4 raritati: Uncommon -> Rare, Epic -> Legendary (iteme, booth, pets - pets primesc modelul raritatii noi)
            foreach (var it in d.items) ItemDB.FixItemRarity(it);
            foreach (var b in d.booth)
            {
                if (b == null) continue;
                if (b.kind == 0 && b.item != null) ItemDB.FixItemRarity(b.item);
                if (b.kind == 1 && b.pet != null && b.pet.rarity != ItemDB.FixRarity(b.pet.rarity)) { b.pet.rarity = ItemDB.FixRarity(b.pet.rarity); b.pet.model = GameData.MapPets[Mathf.Clamp(b.pet.map, 0, GameData.MapPets.Length - 1)][b.pet.rarity]; }
            }
            foreach (var o in d.npcStock) if (o != null && o.item != null) ItemDB.FixItemRarity(o.item);
            foreach (var pp in d.pets)
                if (pp.rarity != ItemDB.FixRarity(pp.rarity)) { pp.rarity = ItemDB.FixRarity(pp.rarity); pp.model = GameData.MapPets[pp.map][pp.rarity]; }
            // v12: catalogul de iteme (6 iteme unice pe categorie pe harta, fara seturi) - orice item vechi devine echivalentul lui
            foreach (var it in d.items) ItemDB.ToCatalog(it);
            foreach (var b in d.booth) if (b != null && b.kind == 0 && b.item != null) ItemDB.ToCatalog(b.item);
            foreach (var o in d.npcStock) if (o != null && o.item != null) ItemDB.ToCatalog(o.item);
            if (d.seenCat == null) d.seenCat = new List<int>();
            if (d.seenPets == null) d.seenPets = new List<string>();
            foreach (var it in d.items) if (it.cat > 0 && !d.seenCat.Contains(it.cat)) d.seenCat.Add(it.cat);
            var seenIds = new HashSet<int>();
            foreach (var it in d.items)
            {
                it.rarity = Mathf.Clamp(it.rarity, 0, 5);
                it.tier = Mathf.Clamp(it.tier, 0, ItemDB.MaxTier);
                it.plus = Mathf.Clamp(it.plus, 0, ItemDB.MaxPlus);
                if (it.roll <= 0f) it.roll = 1f;
                if (it.id <= 0 || !seenIds.Add(it.id)) it.id = 0;   // primeste id nou mai jos
            }
            int maxId = d.nextItemId;
            foreach (var it in d.items) maxId = Mathf.Max(maxId, it.id + 1);
            foreach (var b in d.booth) if (b.item != null) maxId = Mathf.Max(maxId, b.item.id + 1);
            foreach (var o in d.npcStock) maxId = Mathf.Max(maxId, o.item.id + 1);
            ItemDB.SetNextId(maxId);
            foreach (var it in d.items) if (it.id == 0) it.id = ItemDB.NextIdTake();
            // prima pornire: sabie de lemn
            if (d.items.Count == 0 && d.equipped[0] == 0)
            {
                var st = ItemDB.Starter();
                d.items.Add(st);
                d.equipped[0] = st.id;
            }
            // echipamentul trebuie sa existe in inventar
            for (int i = 0; i < 4; i++)
                if (d.equipped[i] != 0 && d.items.Find(x => x.id == d.equipped[i]) == null) d.equipped[i] = 0;
            d.mapIndex = Mathf.Clamp(d.mapIndex, 0, 3);
            // v6
            if (d.potions == null || d.potions.Length != 3) d.potions = new[] { 0, 0, 0 };
            if (d.buffs == null || d.buffs.Length != 3) d.buffs = new[] { 0f, 0f, 0f };
            for (int i = 0; i < 3; i++) { d.potions[i] = Mathf.Max(0, d.potions[i]); d.buffs[i] = Mathf.Clamp(d.buffs[i], 0f, 3600f); }
            if (d.quests == null) d.quests = new List<QuestProgress>();
            d.quests.RemoveAll(q => q == null || q.map < 0 || q.map > 3 || q.index < 0 || q.index >= QuestDB.ForMap(q.map).Length);
            if (d.quests.Count > QuestDB.MaxActive) d.quests.RemoveRange(QuestDB.MaxActive, d.quests.Count - QuestDB.MaxActive);   // acum: o singura misiune odata
            d.tokens = Math.Max(0, d.tokens);
            d.forgeStones = Mathf.Max(0, d.forgeStones);
            d.cloaks = Mathf.Max(0, d.cloaks);
            if (d.version < 3)
            {
                // booth-urile folosesc acum TOKENS: castigurile vechi (coins) se dau direct, preturile se recalculeaza
                d.coins += Math.Max(0, d.boothEarnings);
                d.boothEarnings = 0;
                foreach (var b in d.booth) if (b.item != null) b.price = TradeDB.TokenValue(b.item);
                d.version = 3;
            }
            if (d.boothIndex < -1 || d.boothIndex > 9) d.boothIndex = -1;
            return d;
        }
    }
}
