// =====================================================================
//  TradeHubUI.cs - butonul TRADING: fereastra MARKETS.
//   * tab-uri sus: ALL MARKETS / MY REGION / FRIENDS
//   * grila de piete (3 x 3): numele pietei, regiunea, cat de plina e
//     (bara colorata) si ENTER
//   * jos: QUICK JOIN (o piata cu locuri libere) + PRIVATE MARKET
//  Deocamdata pietele sunt simulate; cand jocul va fi online lista vine de la server.
// =====================================================================
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class TradeHubUI : Window
    {
        int filter;             // 0 = toate, 1 = regiunea ta, 2 = prieteni
        int page, fpage;
        const int Cols = 3, RowsN = 3;

        static readonly Color[] NounCols =
        {
            new Color(1f, 0.72f, 0.35f), new Color(0.5f, 0.85f, 1f), new Color(0.6f, 1f, 0.6f),
            new Color(0.55f, 0.75f, 1f), new Color(1f, 0.6f, 0.85f)
        };

        public static TradeHubUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("TradeHubUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<TradeHubUI>();
            w.Init(parent, g, "TradeHubDim", "MARKETS", new Vector2(1360, 820), "Orange");
            return w;
        }

        protected override void Build()
        {
            var ts = TradeSystem.I;
            if (ts == null) return;
            var D = game.player.D;

            // ---- tab-uri
            string[] names = { "ALL MARKETS", "MY REGION", "FRIENDS (" + D.friends.Count + ")" };
            for (int i = 0; i < names.Length; i++)
            {
                int k = i;
                UIKit.Fit(UIKit.Button(content, "T" + i, names[i], filter == i ? "Orange" : "Grey", TL, TL, new Vector2(24 + i * 214, -18), new Vector2(204, 56),
                                       () => { filter = k; page = 0; Rebuild(); }, 20).GetComponentInChildren<Text>(), 12);
            }
            // ---- cauta / adauga un prieten (dreapta-sus)
            var field = UIKit.MakeInput(content, "Find", TR, TR, new Vector2(-124, -18), new Vector2(330, 56), "", 24, 20);
            var ph = field.placeholder as Text; if (ph != null) ph.text = filter == 2 ? "Add a friend..." : "Find a friend...";
            UIKit.Button(content, "Go", filter == 2 ? "ADD" : "GO", "Blue", TR, TR, new Vector2(-24, -18), new Vector2(92, 56), () =>
            {
                if (filter == 2) { if (ts.AddFriend(field.text)) Rebuild(); return; }
                game.Say("Joining a friend's market will work when the game is online!", 2.5f);
                Sfx.Play("Error", 0.4f);
            }, 22);

            if (filter == 2) BuildFriends(ts, D);
            else BuildMarkets(ts, D);

            // ---- jos: QUICK JOIN + PRIVATE MARKET
            UIKit.Button(content, "Quick", "QUICK JOIN", "Green", BR, BR, new Vector2(-340, 20), new Vector2(300, 72), () =>
            {
                int best = 1;
                for (int t = 0; t < 30; t++) { int id = Random.Range(1, TradeDB.ServerCount + 1); if (ts.PeekPlayers(id) < TradeDB.MaxPlayers) { best = id; break; } }
                Close(); ts.JoinFromHub(best);
            }, 28);
            bool here = ts.Server != null && TradeSystem.IsPrivate(ts.Server.id) && game.InPlaza;
            UIKit.Fit(UIKit.Button(content, "Private", !D.passPrivate ? "PRIVATE MARKET" : here ? "YOU ARE HERE" : "MY PRIVATE MARKET", "Purple", BR, BR, new Vector2(-24, 20), new Vector2(300, 72), () =>
            {
                if (!D.passPrivate)
                {
                    game.Say("Get the PRIVATE SERVER pass in the SHOP - then invite up to " + (TradeDB.MaxPlayers - 1) + " friends!", 3f);
                    Close(); game.hud.shop.Open();
                    return;
                }
                if (here) return;
                Close(); ts.JoinFromHub(TradeSystem.PrivateId);
            }, 24).GetComponentInChildren<Text>(), 12);
        }

        void BuildMarkets(TradeSystem ts, SaveData D)
        {
            string myRegion = TradeSystem.Region(Mathf.Max(1, D.serverId));
            var list = new System.Collections.Generic.List<int>();
            for (int id = 1; id <= TradeDB.ServerCount; id++)
            {
                int pl = ts.PeekPlayers(id);
                if (filter == 1 && TradeSystem.Region(id) != myRegion) continue;
                list.Add(id);
            }
            int perPage = Cols * RowsN;
            int pages = Mathf.Max(1, (list.Count + perPage - 1) / perPage);
            page = Mathf.Clamp(page, 0, pages - 1);
            if (list.Count == 0)
                Label(content, "No markets match this filter.", 26, new Color(1, 1, 1, 0.75f), new Vector2(24, -320), new Vector2(1264, 50), TextAnchor.MiddleCenter);

            float cw = 412f, ch = 170f;
            for (int i = 0; i < perPage && page * perPage + i < list.Count; i++)
            {
                int id = list[page * perPage + i];
                int pl = ts.PeekPlayers(id);
                bool cur = ts.Server != null && ts.Server.id == id && game.InPlaza;
                bool full = pl >= TradeDB.MaxPlayers && !cur;
                bool booth = ts.HasBooth && D.boothServer == id;
                int c = i % Cols, r = i / Cols;
                Color nc = NounCols[((id - 1) / 8) % NounCols.Length];
                var card = Card(content, new Vector2(24 + c * (cw + 14), -94 - r * (ch + 12)), new Vector2(cw, ch),
                                cur ? new Color(0.25f, 0.5f, 0.3f) : new Color(0.24f, 0.25f, 0.3f));
                // banda colorata sus + numele pietei
                var band = UIKit.Rect(card, "Band", TL, TL, new Vector2(10, -10), new Vector2(cw - 20, 7)).gameObject.AddComponent<Image>();
                band.color = nc; band.raycastTarget = false;
                var nm = Label(card, TradeSystem.ServerName(id), 28, nc, new Vector2(18, -24), new Vector2(cw - 110, 40));
                UIKit.Fit(nm, 16);
                // regiunea (eticheta mica dreapta-sus)
                var reg = UIKit.Rect(card, "Reg", TR, TR, new Vector2(-14, -26), new Vector2(76, 32));
                UIKit.Img(reg, "flat_Grey", new Color(0.1f, 0.12f, 0.2f, 0.9f), 12f, 1f);
                UIKit.Text(reg, "T", TradeSystem.Region(id), 16, new Color(0.75f, 0.85f, 1f), TextAnchor.MiddleCenter, C, C, new Vector2(0, 1), new Vector2(76, 30), true, true);
                // cat de plina e piata
                float f = pl / (float)TradeDB.MaxPlayers;
                Color fc = f >= 1f ? new Color(1f, 0.4f, 0.4f) : f > 0.75f ? new Color(1f, 0.8f, 0.3f) : new Color(0.45f, 0.95f, 0.5f);
                var bg = UIKit.Rect(card, "Bar", TL, TL, new Vector2(18, -76), new Vector2(cw - 36, 18));
                UIKit.Img(bg, "flat_Grey", new Color(0.08f, 0.09f, 0.14f, 1f), 8f, 2f);
                var fill = UIKit.Rect(bg, "Fill", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(2, 0), new Vector2((cw - 40) * Mathf.Clamp01(f), 14));
                UIKit.Img(fill, "flat_Grey", fc, 8f, 2f);
                string busy = f >= 1f ? "Full" : f > 0.75f ? "Busy" : f > 0.5f ? "Lively" : "Quiet";
                Label(card, pl + " / " + TradeDB.MaxPlayers + " traders  -  " + busy + (booth ? "   <color=#ffd84d>YOUR BOOTH</color>" : ""), 18, new Color(1, 1, 1, 0.85f), new Vector2(18, -100), new Vector2(cw - 36, 26));
                int sid = id;
                UIKit.Button(card, "Enter", cur ? "HERE" : full ? "FULL" : "ENTER", cur ? "Green" : full ? "Grey" : "Orange", BR, BR, new Vector2(-12, 12), new Vector2(150, 50),
                             () => { if (!cur && !full) { Close(); ts.JoinFromHub(sid); } }, 22).interactable = !cur && !full;
            }
            Pager(pages, () => page, v => page = v);
        }

        /// <summary>Tab-ul FRIENDS: adaugi prieteni (max 100), ii inviti pe piata privata, ii stergi.</summary>
        void BuildFriends(TradeSystem ts, SaveData D)
        {
            Label(content, "FRIENDS  " + D.friends.Count + " / " + TradeSystem.MaxFriends +
                  (D.passPrivate ? "      PRIVATE MARKET: " + (1 + D.privateInvites.Count) + " / " + TradeDB.MaxPlayers : ""),
                  22, new Color(1f, 0.85f, 0.45f), new Vector2(24, -90), new Vector2(1264, 34));
            int perPage = 6;
            int pages = Mathf.Max(1, (D.friends.Count + perPage - 1) / perPage);
            fpage = Mathf.Clamp(fpage, 0, pages - 1);
            if (D.friends.Count == 0)
                Label(content, "No friends yet - type a username above and press ADD.", 24, new Color(1, 1, 1, 0.75f),
                      new Vector2(24, -300), new Vector2(1264, 60), TextAnchor.MiddleCenter);
            for (int i = 0; i < perPage && fpage * perPage + i < D.friends.Count; i++)
            {
                string f = D.friends[fpage * perPage + i];
                bool inv = D.privateInvites.Contains(f);
                int c = i % 2, r = i / 2;
                var row = Card(content, new Vector2(24 + c * 640, -134 - r * 150), new Vector2(626, 138), inv ? new Color(0.6f, 0.5f, 0.2f) : new Color(0.24f, 0.25f, 0.3f));
                Label(row, f, 30, Color.white, new Vector2(20, -16), new Vector2(420, 42));
                Label(row, "Offline" + (inv ? "   -   <color=#ffd84d>INVITED</color>" : ""), 18, new Color(1, 1, 1, 0.75f), new Vector2(20, -60), new Vector2(420, 26));
                string ff = f;
                if (D.passPrivate)
                    UIKit.Button(row, "Inv", inv ? "UNINVITE" : "INVITE", inv ? "Grey" : "Yellow", BR, BR, new Vector2(-170, 14), new Vector2(150, 50), () => { ts.ToggleInvite(ff); Rebuild(); }, 20);
                UIKit.Button(row, "Rem", "REMOVE", "Red", BR, BR, new Vector2(-12, 14), new Vector2(150, 50), () => { ts.RemoveFriend(ff); Rebuild(); }, 20);
            }
            Pager(pages, () => fpage, v => fpage = v);
        }

        void Pager(int pages, System.Func<int> get, System.Action<int> set)
        {
            if (pages <= 1) return;
            UIKit.Button(content, "Prev", "<", "Grey", BL, BL, new Vector2(24, 28), new Vector2(70, 56), () => { set((get() + pages - 1) % pages); Rebuild(); }, 30);
            Label(content, "PAGE " + (get() + 1) + " / " + pages, 22, Color.white, new Vector2(104, 28), new Vector2(150, 56), TextAnchor.MiddleCenter, BL).rectTransform.pivot = BL;
            UIKit.Button(content, "Next", ">", "Grey", BL, BL, new Vector2(264, 28), new Vector2(70, 56), () => { set((get() + 1) % pages); Rebuild(); }, 30);
        }
    }
}
