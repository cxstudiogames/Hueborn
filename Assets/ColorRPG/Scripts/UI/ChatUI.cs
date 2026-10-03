// =====================================================================
//  ChatUI.cs - chat-ul din stanga-sus (langa avatar): GLOBAL + SERVER + FRIENDS
//  (mesaje private catre prieteni - alegi prietenul din fereastra FRIENDS).
//  ENTER sau "/" = scrii, ENTER = trimiti, ESC = renunti.
//  Pana cand jocul e online, ceilalti jucatori sunt simulati (mesaje aleatorii),
//  dar structura e gata: mesajele tale merg in canalul ales.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class ChatUI : MonoBehaviour
    {
        public const int MaxLines = 9;
        const int Keep = 40;

        class Msg { public string name, text, peer; public Color col; public bool system, you, warn; }

        Game game;
        RectTransform panel, body;
        Image bg;
        Text log;
        ScrollRect scroll;
        RectTransform viewport, dmBar;
        Text dmText;
        bool stickBottom = true;
        int lastMapKey = int.MinValue;
        InputField input;
        public static ChatUI I;
        Image[] tabImgs = new Image[3];
        static readonly string[] TabNames = { "GLOBAL", "SERVER", "FRIENDS" };
        string dmTarget;                 // prietenul caruia ii scrii in FRIENDS
        Text placeholder;
        readonly List<KeyValuePair<float, string>> pendingReplies = new List<KeyValuePair<float, string>>();
        Text minText;
        int channel;                 // 0 = GLOBAL, 1 = SERVER, 2 = FRIENDS (privat)
        bool minimized;
        readonly List<Msg>[] msgs = { new List<Msg>(), new List<Msg>(), new List<Msg>() };
        readonly int[] unread = new int[3];
        float simT = 3f, simT2 = 6f;
        bool dirty = true;
        public bool Focused { get { return input != null && input.isFocused; } }

        static readonly string[] Lines =
        {
            "anyone selling forge stones?", "trading 50 stones for 2k tokens, booth 4", "lf group for the Orc Warlord", "gg", "just hatched a LEGENDARY pet!!",
            "how do I get to Frozen Peaks?", "whats the best weapon kind?", "selling epic armor, cheap", "my sword is +9 now :D", "anyone got a guardian cloak?",
            "ruby bazaar has a lot of booths", "need 2 more levels for desert", "lol that boss hits hard", "buying xp potions", "who has the most power here?",
            "rebirth is so worth it", "hi everyone", "anyone want to trade pets?", "max forge failed 3 times rip", "magnet pass is op",
            "awakening II unlocked!", "join my private server later", "tips for lava map?", "afk farming slimes", "nice pet!"
        };

        public static ChatUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("ChatUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<ChatUI>();
            c.game = g;
            I = c;
            c.Build(parent);
            return c;
        }

        void Build(RectTransform parent)
        {
            Vector2 TL = new Vector2(0, 1), TR = new Vector2(1, 1), BL = new Vector2(0, 0), BR = new Vector2(1, 0), C = new Vector2(0.5f, 0.5f);
            panel = UIKit.Rect(parent, "Chat", TL, TL, new Vector2(164, -12), new Vector2(560, 252));
            if (Device.Mobile) panel.localScale = Vector3.one * 1.3f;   // pe telefon: tab-uri si butoane mai mari, usor de apasat
            // tab-uri
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                var b = UIKit.Button(panel, "Tab" + i, TabNames[i], "Grey", TL, TL, new Vector2(i * 138, 0), new Vector2(134, 52), () => { channel = k; unread[k] = 0; dirty = true; stickBottom = true; UpdatePlaceholder(); if (minimized) SetMin(false); }, 19);
                tabImgs[i] = b.GetComponent<Image>();
            }
            var mb = UIKit.Button(panel, "Min", "-", "Grey", TL, TL, new Vector2(414, 0), new Vector2(60, 52), () => SetMin(!minimized), 28);
            // REPORT / BLOCK jucatori (cerinta Google Play pentru chat)
            UIKit.Button(panel, "Safety", "!", "Red", TL, TL, new Vector2(480, 0), new Vector2(60, 52), ToggleSafety, 28);
            minText = mb.GetComponentInChildren<Text>();
            // jucatori online (global), mic, langa butoane: "Online:" verde + numarul alb
            // chiar sub GLOBAL; dispare cat timp chat-ul e deschis (reapare cand il strangi)
            onlineText = UIKit.Text(panel, "Online", "", 19, Color.white, TextAnchor.MiddleLeft, TL, TL, new Vector2(6, -54), new Vector2(220, 30), true, true);
            onlineText.supportRichText = true; onlineText.raycastTarget = false;

            body = UIKit.Rect(panel, "Body", TL, TL, new Vector2(0, -56), new Vector2(560, 206));
            bg = UIKit.Img(body, "flat_Grey", new Color(0.05f, 0.05f, 0.08f, 0.55f), 16f, 1.2f);
            bg.raycastTarget = true;
            // bara cu prietenul cu care vorbesti (doar in FRIENDS)
            dmBar = UIKit.Rect(body, "DmBar", TL, TL, new Vector2(6, -4), new Vector2(548, 30));
            UIKit.Img(dmBar, "flat_Grey", new Color(0.55f, 0.3f, 0.6f, 0.75f), 10f, 1f).raycastTarget = false;
            dmText = UIKit.Text(dmBar, "T", "", 17, Color.white, TextAnchor.MiddleLeft, TL, TL, new Vector2(10, 0), new Vector2(420, 30), false, true);
            dmText.supportRichText = true; dmText.rectTransform.pivot = TL;
            UIKit.Button(dmBar, "Chg", "CHANGE", "Grey", TR, TR, new Vector2(-2, 0), new Vector2(100, 30), () => { if (game.hud.friends != null) game.hud.friends.Open(); }, 14);
            dmBar.gameObject.SetActive(false);
            // log-ul se poate derula (rotita / tragi cu degetul); fara contur dublu - doar o umbra
            viewport = UIKit.Rect(body, "View", TL, TL, new Vector2(8, -4), new Vector2(544, 148));
            var vimg = viewport.gameObject.AddComponent<Image>(); vimg.color = new Color(0, 0, 0, 0.001f);
            viewport.gameObject.AddComponent<RectMask2D>();
            log = UIKit.Text(viewport, "Log", "", 17, Color.white, TextAnchor.LowerLeft, TL, TL, new Vector2(4, 0), new Vector2(530, 148), false, true);
            var lsh = log.gameObject.AddComponent<Shadow>(); lsh.effectColor = new Color(0, 0, 0, 0.9f); lsh.effectDistance = new Vector2(1.5f, -1.5f);
            log.supportRichText = true;
            log.raycastTarget = false;
            log.horizontalOverflow = HorizontalWrapMode.Wrap;
            log.verticalOverflow = VerticalWrapMode.Overflow;
            log.rectTransform.pivot = TL;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = log.rectTransform;
            scroll.viewport = viewport;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 22f;
            scroll.inertia = true;
            scroll.onValueChanged.AddListener(v => { if (!refreshing) stickBottom = scroll.verticalNormalizedPosition <= 0.02f || log.rectTransform.rect.height <= viewport.rect.height + 1f; });
            input = UIKit.MakeInput(body, "Input", BL, BL, new Vector2(8, 8), new Vector2(440, 44), "", ChatFilter.MaxLength, 18);
            placeholder = input.placeholder as Text; UpdatePlaceholder();
            input.lineType = InputField.LineType.SingleLine;
            input.onEndEdit.AddListener(OnEnd);
            UIKit.Button(body, "Send", "SEND", "Green", BR, BR, new Vector2(-8, 8), new Vector2(96, 44), () => Send(input.text), 18);

            Add(0, new Msg { system = true, text = "Welcome to the GLOBAL chat - every online player sees it. Be nice: spam, links and bad words are blocked." });
            Add(1, new Msg { system = true, text = "SERVER chat - only players on your server (max " + TradeDB.MaxPlayers + ") see these messages." });
            Add(2, new Msg { system = true, text = "FRIENDS - private messages. Open FRIENDS (button on the left) and press MESSAGE." });
            // chat-ul porneste strans - se deschide doar cand vrei (ENTER, "/" sau click pe GLOBAL / SERVER)
            minimized = true;
            body.gameObject.SetActive(false);
            minText.text = "+";
            unread[0] = unread[1] = unread[2] = 0;
        }

        bool refreshing;

        void UpdatePlaceholder()
        {
            if (placeholder == null) return;
            placeholder.text = channel == 2 ? (dmTarget != null ? "Message to " + dmTarget + "..." : "Pick a friend in FRIENDS first") : (Device.Mobile ? "Tap here to chat..." : "Press ENTER to chat...");
        }

        /// <summary>Deschide chat-ul pe FRIENDS, cu mesaj catre "name".</summary>
        public void OpenDM(string name)
        {
            dmTarget = name;
            channel = 2; unread[2] = 0; dirty = true; stickBottom = true;
            UpdatePlaceholder();
            if (minimized) SetMin(false);
            input.Select();
            input.ActivateInputField();
        }

        void OnEnable() { ColorRPG.Net.GlobalChat.OnMessage -= OnGlobal; ColorRPG.Net.GlobalChat.OnMessage += OnGlobal; }
        void OnDisable() { ColorRPG.Net.GlobalChat.OnMessage -= OnGlobal; }

        // anti-spam pentru mesajele primite: max 4 mesaje in 10s de la acelasi jucator, fara repetari
        readonly Dictionary<string, List<float>> recvTimes = new Dictionary<string, List<float>>();
        readonly Dictionary<string, string> recvLast = new Dictionary<string, string>();

        bool SpamFrom(string who, string text)
        {
            float now = Time.unscaledTime;
            List<float> l;
            if (!recvTimes.TryGetValue(who, out l)) { l = new List<float>(); recvTimes[who] = l; }
            l.RemoveAll(t => now - t > 10f);
            if (l.Count >= 4) return true;
            string last;
            if (recvLast.TryGetValue(who, out last) && last == text) return true;
            l.Add(now); recvLast[who] = text;
            return false;
        }

        void OnGlobal(string playerId, string name, string text, string meta)
        {
            // anunt de sistem: RESET LEADERBOARDS (doar de la conturile de admin)
            if (meta != null && meta.StartsWith(NetSync.ResetMeta))
            {
                long t;
                if (AdminAccess.IsAdminId(playerId) && long.TryParse(meta.Substring(NetSync.ResetMeta.Length), out t))
                {
                    NetSync.ApplyReset(t);
                    Post(0, "The leaderboards have been reset!");
                }
                return;
            }
            name = ChatFilter.CleanName(name);
            if (ChatSafety.IsBlocked(name)) return;
            string clean = ChatFilter.CleanIncoming(text);
            if (clean == null || SpamFrom(string.IsNullOrEmpty(playerId) ? name : playerId, clean)) return;
            long pw; long.TryParse(meta, out pw);
            Add(0, new Msg { name = name, text = clean, col = Player.PowerTitleCol(pw) });
        }

        /// <summary>Multiplayer: mesaj de la alt jucator din camera.</summary>
        public void Receive(int ch, string name, string text, long power)
        {
            if (SpamFrom(name, text)) return;
            Add(Mathf.Clamp(ch, 0, 1), new Msg { name = name, text = text, col = Player.PowerTitleCol(power) });
        }

        /// <summary>Multiplayer: mesaj privat de la alt jucator.</summary>
        public void ReceiveDm(string from, string text)
        {
            Add(2, new Msg { name = from, text = text, col = new Color(0.6f, 0.9f, 1f), peer = from });
            if (game != null && game.hud != null) game.hud.Toast("Message from " + from, new Color(1f, 0.6f, 0.95f), ItemIcons.Get(33));
        }

        /// <summary>Mesaj de sistem (ex. anunturi / giveaway-uri din ADMIN).</summary>
        public void Post(int ch, string text, bool warn = false)
        {
            Add(Mathf.Clamp(ch, 0, 2), new Msg { system = true, warn = warn, text = text });
        }

        float idleT;
        /// <summary>Chat-ul se strange singur dupa cateva secunde fara folosire; click pe GLOBAL / SERVER il deschide.</summary>
        const float AutoHide = 6f;

        void SetMin(bool m)
        {
            if (onlineText != null) onlineText.gameObject.SetActive(m);
            idleT = 0f;
            minimized = m;
            body.gameObject.SetActive(!m);
            if (!m) { stickBottom = true; dirty = true; }
            minText.text = m ? "+" : "-";
            Sfx.Play("Click", 0.4f);
        }

        void OnEnd(string s)
        {
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            bool enter = k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
#else
            bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
            if (enter) Send(s);
        }

        int sentFrame = -10;

        void Send(string s)
        {
            sentFrame = Time.frameCount;
            s = s == null ? "" : s.Trim();
            input.text = "";
            input.DeactivateInputField();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            if (s.Length == 0) return;
            // anti-spam, fara link-uri, fara injuraturi
            string why = ChatFilter.Check(ref s);
            if (why != null)
            {
                if (why.Length > 0) { Add(channel, new Msg { system = true, warn = true, text = why }); Sfx.Play("Error", 0.4f); }
                return;
            }
            var p = game.player;
            if (channel == 2)
            {
                if (dmTarget == null) { Add(2, new Msg { system = true, warn = true, text = "Open FRIENDS (button on the left) and press MESSAGE on a friend." }); Sfx.Play("Error", 0.4f); return; }
                stickBottom = true;
                Add(2, new Msg { name = "You", text = s, col = new Color(1f, 0.6f, 0.95f), you = true, peer = dmTarget });
                if (Mp.Online)
                {
                    if (!Mp.SendDm(dmTarget, s)) Add(2, new Msg { system = true, text = dmTarget + " is not in your area right now (you can message players on the same map or market server).", peer = dmTarget });
                }
                else if (Social.IsOnline(dmTarget)) { if (Random.value < 0.75f) pendingReplies.Add(new KeyValuePair<float, string>(Time.unscaledTime + Random.Range(2f, 5f), dmTarget)); }
                else Add(2, new Msg { system = true, text = dmTarget + " is offline - they will see your message when they log in.", peer = dmTarget });
                Sfx.Play("Click", 0.4f, 1.3f);
                return;
            }
            if (Social.Active(p.D.playerName, "MUTE") != null) { Add(channel, new Msg { system = true, warn = true, text = "You are muted." }); return; }
            stickBottom = true;
            Add(channel, new Msg { name = p.D.playerName, text = s, col = Player.PowerTitleCol(p.Power), you = true });
            if (channel == 1) game.hud.ShowBubble(s);
            // GLOBAL = toti jucatorii (Vivox); SERVER = jucatorii din camera ta
            if (channel == 0 && ColorRPG.Net.GlobalChat.Ready) ColorRPG.Net.GlobalChat.Send(s, p.Power.ToString());
            else Mp.SendChat(channel, s);   // bula deasupra capului - doar pe serverul curent (SERVER)
            Sfx.Play("Click", 0.4f, 1.3f);
        }

        void Add(int ch, Msg m)
        {
            if (!m.system && !m.you && ChatSafety.IsBlocked(m.name)) return;   // jucator blocat: nu-i mai vezi mesajele
            var l = msgs[ch];
            l.Add(m);
            if (l.Count > (ch == 2 ? Keep * 3 : Keep)) l.RemoveAt(0);
            if (ch != channel || minimized) unread[ch]++;
            dirty = true;
        }

        static string Esc(string s) { return s.Replace("<", "‹").Replace(">", "›"); }

        void Refresh()
        {
            dirty = false;
            refreshing = true;
            var l = msgs[channel];
            var sb = new System.Text.StringBuilder();
            // FRIENDS: doar conversatia cu prietenul ales (ramane pana schimbi prietenul sau harta)
            bool dm = channel == 2 && dmTarget != null;
            dmBar.gameObject.SetActive(channel == 2);
            if (channel == 2)
                dmText.text = dmTarget == null ? "<color=#ffd8ff>No friend selected</color>"
                                               : "Chatting with <b>" + Esc(dmTarget) + "</b>   " + (Social.IsOnline(dmTarget) ? "<color=#5dff73>● online</color>" : "<color=#b0b0c0>● offline</color>");
            viewport.anchoredPosition = new Vector2(8, channel == 2 ? -36 : -4);
            viewport.sizeDelta = new Vector2(544, channel == 2 ? 116 : 148);
            int from = Mathf.Max(0, l.Count - Keep);
            for (int i = from; i < l.Count; i++)
            {
                var m = l[i];
                if (dm && m.peer != null && m.peer != dmTarget) continue;
                if (dm && m.peer == null && !m.system) continue;
                if (!m.system && !m.you && ChatSafety.IsBlocked(m.name)) continue;
                if (sb.Length > 0) sb.Append('\n');
                if (m.system) sb.Append(m.warn ? "<color=#ff7a6b>" : "<color=#ffd84d>").Append(Esc(m.text)).Append("</color>");
                else
                    sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(m.col)).Append(">").Append(m.you && channel != 2 ? "[YOU] " : "").Append(Esc(m.name)).Append(":</color> ").Append(Esc(m.text));
            }
            log.text = sb.ToString();
            log.rectTransform.sizeDelta = new Vector2(530, Mathf.Max(viewport.sizeDelta.y, log.preferredHeight + 6f));
            if (stickBottom) { Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 0f; }
            refreshing = false;
            for (int i = 0; i < 3; i++)
            {
                tabImgs[i].sprite = UIKit.Sprite(i == channel ? (i == 2 ? "btn_Purple" : "btn_Blue") : "btn_Grey", 18f);
                var t = tabImgs[i].GetComponentInChildren<Text>();
                string baseName = TabNames[i];
                t.text = unread[i] > 0 && i != channel ? baseName + " (" + Mathf.Min(99, unread[i]) + ")" : baseName;
            }
        }

        // ---------------- REPORT / BLOCK ----------------
        RectTransform safety;
        string safetyPick;   // jucatorul ales pentru raport

        void ToggleSafety()
        {
            if (safety != null) { Destroy(safety.gameObject); safety = null; return; }
            safetyPick = null;
            BuildSafety();
        }

        /// <summary>Jucatorii care au scris recent (in toate canalele), cei mai noi primii.</summary>
        List<string> RecentNames()
        {
            var l = new List<string>();
            for (int ch = 0; ch < 3; ch++)
                for (int i = msgs[ch].Count - 1; i >= 0; i--)
                {
                    var m = msgs[ch][i];
                    if (m.system || m.you || string.IsNullOrEmpty(m.name) || l.Contains(m.name)) continue;
                    l.Add(m.name);
                }
            return l;
        }

        List<string> LastLinesOf(string name)
        {
            var l = new List<string>();
            for (int ch = 0; ch < 3; ch++) foreach (var m in msgs[ch]) if (!m.system && m.name == name) l.Add("[" + TabNames[ch] + "] " + m.text);
            if (l.Count > 10) l.RemoveRange(0, l.Count - 10);
            return l;
        }

        void BuildSafety()
        {
            if (safety != null) Destroy(safety.gameObject);
            Vector2 TL = new Vector2(0, 1), TR = new Vector2(1, 1), C = new Vector2(0.5f, 0.5f);
            safety = UIKit.Rect(panel, "Safety", TL, TL, new Vector2(0, -56), new Vector2(560, 360));
            UIKit.Img(safety, "panel_dark", Color.white, 20f, 1f).raycastTarget = true;
            UIKit.Button(safety, "X", "X", "Red", TR, TR, new Vector2(-8, -8), new Vector2(40, 40), ToggleSafety, 20);
            if (safetyPick != null)
            {
                // alegi motivul raportului
                UIKit.Text(safety, "T", "REPORT " + Esc(safetyPick).ToUpper(), 22, new Color(1f, 0.55f, 0.5f), TextAnchor.MiddleLeft, TL, TL, new Vector2(16, -10), new Vector2(480, 36));
                UIKit.Text(safety, "S", "Why are you reporting this player?", 16, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleLeft, TL, TL, new Vector2(16, -46), new Vector2(520, 26), false, true);
                for (int i = 0; i < ChatSafety.Reasons.Length; i++)
                {
                    int r = i; string who = safetyPick;
                    UIKit.Button(safety, "R" + i, ChatSafety.Reasons[i], "Grey", TL, TL, new Vector2(16, -80 - i * 54), new Vector2(528, 48), () =>
                    {
                        ChatSafety.Report(who, r, LastLinesOf(who));
                        game.hud.Toast("REPORT SENT\n" + who + " is blocked. Press SEND in your email app to finish the report.", new Color(1f, 0.6f, 0.5f));
                        dirty = true; safetyPick = null; ToggleSafety();
                    }, 16);
                }
                UIKit.Button(safety, "Back", "BACK", "Grey", TL, TL, new Vector2(16, -300), new Vector2(140, 44), () => { safetyPick = null; BuildSafety(); }, 16);
                return;
            }
            UIKit.Text(safety, "T", "REPORT / BLOCK PLAYERS", 22, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleLeft, TL, TL, new Vector2(16, -10), new Vector2(480, 36));
            UIKit.Text(safety, "S", "Blocked players can't be seen in your chat.", 15, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleLeft, TL, TL, new Vector2(16, -44), new Vector2(520, 24), false, true);
            var names = RecentNames();
            foreach (var b in ChatSafety.Blocked) if (!names.Contains(b)) names.Add(b);
            if (names.Count == 0) UIKit.Text(safety, "E", "No players have written in chat yet.", 17, Color.white, TextAnchor.MiddleCenter, TL, TL, new Vector2(16, -90), new Vector2(528, 40), false, true);
            int n = Mathf.Min(names.Count, 5);
            for (int i = 0; i < n; i++)
            {
                string who = names[i];
                float y = -76 - i * 54;
                bool isB = ChatSafety.IsBlocked(who);
                UIKit.Text(safety, "N" + i, Esc(who) + (isB ? "  <color=#ff7a6b>(blocked)</color>" : ""), 18, Color.white, TextAnchor.MiddleLeft, TL, TL, new Vector2(16, y - 4), new Vector2(250, 44), false, true).supportRichText = true;
                UIKit.Button(safety, "B" + i, isB ? "UNBLOCK" : "BLOCK", isB ? "Grey" : "Orange", TL, TL, new Vector2(272, y), new Vector2(130, 46), () =>
                {
                    if (ChatSafety.IsBlocked(who)) ChatSafety.Unblock(who); else { ChatSafety.Block(who); game.hud.Toast("PLAYER BLOCKED\nYou won't see messages from " + who + ".", new Color(1f, 0.7f, 0.4f)); }
                    dirty = true; BuildSafety();
                }, 15);
                UIKit.Button(safety, "Rp" + i, "REPORT", "Red", TL, TL, new Vector2(410, y), new Vector2(134, 46), () => { safetyPick = who; BuildSafety(); }, 15);
            }
        }

        string RandomName()
        {
            var n = TradeDB.ShopperNames;
            for (int t = 0; t < 10; t++)
            {
                string s = n[Random.Range(0, n.Length)];
                if (!Social.Silenced(s)) return s;   // jucatorii banati / cu mute nu mai scriu
            }
            return n[0];
        }

        Text onlineText;
        float onlineT;

        void Update()
        {
            if (game == null || game.player == null) return;
            if (onlineText != null && (onlineT -= Time.unscaledDeltaTime) <= 0f)
            {
                onlineT = 3f;
                int n = ColorRPG.Net.GlobalChat.OnlineCount;
                if (n <= 0 && ColorRPG.Net.MpLink.InSession) n = Mathf.Max(1, ColorRPG.Net.MpLink.PlayerCount);
                onlineText.text = n > 0 ? "<color=#7dff8a>Online:</color> " + n.ToString("N0") : "";
            }
            // ENTER sau "/" = deschide scrisul
            if (!Inp.Typing && !game.hud.Blocking && Time.frameCount - sentFrame > 2 && Inp.ChatDown())
            {
                if (minimized) SetMin(false);
                input.Select();
                input.ActivateInputField();
            }
            if (Focused && Inp.EscapeDown()) { input.text = ""; input.DeactivateInputField(); UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null); }

            // jucatori simulati (doar offline; online vorbesc jucatorii reali)
            if (Mp.Online || ColorRPG.Net.Ugs.Ready) { simT = 8f; simT2 = 15f; pendingReplies.Clear(); }
            simT -= Time.unscaledDeltaTime;
            if (simT <= 0f)
            {
                simT = Random.Range(5f, 13f);
                long pw = (long)(game.player.Power * Random.Range(0.3f, 6f));
                Add(0, new Msg { name = RandomName(), text = Lines[Random.Range(0, Lines.Length)], col = Player.PowerTitleCol(pw) });
            }
            simT2 -= Time.unscaledDeltaTime;
            if (simT2 <= 0f)
            {
                simT2 = Random.Range(12f, 26f);
                long pw = (long)(game.player.Power * Random.Range(0.5f, 3f));
                Add(1, new Msg { name = RandomName(), text = Lines[Random.Range(0, Lines.Length)], col = Player.PowerTitleCol(pw) });
            }
            // raspunsurile prietenilor online (simulat)
            for (int i = pendingReplies.Count - 1; i >= 0; i--)
                if (Time.unscaledTime >= pendingReplies[i].Key)
                {
                    Add(2, new Msg { name = pendingReplies[i].Value, text = Social.RandomReply(), col = new Color(0.6f, 0.9f, 1f), peer = pendingReplies[i].Value });
                    game.hud.Toast("Message from " + pendingReplies[i].Value, new Color(1f, 0.6f, 0.95f), ItemIcons.Get(33));
                    pendingReplies.RemoveAt(i);
                }
            // conversatiile private se sterg cand schimbi harta (ca in cerinta: raman pana pleci)
            int mk = game.InPlaza ? -100 : game.mapIndex;
            if (lastMapKey == int.MinValue) lastMapKey = mk;
            else if (mk != lastMapKey)
            {
                lastMapKey = mk;
                msgs[2].RemoveAll(m => m.peer != null);
                dirty = true;
            }
            bg.color = new Color(0.05f, 0.05f, 0.08f, Focused ? 0.8f : 0.5f);
            // se inchide singur daca nu-l folosesti (nu scrii si nu ai mouse-ul pe el)
            if (!minimized)
            {
                bool over = RectTransformUtility.RectangleContainsScreenPoint(panel, Inp.MousePos(), null) || RectTransformUtility.RectangleContainsScreenPoint(body, Inp.MousePos(), null);
                if (Focused || over) idleT = 0f;
                else if ((idleT += Time.unscaledDeltaTime) > AutoHide) { minimized = true; body.gameObject.SetActive(false); minText.text = "+"; if (onlineText != null) onlineText.gameObject.SetActive(true); }
            }
            if (dirty) Refresh();
        }
    }
}
