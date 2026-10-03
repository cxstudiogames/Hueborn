// =====================================================================
//  HUD.cs - interfata jocului pe Canvas (inlocuieste vechiul OnGUI):
//  bare HP/XP, monede, butoane, prompt, mesaje, numere plutitoare,
//  bare de viata inamici, bara de boss, etichete in lume, notificari.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColorRPG
{
    public class HUD : MonoBehaviour
    {
        Canvas canvas;
        RectTransform root;
        Game game;
        Camera cam;

        // panou jucator
        Text coinText, shardText, tokenText, autoText, pointsText, serverText, serverBtnText;
        // potiuni + boost-uri active + quest tracker
        readonly RectTransform[] potSlot = new RectTransform[3];
        readonly Text[] potTime = new Text[3];
        // bara de skill-uri (1 / 2 / 3) - stanga barelor HP/XP
        readonly Image[] skillIcon = new Image[3], skillCdImg = new Image[3];
        readonly Text[] skillCdText = new Text[3], skillName = new Text[3];
        RectTransform skillBar;
        int skillHero = -1;
        RectTransform trackerRt, trackerBody, trackerOpenBtn;
        Text trackerTitle;
        string trackerKey = "";
        UIKit.Bar hpBar, xpBar, bossBar;
        Text bossName, lvlBarText;
        RectTransform coinPill, pointsBadge, leaveBtn, statsBtn, serverBar;
        float serverConfirmT;
        float coinPulse;
        Image autoBtnImg;
        Text nameplate, npTitle, npPower;
        string npTitleKey = "";
        RectTransform nameplateRt;
        Image npFlame;

        // prompt / mesaje
        RectTransform promptRt; Text promptText; Image promptImg;
        Sprite promptBtnSprite, promptFlatSprite; float promptW; bool promptLootStyle;
        RectTransform msgRt; Text msgText; float msgT;
        Text bigText; float bigT; Color bigCol;
        // banner LEVEL UP (panou ~70% cu titlu auriu)
        RectTransform lvlRt; CanvasGroup lvlCg; Text lvlTitle, lvlSub; Image lvlShine; float lvlT;
        Image flash, fade; float flashA, fadeT, fadeDur;

        // pool-uri
        class PopItem { public Text t; public RectTransform rt; public Vector3 pos; public float age, life, scale; public Color col; public bool on; }
        readonly List<PopItem> pops = new List<PopItem>();
        class EBar { public RectTransform rt; public Image fill; public Text name; public Enemy owner; }
        readonly List<EBar> ebars = new List<EBar>();
        RectTransform popLayer, barLayer, labelLayer, toastLayer;
        class WLabel { public Vector3 pos; public Text t; public float maxDist; }
        readonly List<WLabel> labels = new List<WLabel>();
        class ToastItem { public RectTransform rt; public CanvasGroup cg; public float age; }
        readonly List<ToastItem> toasts = new List<ToastItem>();

        public InventoryUI inventory;
        /// <summary>Ascunde / arata tot HUD-ul (filmarea pentru reclama).</summary>
        public void SetCanvasVisible(bool v) { if (canvas != null) canvas.enabled = v; }
        public TradeUI trade;
        public CharacterUI character;
        public AwakeningUI awakening;
        public TeleportUI teleport;
        public PetsUI pets;
        public ShopUI shop;
        public FriendsUI friends;
        public RewardsUI rewards;
        public IndexUI index;
        RectTransform rewardsDot;
        public AdminUI admin;
        public QuestUI quests;
        public TradeHubUI tradeHub;
        public ForgeUI forge;
        public EggHatchUI hatch;
        public ChatUI chat;
        public TitlesUI titles;
        public GuideUI guide;
        public StatsUI stats;
        public SettingsUI settings;
        public bool Blocking
        {
            get
            {
                return (inventory != null && inventory.IsOpen) || (trade != null && trade.IsOpen) ||
                       (character != null && character.IsOpen) || (awakening != null && awakening.IsOpen) || (stats != null && stats.IsOpen) || (settings != null && settings.IsOpen) ||
                       (teleport != null && teleport.IsOpen) || (pets != null && pets.IsOpen) || (shop != null && shop.IsOpen) ||
                       (admin != null && admin.IsOpen) || (quests != null && quests.IsOpen) || (tradeHub != null && tradeHub.IsOpen) ||
                       (forge != null && forge.IsOpen) || (hatch != null && hatch.IsOpen) || (titles != null && titles.IsOpen) || (guide != null && guide.IsOpen) ||
                       (friends != null && friends.IsOpen) || (rewards != null && rewards.IsOpen) || (index != null && index.IsOpen);
            }
        }

        public static HUD Create(Game g)
        {
            var go = new GameObject("HUD");
            go.transform.SetParent(SceneGroups.Get("Systems"), false);
            var h = go.AddComponent<HUD>();
            h.game = g;
            h.Build();
            return h;
        }

        // ---------------------------------------------------------------
        //  Constructie
        // ---------------------------------------------------------------
        void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var sc = gameObject.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            root = (RectTransform)transform;
            EnsureEventSystem();

            popLayer = UIKit.Stretch(root, "Pops");
            barLayer = UIKit.Stretch(root, "EnemyBars");
            labelLayer = UIKit.Stretch(root, "Labels");
            barLayer.SetAsFirstSibling(); labelLayer.SetAsFirstSibling();

            Vector2 TL = new Vector2(0, 1), TR = new Vector2(1, 1), BL = new Vector2(0, 0), BR = new Vector2(1, 0), TC = new Vector2(0.5f, 1), BC = new Vector2(0.5f, 0), C = new Vector2(0.5f, 0.5f);

            // ---- stanga-sus: doar imaginea statica a personajului (click = CHARACTER), fara chenar
            var card = UIKit.Rect(root, "Avatar", TL, TL, new Vector2(14, -10), new Vector2(140, 140));
            var raw = card.gameObject.AddComponent<RawImage>();
            raw.texture = game.portrait != null ? game.portrait.Snapshot : null;   // poza statica
            raw.uvRect = new Rect(0.14f, 0.24f, 0.72f, 0.72f);
            raw.raycastTarget = true;
            var cardBtn = card.gameObject.AddComponent<Button>();
            cardBtn.targetGraphic = raw;
            cardBtn.transition = Selectable.Transition.None;
            cardBtn.onClick.AddListener(() => { Sfx.Play("Click", 0.5f); character.Toggle(); });

            // ---- chat (GLOBAL / SERVER) langa avatar
            chat = ChatUI.Create(root, game);


            // ---- stanga: butonul STATS (+ badge cu punctele libere); coloana coborata (pe telefon nu mai atinge avatarul)
            float ly = Device.Mobile ? -75f : -30f;
            statsBtn = (RectTransform)UIKit.SpriteButton(root, "BtnStats", 34, Color.white, "Yellow", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 150 + ly), 96, () => stats.Toggle()).transform;
            UIKit.Text(root, "BtnStatsL", "STATS", 19, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(72, 90 + ly), new Vector2(160, 24), true, true);
            UIKit.SpriteButton(root, "BtnPets", 6, new Color(1f, 0.75f, 0.95f), "Blue", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 10 + ly), 96, () => pets.Toggle());
            UIKit.Text(root, "BtnPetsL", "PETS", 19, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(72, -50 + ly), new Vector2(160, 24), true, true);
            UIKit.SpriteButton(root, "BtnShop", 27, Color.white, "Green", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, -130 + ly), 96, () => shop.Toggle());
            UIKit.Text(root, "BtnShopL", "SHOP", 19, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(72, -190 + ly), new Vector2(160, 24), true, true);
            // FRIENDS: cauti jucatori, ii adaugi, le scrii mesaje
            UIKit.SpriteButton(root, "BtnFriends", 39, Color.white, "Purple", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, -270 + ly), 96, () => friends.Toggle());
            UIKit.Text(root, "BtnFriendsL", "FRIENDS", 19, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(72, -330 + ly), new Vector2(160, 24), true, true);
            // REWARDS (online azi + 7 zile) - deasupra lui STATS, cu punct rosu cand ai ceva de luat
            var rwBtn = (RectTransform)UIKit.SpriteButton(root, "BtnRewards", 7, Color.white, "Orange", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 290 + ly), 96, () => rewards.Toggle()).transform;
            UIKit.Text(root, "BtnRewardsL", "REWARDS", 19, Color.white, TextAnchor.MiddleCenter, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(72, 230 + ly), new Vector2(160, 24), true, true);
            rewardsDot = UIKit.Rect(rwBtn, "Dot", TR, C, new Vector2(-8, -8), new Vector2(32, 32));
            UIKit.Img(rewardsDot, "round_Red", Color.white, 16f).raycastTarget = false;
            pointsBadge = UIKit.Rect(statsBtn, "Points", TR, C, new Vector2(-6, -6), new Vector2(44, 44));
            UIKit.Img(pointsBadge, "round_Red", Color.white, 16f);
            pointsText = UIKit.Text(pointsBadge, "T", "0", 20, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 2), new Vector2(44, 40));

            // ---- dreapta-sus: setari (coltul) + monede, minereu, pets
            UIKit.SpriteButton(root, "BtnSettings", 28, Color.white, "Blue", TR, TR, new Vector2(-20, -20), 84, () => settings.Toggle());
            // GUIDE: tot ce trebuie sa stii despre joc, pe sectiuni (pass-uri, forja, pets, trading...)
            UIKit.SpriteButton(root, "BtnGuide", 29, Color.white, "Yellow", TR, TR, new Vector2(-114, -20), 84, () => guide.Toggle());
            // ---- dreapta-jos: banii si materialele (iconita in dreapta, textul spre stanga)
            // doar Coins / Shards / Tokens (restul - minereu, pietre, cloaks, pets - sunt in INVENTORY)
            // pe telefon: monedele urca langa coloana din dreapta (jos sunt FIGHT / JUMP)
            bool mob = Device.Mobile;
            // monedele: coltul dreapta-jos (pe telefon deasupra FIGHT, la stanga coloanei de butoane)
            float pillX = mob ? -190f : -24f, pillY = mob ? 300f : 18f;
            coinPill = Pill(root, "Coins", new Vector2(pillX, pillY), new Color(1f, 0.85f, 0.2f), 4, out coinText);
            Pill(root, "Shards", new Vector2(pillX, pillY + 66), new Color(0.6f, 0.85f, 1f), 9, out shardText);
            Pill(root, "Tokens", new Vector2(pillX, pillY + 132), new Color(0.55f, 0.95f, 0.75f), 19, out tokenText);

            // ---- dreapta-sus: quest tracker (se poate ascunde)
            trackerRt = UIKit.Rect(root, "QuestTracker", TR, TR, new Vector2(-234, -24), new Vector2(460, 60));
            UIKit.Img(trackerRt, "flat_Grey", new Color(0.08f, 0.1f, 0.18f, 0.78f), 16f, 1.2f);
            trackerTitle = UIKit.Text(trackerRt, "Title", "QUEST", 20, new Color(1f, 0.85f, 0.35f), TextAnchor.MiddleLeft, TL, TL, new Vector2(14, -6), new Vector2(300, 30), true, true);
            UIKit.Button(trackerRt, "Hide", "HIDE", "Grey", TR, TR, new Vector2(-8, -5), new Vector2(92, 36), () => { game.player.D.trackerHidden = true; trackerKey = ""; }, 15);
            trackerBody = UIKit.Rect(trackerRt, "Body", TL, TL, new Vector2(0, -40), new Vector2(460, 10));
            trackerOpenBtn = (RectTransform)UIKit.Button(root, "QuestShow", "QUEST", "Yellow", TR, TR, new Vector2(-214, -24), new Vector2(180, 56), () => { game.player.D.trackerHidden = false; trackerKey = ""; }, 20).transform;
            if (Device.Mobile)
            {
                // pe telefon: questurile mai mari (usor de citit si de apasat)
                trackerRt.localScale = Vector3.one * 1.3f;
                trackerOpenBtn.localScale = Vector3.one * 1.45f;
            }

            // ---- butoane dreapta (pe telefon: mai sus si putin mai mici, ca sa nu atinga FIGHT / JUMP de jos)
            // mai departe una de alta si putin mai la stanga
            float[] ry = mob ? new[] { 290f, 160f, 30f, -100f } : new[] { 270f, 120f, -30f, -180f };
            const float rx = -48f, rlx = -96f;
            float rs = mob ? 88f : 96f, rl = mob ? 56f : 60f;
            UIKit.SpriteButton(root, "BtnTrade", 21, new Color(1f, 0.85f, 0.55f), "Orange", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(rx, ry[0]), rs, () => tradeHub.Toggle());
            UIKit.Text(root, "BtnTradeL", "TRADING", 22, Color.white, TextAnchor.MiddleCenter, new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(rlx, ry[0] - rl), new Vector2(170, 28), true, true);
            UIKit.SpriteButton(root, "BtnTele", 8, new Color(0.75f, 0.9f, 1f), "Purple", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(rx, ry[1]), rs, () => teleport.Toggle());
            UIKit.Text(root, "BtnTeleL", "TELEPORT", 22, Color.white, TextAnchor.MiddleCenter, new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(rlx, ry[1] - rl), new Vector2(170, 28), true, true);
            UIKit.SpriteButton(root, "BtnInv", 10, new Color(1f, 0.85f, 0.5f), "Blue", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(rx, ry[2]), rs, () => ToggleInventory(false));
            UIKit.Text(root, "BtnInvL", "INVENTORY", 22, Color.white, TextAnchor.MiddleCenter, new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(rlx, ry[2] - rl), new Vector2(170, 28), true, true);
            UIKit.SpriteButton(root, "BtnAuto", 25, Color.white, "Red", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(rx, ry[3]), rs, ToggleAuto);
            autoBtnImg = root.Find("BtnAuto").GetComponent<Image>();
            autoText = UIKit.Text(root, "BtnAutoL", "AUTO SWING", 22, Color.white, TextAnchor.MiddleCenter, new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(rlx, ry[3] - rl), new Vector2(190, 28), true, true);

            // ---- jos-centru: HP + XP
            var bars = UIKit.Rect(root, "Bars", BC, BC, new Vector2(0, 16), new Vector2(720, 96));
            UIKit.Img(bars, "flat_Grey", new Color(0.08f, 0.1f, 0.18f, 0.7f), 16f, 1.2f);
            hpBar = UIKit.MakeBar(bars, "HP", TC, TC, new Vector2(0, -12), new Vector2(690, 38), new Color(1f, 0.3f, 0.35f), 20);
            xpBar = UIKit.MakeBar(bars, "XP", BC, BC, new Vector2(0, 12), new Vector2(690, 24), new Color(0.3f, 0.8f, 1f), 15, false);
            lvlBarText = UIKit.Text(bars, "Lv", "", 22, new Color(1f, 0.9f, 0.5f), TextAnchor.MiddleRight, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(160, 40));

            BuildPotionBar(root);
            BuildSkillBar(root);

            // ---- Trade Plaza: butonul X (inapoi la prima harta)
            leaveBtn = (RectTransform)UIKit.IconButton(root, "BtnLeave", "cross", "Red", TC, TC, new Vector2(0, -16), 84, LeavePlaza, out _).transform;
            UIKit.Text(leaveBtn, "L", "LEAVE", 16, Color.white, TextAnchor.MiddleCenter, BC, TC, new Vector2(0, -2), new Vector2(160, 24), true, true);
            leaveBtn.gameObject.SetActive(false);
            // ---- Trade Plaza: serverul curent + CHANGE SERVER
            serverBar = UIKit.Rect(root, "ServerBar", TC, TC, new Vector2(0, -122), new Vector2(560, 76));
            UIKit.Img(serverBar, "flat_Grey", new Color(0.08f, 0.1f, 0.18f, 0.8f), 16f, 1.2f);
            serverText = UIKit.Text(serverBar, "T", "", 18, Color.white, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(300, 68), true, true);
            serverText.supportRichText = true;
            var sbtn = UIKit.Button(serverBar, "Change", "CHANGE SERVER", "Blue", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(190, 58), ChangeServerClick, 16);
            serverBtnText = sbtn.GetComponentInChildren<Text>();
            serverBar.gameObject.SetActive(false);

            // ---- deasupra jucatorului: TITLUL (dupa putere), "[W] nume" si flacara + POWER
            nameplateRt = UIKit.Rect(labelLayer, "Nameplate", Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(600, 120));
            npTitle = UIKit.Text(nameplateRt, "Title", "", 24, Color.white, TextAnchor.MiddleCenter, TC, TC, new Vector2(0, 0), new Vector2(600, 32));
            nameplate = UIKit.Text(nameplateRt, "Name", "", 28, Color.white, TextAnchor.MiddleCenter, TC, TC, new Vector2(0, -32), new Vector2(600, 36));
            nameplate.supportRichText = true;
            var pw = UIKit.Rect(nameplateRt, "Power", TC, TC, new Vector2(0, -70), new Vector2(300, 40));
            npFlame = UIKit.Rect(pw, "Flame", new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0, 0), new Vector2(36, 36)).gameObject.AddComponent<Image>();
            npFlame.sprite = ItemIcons.Get(22); ItemIcons.Art(npFlame); npFlame.color = new Color(1f, 0.55f, 0.15f); npFlame.raycastTarget = false;
            npPower = UIKit.Text(pw, "T", "", 28, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4, 1), new Vector2(240, 40));

            // ---- bara boss
            bossBar = UIKit.MakeBar(root, "BossBar", TC, TC, new Vector2(0, -150), new Vector2(720, 34), new Color(0.85f, 0.3f, 1f), 18);
            bossName = UIKit.Text(bossBar.root, "Name", "", 26, new Color(1f, 0.85f, 0.9f), TextAnchor.MiddleCenter, TC, BC, new Vector2(0, 4), new Vector2(720, 34));
            bossBar.root.gameObject.SetActive(false);

            // ---- mesaje
            msgRt = UIKit.Rect(root, "Msg", TC, TC, new Vector2(0, -101), new Vector2(900, 64));
            UIKit.Img(msgRt, "flat_Grey", new Color(0.08f, 0.1f, 0.18f, 0.7f), 16f, 1.2f);
            msgText = UIKit.Text(msgRt, "T", "", 26, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 2), new Vector2(880, 60));
            msgRt.gameObject.SetActive(false);

            bigText = UIKit.Text(root, "Big", "", 78, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 170), new Vector2(1400, 120));
            bigText.gameObject.SetActive(false);

            // ---- prompt interactiune (deasupra barelor HP/XP)
            promptRt = UIKit.Rect(root, "Prompt", BC, BC, new Vector2(0, Device.Mobile ? 352 : 290), new Vector2(1000, Device.Mobile ? 80 : 66));
            promptImg = UIKit.Img(promptRt, "btn_Yellow", Color.white, 18f, 1f);
            promptBtnSprite = promptImg.sprite;
            promptFlatSprite = UIKit.Sprite("flat_Grey", 18f);
            promptW = promptRt.sizeDelta.x;
            if (Device.Mobile)
            {
                // pe telefon: apesi pe mesaj = interactionezi (in loc de tasta E)
                promptImg.raycastTarget = true;
                var pb = promptRt.gameObject.AddComponent<Button>();
                pb.targetGraphic = promptImg;
                pb.onClick.AddListener(MobileControls.TapInteract);
            }
            promptText = UIKit.Text(promptRt, "T", "", 24, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 4), new Vector2(1060, 60));
            promptText.horizontalOverflow = HorizontalWrapMode.Wrap;
            promptText.resizeTextForBestFit = true; promptText.resizeTextMinSize = 12; promptText.resizeTextMaxSize = 28;
            promptRt.gameObject.SetActive(false);

            // notificarile (rank nou, iteme, vanzari...) - in centrul de sus, sub bara de boss, unde nu acopera butoanele
            toastLayer = UIKit.Rect(root, "Toasts", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -196), new Vector2(660, 330));
            if (Device.Mobile) MobileControls.Create(root);   // joystick + FIGHT + JUMP (doar pe telefon)

            // ---- overlay-uri
            var fr = UIKit.Stretch(root, "Flash");
            flash = fr.gameObject.AddComponent<Image>();
            flash.color = new Color(1f, 0.1f, 0.1f, 0f);
            flash.raycastTarget = false;
            var fd = UIKit.Stretch(root, "Fade");
            fade = fd.gameObject.AddComponent<Image>();
            fade.color = new Color(0, 0, 0, 0);
            fade.raycastTarget = false;

            inventory = InventoryUI.Create(root, game);
            trade = TradeUI.Create(root, game);
            character = CharacterUI.Create(root, game);
            awakening = AwakeningUI.Create(root, game);
            stats = StatsUI.Create(root, game);
            settings = SettingsUI.Create(root, game);
            teleport = TeleportUI.Create(root, game);
            pets = PetsUI.Create(root, game);
            shop = ShopUI.Create(root, game);
            friends = FriendsUI.Create(root, game);
            rewards = RewardsUI.Create(root, game);
            index = IndexUI.Create(root, game);
            quests = QuestUI.Create(root, game);
            tradeHub = TradeHubUI.Create(root, game);
            forge = ForgeUI.Create(root, game);
            hatch = EggHatchUI.Create(root, game);
            titles = TitlesUI.Create(root, game);
            guide = GuideUI.Create(root, game);
            admin = AdminUI.Create(root, game);
            QuestGuide.Create();
            // butonul ADMIN: doar pentru admin (editor, contul tau online sau dispozitiv deblocat cu codul) - vezi AdminAccess.cs
            // creat ultimul ca sa nu fie acoperit de alte elemente ale HUD-ului; apare / dispare singur
            UIKit.Button(root, "BtnAdmin", "ADMIN", "Red", TR, TR, new Vector2(-20, -112), new Vector2(84, 44), () => { if (AdminAccess.IsAdmin) admin.Toggle(); }, 15);
            ItemTooltip.Create(root);   // chenarul cu statisticile itemelor (hover)
        }

        void ChangeServerClick()
        {
            var ts = TradeSystem.I;
            if (ts == null) return;
            if (ts.HasBooth && serverConfirmT <= 0f)
            {
                serverConfirmT = 3f;
                Say("You have a booth on this server! Click CHANGE SERVER again to leave it (your items stay safe).", 3f);
                Sfx.Play("Error", 0.4f, 1.3f);
                return;
            }
            serverConfirmT = 0f;
            ts.ChangeServer();
            if (trade != null && trade.IsOpen) trade.Close();
        }

        void LeavePlaza()
        {
            if (game.InPlaza) game.Travel(0, true);
        }

        /// <summary>Textul de deasupra capului: (A) 32 Ionutcx</summary>
        public static string NameplateText(Player p)
        {
            var D = p.D;
            string s = "";
            if (D.awakenClass > 0)
            {
                s += GameData.ClassTagRich(D.awakenClass, D.awakenTier, 28);
            }
            s += D.playerName + " <color=#b8c4d6>Lv " + D.level + "</color>";
            return s;
        }

        /// <summary>O moneda in coltul din dreapta-jos: iconita mare in dreapta, numarul (mare, cu contur) spre stanga. Fara chenar.</summary>
        RectTransform Pill(RectTransform parent, string n, Vector2 pos, Color c, int icon, out Text txt)
        {
            var rt = UIKit.Rect(parent, n, new Vector2(1, 0), new Vector2(1, 0), pos, new Vector2(300, 62));
            var ic = UIKit.Rect(rt, "Icon", new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-30, 0), new Vector2(60, 60));
            var img = ic.gameObject.AddComponent<Image>();
            img.sprite = ItemIcons.Get(icon); ItemIcons.Art(img);
            img.color = c;
            img.raycastTarget = false;
            var sh = ic.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(2, -3);
            txt = UIKit.Text(rt, "T", "0", 38, Color.white, TextAnchor.MiddleRight, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-68, 2), new Vector2(240, 60));
            return rt;
        }

        /// <summary>Potiunile ACTIVE (jos-dreapta): iconita + timpul ramas. Apar doar cat timp sunt active.
        /// Le activezi din INVENTORY (MATERIALS).</summary>
        void BuildPotionBar(RectTransform root)
        {
            Vector2 TC = new Vector2(0.5f, 1), BC = new Vector2(0.5f, 0), C = new Vector2(0.5f, 0.5f), BR = new Vector2(1, 0);
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                var rt = UIKit.Rect(root, "Potion" + i, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(Device.Mobile ? -430 - i * 100 : 430 + i * 100, 14), new Vector2(92, 104));
                var ic = UIKit.Rect(rt, "I", TC, TC, new Vector2(0, 0), new Vector2(76, 76)).gameObject.AddComponent<Image>();
                ic.sprite = ItemIcons.Get(Player.PotionIcons[i]); ItemIcons.Art(ic); ic.raycastTarget = true;
                TextHover.Attach(ic.gameObject, () => "<size=26><color=#" + ColorUtility.ToHtmlStringRGB(Player.PotionCols[k]) + ">" + Player.PotionNames[k] + "</color></size>\n" + Player.PotionShort[k] + " - active", Player.PotionCols[k]);
                potTime[i] = UIKit.Text(rt, "T", "", 20, Player.PotionCols[i], TextAnchor.MiddleCenter, BC, BC, new Vector2(0, 2), new Vector2(110, 28), true, true);
                potSlot[i] = rt;
                rt.gameObject.SetActive(false);
            }
        }

        /// <summary>Skill-urile (1 / 2 / 3) deasupra barei de HP: doar iconitele (fara chenar). In cooldown iconita e
        /// stinsa si se "reumple" circular, cu secundele ramase pe ea.</summary>
        void BuildSkillBar(RectTransform root)
        {
            Vector2 TC = new Vector2(0.5f, 1), BC = new Vector2(0.5f, 0), C = new Vector2(0.5f, 0.5f), BR = new Vector2(1, 0);
            skillBar = UIKit.Rect(root, "Skills", new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);
            string[] keys = { Device.Key("1"), Device.Key("2"), Device.Key("3") };
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                // putin mai sus decat inainte (usor de apasat si pe telefon), mai mari pe telefon
                bool mobS = Device.Mobile;
                float ssz = mobS ? 124f : 108f;
                var rt = UIKit.Rect(skillBar, "Skill" + i, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2((-1 + i) * (mobS ? 150f : 130f), mobS ? 150f : 136f), new Vector2(ssz, ssz));
                // iconita stinsa (fundal) + aceeasi iconita "plina" care se reumple pe masura ce scade cooldown-ul
                skillIcon[i] = UIKit.Rect(rt, "I", C, C, Vector2.zero, new Vector2(ssz - 4f, ssz - 4f)).gameObject.AddComponent<Image>();
                skillIcon[i].raycastTarget = true;
                var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = skillIcon[i]; b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => game.player.UseSkill(k));
                TextHover.Attach(rt.gameObject, () => SkillDB.Tooltip(game.player.Skills[k], keys[k]), new Color(0.75f, 0.6f, 1f));
                var cd = UIKit.Rect(rt, "Fill", C, C, Vector2.zero, new Vector2(ssz - 4f, ssz - 4f)).gameObject.AddComponent<Image>();
                cd.type = Image.Type.Filled; cd.fillMethod = Image.FillMethod.Radial360;
                cd.fillOrigin = (int)Image.Origin360.Top; cd.fillClockwise = true; cd.raycastTarget = false; cd.fillAmount = 1f;
                skillCdImg[i] = cd;
                skillCdText[i] = UIKit.Text(rt, "S", "", 38, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(0, 2), new Vector2(100, 50), true, true);
                skillName[i] = UIKit.Text(rt, "K", keys[i], 22, new Color(1f, 0.9f, 0.5f), TextAnchor.LowerRight, BR, BR, new Vector2(2, -2), new Vector2(40, 30), true, true);
            }
        }

        void UpdateSkillBar(Player p)
        {
            bool show = !game.InPlaza;
            if (skillBar.gameObject.activeSelf != show) skillBar.gameObject.SetActive(show);
            if (!show) return;
            var sk = p.Skills;
            if (skillHero != p.D.hero)
            {
                skillHero = p.D.hero;
                for (int i = 0; i < 3; i++)
                {
                    var d = SkillDB.Get(sk[i]);
                    skillIcon[i].sprite = ItemIcons.Get(d.icon); ItemIcons.Art(skillIcon[i]);
                    skillCdImg[i].sprite = skillIcon[i].sprite; ItemIcons.Art(skillCdImg[i]);
                    skillCdImg[i].type = Image.Type.Filled; skillCdImg[i].preserveAspect = true;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                float left = p.SkillCooldownLeft(i);
                bool cd = left > 0f;
                skillIcon[i].color = cd ? new Color(0.35f, 0.35f, 0.4f, 0.55f) : Color.white;
                skillCdImg[i].enabled = cd;
                if (cd) skillCdImg[i].fillAmount = 1f - p.SkillCooldownFrac(i);
                skillCdText[i].text = cd ? Mathf.CeilToInt(left).ToString() : "";
                // gata de folosit: mic "puls" cand iese din cooldown
                float s = !cd && left > -0.25f ? 1f + 0.15f * Mathf.Sin((left + 0.25f) / 0.25f * Mathf.PI) : 1f;
                skillIcon[i].rectTransform.localScale = Vector3.one * s;
            }
        }

        void UpdateTracker(Player p)
        {
            var D = p.D;
            bool show = D.quests.Count > 0 && !D.trackerHidden;
            trackerRt.gameObject.SetActive(show);
            trackerOpenBtn.gameObject.SetActive(D.quests.Count > 0 && D.trackerHidden);
            if (!show) return;
            var sb = new System.Text.StringBuilder();
            foreach (var q in D.quests) sb.Append(q.map).Append(q.index).Append(':').Append(q.progress).Append(q.tracked).Append('|');
            string key = sb.ToString() + game.mapIndex;
            if (key == trackerKey) return;
            trackerKey = key;
            for (int i = trackerBody.childCount - 1; i >= 0; i--) Destroy(trackerBody.GetChild(i).gameObject);
            trackerTitle.text = "QUEST";
            float y = 0;
            foreach (var q in D.quests)
            {
                var def = QuestDB.Get(q);
                bool here = q.map == game.mapIndex && !game.InPlaza;
                var row = UIKit.Rect(trackerBody, "Q", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -y), new Vector2(460, 80));
                UIKit.Text(row, "T", def.title + (here ? "" : "  <size=15><color=#9aa4c0>(" + GameData.Maps[q.map].name + ")</color></size>"), 20, here ? Color.white : new Color(1, 1, 1, 0.6f), TextAnchor.MiddleLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -2), new Vector2(330, 30), true, true).supportRichText = true;
                var bar = UIKit.MakeBar(row, "B", new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -36), new Vector2(330, 34), new Color(1f, 0.8f, 0.3f), 18, false);
                bar.Set(q.progress / (float)def.count, def.target + "  " + q.progress + " / " + def.count);
                var qq = q;
                UIKit.Button(row, "Track", q.tracked ? "TRACK: ON" : "TRACK: OFF", q.tracked ? "Green" : "Grey", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -20), new Vector2(100, 40),
                             () => { qq.tracked = !qq.tracked; trackerKey = ""; Sfx.Play("Click", 0.5f); }, 12);
                y += 82;
            }
            trackerRt.sizeDelta = new Vector2(460, 44 + y);
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
#if UNITY_2023_1_OR_NEWER
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
#endif
            var es = new GameObject("EventSystem");
            es.transform.SetParent(SceneGroups.Get("Systems"), false);
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        public bool PointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        // ---------------------------------------------------------------
        //  API
        // ---------------------------------------------------------------
        public void Say(string s, float t)
        {
            if (PromoDirector.Recording) return;   // imagine curata in reclama
            msgText.text = s;
            msgT = t;
            msgRt.gameObject.SetActive(true);
            msgRt.localScale = Vector3.one * 1.12f;
        }

        // ---- "WELCOME TO ..." (5 secunde, la fiecare intrare pe o harta)
        float titleCheckT = 1f;
        RectTransform welcomeRt; CanvasGroup welcomeCg; Text welcomeName, welcomeSub; float welcomeT, welcomeDelay;

        public void Welcome(string name, string sub, Color c)
        {
            if (welcomeRt == null)
            {
                Vector2 TC = new Vector2(0.5f, 1), C = new Vector2(0.5f, 0.5f);
                welcomeRt = UIKit.Rect(root, "Welcome", TC, TC, new Vector2(0, -190), new Vector2(1000, 170));
                welcomeCg = welcomeRt.gameObject.AddComponent<CanvasGroup>();
                welcomeCg.blocksRaycasts = false; welcomeCg.interactable = false;
                welcomeName = UIKit.Text(welcomeRt, "N", "", 72, Color.white, TextAnchor.MiddleCenter, TC, TC, new Vector2(0, -40), new Vector2(1000, 90));
                welcomeSub = UIKit.Text(welcomeRt, "S", "", 24, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter, TC, TC, new Vector2(0, -128), new Vector2(1000, 36));
                welcomeSub.supportRichText = true;
            }
            welcomeName.text = name.ToUpper();
            welcomeName.color = c;
            welcomeSub.text = "";   // doar numele hartii (detaliile sunt in GUIDE)
            welcomeT = 5f;
            welcomeDelay = 1f;   // apare la 1 secunda dupa ce intri pe harta
            welcomeCg.alpha = 0f;
            welcomeRt.gameObject.SetActive(true);
            welcomeRt.SetAsLastSibling();
        }

        void UpdateWelcome(float dt)
        {
            if (welcomeRt == null || welcomeT <= 0f) return;
            if (LoadingScreen.Active) { welcomeCg.alpha = 0f; return; }   // bannerul apare dupa ecranul de incarcare
            if (welcomeDelay > 0f) { welcomeDelay -= dt; welcomeCg.alpha = 0f; return; }
            welcomeT -= dt;
            float shown = 5f - welcomeT;
            welcomeCg.alpha = Mathf.Clamp01(shown / 0.35f) * Mathf.Clamp01(welcomeT / 0.7f);
            welcomeRt.localScale = Vector3.one * (shown < 0.35f ? Mathf.Lerp(1.25f, 1f, shown / 0.35f) : 1f);
            if (welcomeT <= 0f) welcomeRt.gameObject.SetActive(false);
        }

        /// <summary>Banner-ul LEVEL UP (inlocuieste textul mare + mesajul lung).</summary>
        public void LevelUp(int level, int points)
        {
            if (lvlRt == null)
            {
                lvlRt = UIKit.Rect(root, "LevelUp", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 190), new Vector2(620, 156));
                lvlCg = lvlRt.gameObject.AddComponent<CanvasGroup>();
                lvlCg.blocksRaycasts = false; lvlCg.interactable = false;
                UIKit.Img(lvlRt, "flat_Grey", new Color(0.09f, 0.08f, 0.16f, 0.72f), 18f, 1f).raycastTarget = false;
                var bar = UIKit.Rect(lvlRt, "Bar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(560, 6));
                UIKit.Img(bar, "flat_Grey", new Color(1f, 0.82f, 0.3f, 0.9f), 2f, 1f).raycastTarget = false;
                var sh = UIKit.Rect(lvlRt, "Shine", new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 156));
                lvlShine = UIKit.Img(sh, "flat_Grey", new Color(1f, 1f, 1f, 0.12f), 2f, 1f);
                lvlShine.raycastTarget = false;
                sh.localRotation = Quaternion.Euler(0, 0, -18f);
                lvlRt.gameObject.AddComponent<RectMask2D>();
                lvlTitle = UIKit.Text(lvlRt, "T", "LEVEL UP!", 62, new Color(1f, 0.85f, 0.32f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(600, 80));
                lvlSub = UIKit.Text(lvlRt, "S", "", 28, new Color(0.55f, 0.95f, 1f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(600, 46));
            }
            lvlSub.text = "LEVEL " + level + "   <color=#ffffffcc>+" + points + " skill points</color>";
            lvlT = 2.8f;
            lvlRt.gameObject.SetActive(true);
            lvlRt.SetAsLastSibling();
        }

        public void BigText(string s, Color c)
        {
            bigText.text = s;
            bigCol = c;
            bigT = 1.8f;
            bigText.gameObject.SetActive(true);
        }

        public void Pop(Vector3 worldPos, string s, Color c, float scale)
        {
            // setare: fara numere de damage (textele de loot raman)
            if (!GameSettings.ShowDamageNumbers && s.Length > 0 && (char.IsDigit(s[0]) || s[0] == '-' || s.StartsWith("CRIT "))) return;
            PopItem p = null;
            foreach (var x in pops) if (!x.on) { p = x; break; }
            if (p == null)
            {
                if (pops.Count >= 70) { p = pops[0]; pops.RemoveAt(0); pops.Add(p); }
                else
                {
                    p = new PopItem();
                    p.t = UIKit.Text(popLayer, "Pop", "", 34, Color.white, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 60));
                    p.rt = p.t.rectTransform;
                    pops.Add(p);
                }
            }
            p.on = true;
            p.t.gameObject.SetActive(true);
            p.t.text = s;
            p.col = c;
            p.pos = worldPos + new Vector3(Random.Range(-0.4f, 0.4f), 0, Random.Range(-0.2f, 0.2f));
            p.age = 0f;
            p.life = 0.9f + scale * 0.2f;
            p.scale = scale;
            p.t.fontSize = Mathf.RoundToInt(38 * scale);
        }

        public void Toast(string s, Color c) { AddToast(s, c, null); }

        public void ItemToast(Item it)
        {
            AddToast(it.name + "\n" + ItemDB.RarityNames[it.rarity].ToUpper() + "   " + ItemDB.StatText(it), ItemDB.Col(it.rarity), ItemIcons.For(it));
        }

        public void Toast(string s, Color c, Sprite icon) { AddToast(s, c, icon); }
        /// <summary>Imparte textul unei notificari in TITLU + detalii (ex. "NEW TITLE UNLOCKED: Hero" -> titlu / detalii).</summary>
        static void SplitToast(string s, out string title, out string body)
        {
            s = s ?? "";
            int i;
            if ((i = s.IndexOf('\n')) > 0) { title = s.Substring(0, i); body = s.Substring(i + 1); }
            else if ((i = s.IndexOf(": ")) > 0 && i < 42) { title = s.Substring(0, i); body = s.Substring(i + 2); }
            else if ((i = s.IndexOf(" - ")) > 0 && i < 42) { title = s.Substring(0, i); body = s.Substring(i + 3); }
            else if ((i = s.IndexOf("! ")) > 0 && i < 42) { title = s.Substring(0, i + 1); body = s.Substring(i + 2); }
            else { title = s; body = ""; }
            title = title.Trim().TrimEnd(':');
            body = body.Trim();
            if (body.StartsWith("(") && body.EndsWith(")")) body = body.Substring(1, body.Length - 2);
        }

        void AddToast(string s, Color c, Sprite icon)
        {
            // notificare noua: card in centrul de sus (sub bara de boss), cu iconita in cerc, TITLU colorat + detalii
            const float W = 640f, Hh = 96f;
            string title, body;
            SplitToast(s, out title, out body);
            var rt = UIKit.Rect(toastLayer, "Toast", new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(W, Hh));
            // contur colorat (stralucire) + fundal inchis
            UIKit.Img(rt, "flat_Grey", new Color(0.09f, 0.08f, 0.16f, 0.72f), 18f, 1f).raycastTarget = false;   // ca meniurile (~70%)
            // iconita intr-un cerc
            var circ = UIKit.Rect(rt, "Circle", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(72, 72));
            UIKit.Img(circ, "round_Grey", new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, 1f), 30f, 1f).raycastTarget = false;
            var im = UIKit.Rect(circ, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 52)).gameObject.AddComponent<Image>();
            im.sprite = icon != null ? icon : ItemIcons.Get(26); ItemIcons.Art(im);
            im.color = icon != null ? c : new Color(1f, 0.85f, 0.4f);
            im.raycastTarget = false;
            float x = 100f;
            var tt = UIKit.Text(rt, "Title", title.ToUpperInvariant(), 25, Color.Lerp(c, Color.white, 0.25f), body.Length > 0 ? TextAnchor.LowerLeft : TextAnchor.MiddleLeft,
                                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x, body.Length > 0 ? 14 : 0), new Vector2(W - x - 16, body.Length > 0 ? 40 : 80), true, true);
            tt.supportRichText = true; UIKit.Fit(tt, 14);
            if (body.Length > 0)
            {
                var bt = UIKit.Text(rt, "Body", body, 19, new Color(0.92f, 0.93f, 1f), TextAnchor.UpperLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x, -18), new Vector2(W - x - 16, 40), true, true);
                bt.supportRichText = true; UIKit.Fit(bt, 12);
            }
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; cg.interactable = false;
            toasts.Insert(0, new ToastItem { rt = rt, cg = cg, age = 0f });
            while (toasts.Count > 3) { Destroy(toasts[toasts.Count - 1].rt.gameObject); toasts.RemoveAt(toasts.Count - 1); }
        }

        /// <summary>Eticheta fixa in lume (ex. "FORJA"), vizibila de aproape.</summary>
        public Text Label(Vector3 worldPos, string s, Color c, float size = 24f, float maxDist = 45f)
        {
            var t = UIKit.Text(labelLayer, "L_" + s, s, Mathf.RoundToInt(size), c, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 50));
            t.supportRichText = true;
            t.gameObject.SetActive(false);
            labels.Add(new WLabel { pos = worldPos, t = t, maxDist = maxDist });
            return t;
        }

        /// <summary>Muta o eticheta creata cu Label (ex. numele unui jucator care se misca).</summary>
        public void MoveLabel(Text t, Vector3 worldPos)
        {
            if (t == null) return;
            foreach (var l in labels) if (l.t == t) { l.pos = worldPos; return; }
        }

        public void RemoveLabel(Text t)
        {
            if (t == null) return;
            labels.RemoveAll(l => l.t == t);
            Destroy(t.gameObject);
        }

        public void PulseCoins() { coinPulse = 1f; }
        public void FlashDamage() { flashA = 0.28f; }
        public void DeathFade(float dur) { fadeT = dur; fadeDur = dur; }

        public void ToggleInventory(bool shop)
        {
            if (inventory.IsOpen) inventory.Close();
            else inventory.Open(shop);
        }

        public void OpenInventory(bool shop) { if (!inventory.IsOpen) inventory.Open(shop); }

        void ToggleAuto()
        {
            var D = game.player.D;
            if (!D.passAuto)
            {
                D.autoAttack = false;
                Say("AUTO SWING needs the Auto Swing pass (SHOP)", 2.5f);
                Sfx.Play("Error", 0.5f);
                shop.Open();
                return;
            }
            D.autoAttack = !D.autoAttack;
            Say(D.autoAttack ? "Auto swing ON" : "Auto swing OFF", 1.5f);
        }


        void ToggleMute()
        {
            if (Sfx.I != null) Sfx.I.ToggleMute();
        }

        // ---------------------------------------------------------------
        //  Update
        // ---------------------------------------------------------------
        // ---- cache-uri: textele se refac doar cand se schimba valoarea (fara gunoi de memorie in fiecare cadru)
        long cLvl = -1, cHp = -1, cHpMax = -1, cXp = -1, cXpNeed = -1, cCoins = -1, cShards = -1, cTokens = -1, cPow = -1, cPts = -1;
        int cAuto = -1, cBossHp = -1, cBossMax = -1;
        readonly int[] cPot = { -1, -1, -1 };
        float slowT;
        Loot cLoot;
        string cPrompt = null; Interactable cPromptOwner;
        Transform adminBtnT;
        Enemy cBoss;

        void LateUpdate()
        {
            if (game == null || game.player == null) return;
            if (cam == null) cam = game.cam;
            if (cam == null) return;
            var p = game.player;
            float dt = Time.unscaledDeltaTime;

            // butoanele nu raman "selectate" dupa click (altfel ENTER / SPACE le-ar apasa din nou)
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null && es.currentSelectedGameObject.GetComponent<InputField>() == null)
                es.SetSelectedGameObject(null);

            // tastatura (toggle-uri)
            if (Inp.InventoryDown() && (trade == null || !trade.IsOpen)) ToggleInventory(false);
            if (Inp.EscapeDown() && inventory.IsOpen) inventory.Close();
            if (AdminAccess.IsAdmin && Inp.AdminDown()) admin.Toggle();
            if (AdminAccess.IsAdmin && Inp.PhotoDown()) IconShot.Snap();
            if (Inp.AutoDown()) ToggleAuto();
            if (Inp.MuteDown()) ToggleMute();

            // filmarea pentru reclama: fara ADMIN, mesaje, notificari si bannere (imagine curata)
            bool promo = PromoDirector.Recording;
            if (adminBtnT == null) adminBtnT = root.Find("BtnAdmin");
            var adminBtn = adminBtnT;
            bool showAdmin = !promo && AdminAccess.IsAdmin;
            if (adminBtn != null && adminBtn.gameObject.activeSelf != showAdmin) adminBtn.gameObject.SetActive(showAdmin);
            if (!AdminAccess.IsAdmin && admin != null && admin.IsOpen) admin.Close();
            if (pointsBadge != null && promo) pointsBadge.gameObject.SetActive(false);
            if (toastLayer != null && toastLayer.gameObject.activeSelf == promo) toastLayer.gameObject.SetActive(!promo);
            if (promo && msgRt != null) msgRt.gameObject.SetActive(false);
            if (promo && welcomeCg != null) { welcomeCg.alpha = 0f; welcomeT = 0f; }

            // boost-uri active + potiuni
            for (int i = 0; i < 3; i++)
            {
                float left = p.D.buffs[i];
                bool on = left > 0f;
                if (potSlot[i].gameObject.activeSelf != on) potSlot[i].gameObject.SetActive(on);
                if (on)
                {
                    int sec = Mathf.CeilToInt(left);
                    if (cPot[i] != sec) { cPot[i] = sec; potTime[i].text = (sec / 60) + ":" + (sec % 60).ToString("00"); }
                }
                if (i == 0) UpdateSkillBar(p);
            }
            // potiunile active se aseaza una langa alta (jos-dreapta)
            float bx = Device.Mobile ? -430 : 430;   // pe telefon in stanga (dreapta e ocupata de FIGHT / JUMP)
            for (int i = 0; i < 3; i++) if (potSlot[i].gameObject.activeSelf) { potSlot[i].anchoredPosition = new Vector2(bx, 14); bx += Device.Mobile ? -100 : 100; }
            slowT -= dt;
            bool slow = slowT <= 0f;          // lucrurile care nu trebuie verificate in fiecare cadru: de 5 ori pe secunda
            if (slow) slowT = 0.2f;
            if (slow) UpdateTracker(p);
            if (cLvl != p.Level) { cLvl = p.Level; lvlBarText.text = "LV " + p.Level; }
            long hpI = Mathf.CeilToInt(p.hp), hpM = Mathf.CeilToInt(p.MaxHp);
            if (hpI != cHp || hpM != cHpMax) { cHp = hpI; cHpMax = hpM; hpBar.Set(p.hp / p.MaxHp, "HP " + Fmt.Num(hpI) + " / " + Fmt.Num(hpM)); }
            else hpBar.Set(p.hp / p.MaxHp, null);
            long xpN = p.MaxedLevel ? -2 : p.XpNeed;
            if (p.D.xp != cXp || xpN != cXpNeed)
            {
                cXp = p.D.xp; cXpNeed = xpN;
                if (p.MaxedLevel) xpBar.Set(1f, "MAX LEVEL " + GameData.MaxLevel);
                else xpBar.Set((float)p.D.xp / p.XpNeed, "XP " + Fmt.Num(p.D.xp) + " / " + Fmt.Num(p.XpNeed));
            }
            Rewards.Tick(Time.unscaledDeltaTime);
            if (rewardsDot != null)
            {
                bool rdy = Rewards.AnyReady && !PromoDirector.Recording;
                if (rewardsDot.gameObject.activeSelf != rdy) rewardsDot.gameObject.SetActive(rdy);
                if (rdy) rewardsDot.localScale = Vector3.one * (1f + 0.1f * Mathf.Sin(Time.unscaledTime * 6f));
            }
            int free = p.PointsFree;
            pointsBadge.gameObject.SetActive(free > 0 && !PromoDirector.Recording);
            if (free > 0)
            {
                if (cPts != free) { cPts = free; pointsText.text = free.ToString(); }
                pointsBadge.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 6f));
            }
            if (cCoins != p.D.coins) { cCoins = p.D.coins; coinText.text = Fmt.Num(p.D.coins); }
            if (cShards != p.D.shards) { cShards = p.D.shards; shardText.text = Fmt.Num(p.D.shards); }
            if (cTokens != p.D.tokens) { cTokens = p.D.tokens; tokenText.text = Fmt.Num(p.D.tokens); }
            coinPulse = Mathf.MoveTowards(coinPulse, 0f, dt * 5f);
            coinPill.localScale = Vector3.one * (1f + coinPulse * 0.12f);
            // AUTO SWING pornit = buton verde, oprit = rosu
            string autoSpr = p.D.passAuto && p.D.autoAttack ? "sq_Green" : "sq_Red";
            if (autoBtnImg.sprite == null || autoBtnImg.sprite.name != autoSpr) autoBtnImg.sprite = UIKit.Sprite(autoSpr, 18f);
            autoBtnImg.color = Color.white;
            int au = !p.D.passAuto ? 0 : p.D.autoAttack ? 1 : 2;
            if (au != cAuto) { cAuto = au; autoText.text = au == 0 ? "AUTO SWING" : au == 1 ? "AUTO: ON" : "AUTO: OFF"; }

            // plaza: butonul X sus; mesajele coboara putin ca sa nu se suprapuna
            if (leaveBtn.gameObject.activeSelf != game.InPlaza) leaveBtn.gameObject.SetActive(game.InPlaza);
            // chenarul cu serverul / jucatorii a fost scos - toate detaliile sunt in fereastra TRADING
            if (serverBar.gameObject.activeSelf) serverBar.gameObject.SetActive(false);
            msgRt.anchoredPosition = new Vector2(0, -101);   // intre butoanele de chat si notificari

            // numele deasupra capului
            Vector2 np;
            if (!p.dead && !Blocking && ToScreen(p.transform.position + Vector3.up * (Player.HeroHeight + 0.45f), out np))
            {
                if (!nameplateRt.gameObject.activeSelf) nameplateRt.gameObject.SetActive(true);
                if (slow) { string nm = NameplateText(p); if (nameplate.text != nm) nameplate.text = nm; }
                long pow = p.Power;
                if (pow != cPow) { cPow = pow; npPower.text = Fmt.Num(pow); }
                {
                    if (slow)
                    {
                        string tn; Color tc; int tt;
                        TitleDB.Current(p, out tn, out tc, out tt);
                        string tk = tn + tt;
                        if (npTitleKey != tk) { npTitleKey = tk; TitleStyle.Apply(npTitle, tn, tc, tt, 24); }
                    }
                    // flacara + numar centrate impreuna
                    float w = npPower.preferredWidth;
                    npFlame.rectTransform.anchoredPosition = new Vector2(-w * 0.5f, 0);
                    npPower.rectTransform.anchoredPosition = new Vector2(4 - w * 0.5f, 1);
                }
                nameplateRt.position = new Vector3(np.x, np.y, 0);
            }
            else if (nameplateRt.gameObject.activeSelf) nameplateRt.gameObject.SetActive(false);

            // mesaj
            if (msgT > 0f)
            {
                msgT -= dt;
                msgRt.localScale = Vector3.Lerp(msgRt.localScale, Vector3.one, 1f - Mathf.Exp(-14f * dt));
                if (msgT <= 0f) msgRt.gameObject.SetActive(false);
            }

            UpdateWelcome(dt);
            titleCheckT -= dt;
            if (titleCheckT <= 0f && game.player != null && game.player.D != null) { titleCheckT = 2f; TitleDB.CheckUnlocks(game.player); }

            // banner LEVEL UP: apare cu "pop", o raza de lumina trece peste el, apoi se stinge
            if (lvlT > 0f && lvlRt != null)
            {
                lvlT -= dt;
                float k = 2.8f - lvlT;
                float sc = k < 0.12f ? Mathf.Lerp(0.6f, 1.08f, k / 0.12f) : k < 0.25f ? Mathf.Lerp(1.08f, 1f, (k - 0.12f) / 0.13f) : 1f;
                lvlRt.localScale = Vector3.one * sc;
                lvlCg.alpha = Mathf.Clamp01(lvlT / 0.45f) * Mathf.Clamp01(k / 0.08f);
                var shr = lvlShine.rectTransform;
                shr.anchoredPosition = new Vector2(Mathf.Lerp(-120f, 760f, Mathf.Clamp01((k - 0.2f) / 0.7f)), 0f);
                lvlTitle.color = Color.Lerp(new Color(1f, 0.85f, 0.32f), Color.white, 0.25f + 0.25f * Mathf.Sin(k * 9f));
                if (lvlT <= 0f) lvlRt.gameObject.SetActive(false);
            }

            // text mare (level up, boss defeated...)
            if (bigT > 0f)
            {
                bigT -= dt;
                float k = 1.8f - bigT;
                float s = k < 0.15f ? Mathf.Lerp(2.2f, 1f, k / 0.15f) : 1f + 0.03f * Mathf.Sin(k * 10f);
                bigText.rectTransform.localScale = Vector3.one * s;
                Color c = bigCol; c.a = Mathf.Clamp01(bigT / 0.4f);
                bigText.color = c;
                if (bigT <= 0f) bigText.gameObject.SetActive(false);
            }

            // prompt
            var cur = game.CurrentInteractable;
            if (cur != null && !Blocking)
            {
                promptRt.gameObject.SetActive(true);
                if (slow || cur != cPromptOwner || cPrompt == null)
                {
                    cPromptOwner = cur;
                    string lbl = Device.StripKeys(cur.Label());
                    if (Device.Mobile) lbl = "TAP:  " + lbl;
                    cPrompt = lbl;
                    if (promptText.text != lbl) promptText.text = lbl;
                }
                if (promptLootStyle)
                {   // inapoi la butonul galben (interactiuni: shop, portal ...)
                    promptLootStyle = false;
                    promptImg.sprite = promptBtnSprite; promptImg.type = Image.Type.Sliced;
                    promptText.color = Color.white;
                    promptRt.sizeDelta = new Vector2(promptW, promptRt.sizeDelta.y);
                }
                promptImg.color = Color.Lerp(cur.color, Color.white, 0.3f);
                promptRt.localScale = Vector3.one * (1f + 0.02f * Mathf.Sin(Time.unscaledTime * 5f));
            }
            else if (game.CurrentLoot != null && !Blocking)
            {
                cPromptOwner = null;
                promptRt.gameObject.SetActive(true);
                if (slow || game.CurrentLoot != cLoot)
                {
                cLoot = game.CurrentLoot;
                int more = 0;
                foreach (var l in Loot.Pickable)
                {
                    if (l == null || !l.Landed || l == game.CurrentLoot) continue;
                    Vector3 d = l.transform.position - game.player.transform.position; d.y = 0;
                    if (d.magnitude <= Game.PickRadius + 0.6f) more++;
                }
                // doar numarul si itemul (ca un toast, ~70% opac)
                var cl = game.CurrentLoot;
                string lbl = (cl.kind == Loot.Kind.Item ? "1x  " : "") + cl.PickName + (more > 0 ? "   <color=#ffffffaa>+" + more + "</color>" : "");
                promptText.supportRichText = true;
                if (promptText.text != lbl)
                {
                    promptText.text = lbl;
                    promptRt.sizeDelta = new Vector2(Mathf.Clamp(promptText.preferredWidth + 90f, 260f, promptW), promptRt.sizeDelta.y);
                }
                if (!promptLootStyle)
                {
                    promptLootStyle = true;
                    promptImg.sprite = promptFlatSprite; promptImg.type = Image.Type.Sliced;
                    promptImg.color = new Color(0.09f, 0.08f, 0.16f, 0.72f);
                }
                promptText.color = Color.Lerp(cl.PickCol, Color.white, 0.25f);
                }
                promptRt.localScale = Vector3.one;
            }
            else promptRt.gameObject.SetActive(false);

            UpdatePops(dt);
            UpdateEnemyBars(p);
            UpdateLabels(p);
            UpdateToasts(dt);

            flashA = Mathf.MoveTowards(flashA, 0f, dt * 0.9f);
            float lowHp = p.hp / p.MaxHp < 0.25f && !p.dead ? 0.12f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f) : 0f;
            flash.color = new Color(1f, 0.1f, 0.1f, Mathf.Max(flashA, lowHp));
            if (fadeT > 0f)
            {
                fadeT -= dt;
                float k = 1f - fadeT / fadeDur;
                fade.color = new Color(0, 0, 0, Mathf.Sin(k * Mathf.PI) * 0.85f);
            }
            else fade.color = new Color(0, 0, 0, 0);
        }

        bool ToScreen(Vector3 world, out Vector2 sp)
        {
            var v = cam.WorldToScreenPoint(world);
            sp = new Vector2(v.x, v.y);
            return v.z > 0f && v.x > -200 && v.x < Screen.width + 200 && v.y > -200 && v.y < Screen.height + 200;
        }

        void UpdatePops(float dt)
        {
            foreach (var p in pops)
            {
                if (!p.on) continue;
                p.age += dt;
                if (p.age >= p.life) { p.on = false; p.t.gameObject.SetActive(false); continue; }
                float k = p.age / p.life;
                Vector2 sp;
                if (!ToScreen(p.pos + Vector3.up * (k * 1.8f), out sp)) { p.t.enabled = false; continue; }
                p.t.enabled = true;
                p.rt.position = new Vector3(sp.x, sp.y, 0);
                float s = p.age < 0.1f ? Mathf.Lerp(1.8f, 1f, p.age / 0.1f) : 1f;
                p.rt.localScale = Vector3.one * s;
                Color c = p.col; c.a = k > 0.65f ? 1f - (k - 0.65f) / 0.35f : 1f;
                p.t.color = c;
            }
        }

        void UpdateEnemyBars(Player p)
        {
            int used = 0;
            Enemy bossNear = null;
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dying) continue;
                Vector3 d = e.transform.position - p.transform.position; d.y = 0;
                float dist = d.magnitude;
                if (e.boss && dist < 34f) bossNear = e;
                if (dist > 32f) continue;
                if (e.boss) continue; // boss-ul are bara mare
                bool show = e.hp < e.maxHp && Time.time - e.LastHitTime < 6f;
                if (!show) continue;
                Vector2 sp;
                if (!ToScreen(e.transform.position + Vector3.up * (e.height + 0.3f), out sp)) continue;
                if (used >= ebars.Count) ebars.Add(MakeEBar());
                var b = ebars[used++];
                b.rt.gameObject.SetActive(true);
                b.rt.position = new Vector3(sp.x, sp.y, 0);
                b.fill.fillAmount = (float)e.hp / e.maxHp;
                if (b.owner != e) { b.owner = e; b.name.text = e.nm + "  Lv" + e.lvl; }
            }
            for (int i = used; i < ebars.Count; i++) if (ebars[i].rt.gameObject.activeSelf) ebars[i].rt.gameObject.SetActive(false);

            if (bossNear != null)
            {
                bossBar.root.gameObject.SetActive(true);
                int bh = Mathf.Max(0, (int)bossNear.hp), bm = (int)bossNear.maxHp;
                if (bh != cBossHp || bm != cBossMax || cBoss != bossNear) { cBossHp = bh; cBossMax = bm; bossBar.Set((float)bossNear.hp / bossNear.maxHp, Fmt.Num(bh) + " / " + Fmt.Num(bm)); }
                else bossBar.Set((float)bossNear.hp / bossNear.maxHp, null);
                if (cBoss != bossNear) { cBoss = bossNear; bossName.text = "BOSS: " + bossNear.nm + "  (Lv " + bossNear.lvl + ")"; }
            }
            else bossBar.root.gameObject.SetActive(false);
        }

        EBar MakeEBar()
        {
            var b = new EBar();
            b.rt = UIKit.Rect(barLayer, "EBar", Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(120, 16));
            UIKit.Img(b.rt, "flat_Grey", new Color(0.05f, 0.05f, 0.08f, 0.85f), 8f, 3f);
            var fr = UIKit.Stretch(b.rt, "Fill", 3f);
            b.fill = fr.gameObject.AddComponent<Image>();
            b.fill.sprite = UIKit.Sprite("flat_Grey", 8f);
            b.fill.type = Image.Type.Filled;
            b.fill.fillMethod = Image.FillMethod.Horizontal;
            b.fill.color = new Color(1f, 0.3f, 0.3f);
            b.fill.raycastTarget = false;
            b.name = UIKit.Text(b.rt, "N", "", 15, Color.white, TextAnchor.LowerCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0, 2), new Vector2(240, 22), true, true);
            return b;
        }

        void UpdateLabels(Player p)
        {
            foreach (var l in labels)
            {
                Vector3 d = l.pos - p.transform.position; d.y = 0;
                float dist = d.magnitude;
                Vector2 sp;
                bool vis = dist < l.maxDist && ToScreen(l.pos, out sp);
                if (vis)
                {
                    ToScreen(l.pos, out sp);
                    if (!l.t.gameObject.activeSelf) l.t.gameObject.SetActive(true);
                    l.t.rectTransform.position = new Vector3(sp.x, sp.y, 0);
                    Color c = l.t.color; c.a = Mathf.Clamp01((l.maxDist - dist) / 10f);
                    l.t.color = c;
                }
                else if (l.t.gameObject.activeSelf) l.t.gameObject.SetActive(false);
            }
        }

        // ---------------------------------------------------------------
        //  Bula de chat deasupra capului (cand scrii in GLOBAL / SERVER)
        // ---------------------------------------------------------------
        RectTransform bubbleRt;
        Text bubbleText;
        CanvasGroup bubbleCg;
        float bubbleT;

        public void ShowBubble(string msg)
        {
            if (nameplateRt == null || string.IsNullOrEmpty(msg)) return;
            if (bubbleRt == null)
            {
                Vector2 TC = new Vector2(0.5f, 1), BC = new Vector2(0.5f, 0), C = new Vector2(0.5f, 0.5f);
                bubbleRt = UIKit.Rect(nameplateRt, "ChatBubble", TC, BC, new Vector2(0, 14), new Vector2(300, 60));
                UIKit.Img(bubbleRt, "flat_Grey", new Color(1f, 1f, 1f, 0.96f), 16f, 1.2f);
                var tail = UIKit.Rect(bubbleRt, "Tail", BC, C, new Vector2(0, 2), new Vector2(18, 18));
                tail.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.96f);
                tail.localRotation = Quaternion.Euler(0, 0, 45);
                tail.SetAsFirstSibling();
                bubbleText = UIKit.Text(bubbleRt, "T", "", 22, new Color(0.12f, 0.12f, 0.18f), TextAnchor.MiddleCenter, C, C, Vector2.zero, new Vector2(280, 56), false, false);
                bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
                bubbleCg = bubbleRt.gameObject.AddComponent<CanvasGroup>();
                bubbleCg.blocksRaycasts = false;
            }
            bubbleText.text = msg;
            // latimea dupa text (max 420), inaltimea dupa cate randuri are
            bubbleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            float w = Mathf.Clamp(bubbleText.preferredWidth + 36f, 90f, 420f);
            bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bubbleText.rectTransform.sizeDelta = new Vector2(w - 24f, 200f);
            float h = Mathf.Clamp(bubbleText.preferredHeight + 22f, 50f, 140f);
            bubbleRt.sizeDelta = new Vector2(w, h);
            bubbleText.rectTransform.sizeDelta = new Vector2(w - 24f, h - 10f);
            bubbleRt.gameObject.SetActive(true);
            bubbleT = 5f;
        }

        void UpdateBubble(float dt)
        {
            if (bubbleRt == null || bubbleT <= 0f) return;
            bubbleT -= dt;
            float shown = 5f - bubbleT;
            bubbleCg.alpha = Mathf.Clamp01(bubbleT / 0.5f);
            bubbleRt.localScale = Vector3.one * (shown < 0.18f ? Mathf.Lerp(0.6f, 1f, shown / 0.18f) : 1f);
            if (bubbleT <= 0f) bubbleRt.gameObject.SetActive(false);
        }

        void UpdateToasts(float dt)
        {
            UpdateBubble(dt);
            // cand e afisat numele hartii (WELCOME), notificarile coboara sub el
            float wantY = -196f - (welcomeCg != null && welcomeCg.alpha > 0.05f ? 178f : 0f);
            var tp = toastLayer.anchoredPosition;
            tp.y = Mathf.Lerp(tp.y, wantY, 1f - Mathf.Exp(-8f * dt));
            toastLayer.anchoredPosition = tp;
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var t = toasts[i];
                t.age += dt;
                float targetY = -i * 106f;
                var pos = t.rt.anchoredPosition;
                pos.x = 0f;
                pos.y = Mathf.Lerp(pos.y, targetY, 1f - Mathf.Exp(-12f * dt));
                t.rt.anchoredPosition = pos;
                // apare cu un mic "pop", dispare cu fade
                float k = Mathf.Clamp01(t.age / 0.22f);
                float pop = t.age < 0.22f ? Mathf.Lerp(0.82f, 1.04f, k) : Mathf.Lerp(1.04f, 1f, Mathf.Clamp01((t.age - 0.22f) / 0.12f));
                t.rt.localScale = Vector3.one * pop;
                t.cg.alpha = t.age < 0.15f ? t.age / 0.15f : t.age > 4f ? 1f - (t.age - 4f) / 0.6f : 1f;
                if (t.age > 4.6f) { Destroy(t.rt.gameObject); toasts.RemoveAt(i); }
            }
        }
    }
}
