// =====================================================================
//  FriendsUI.cs - butonul FRIENDS (stanga): cauti jucatori dupa nume,
//  ii adaugi la prieteni, le scrii mesaje (merg in chat, tab-ul FRIENDS),
//  ii inviti pe piata privata sau ii stergi.
// =====================================================================
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class FriendsUI : Window
    {
        string query = "";
        int fpage;
        InputField searchField;

        public static FriendsUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("FriendsUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<FriendsUI>();
            w.Init(parent, g, "FriendsDim", "FRIENDS", new Vector2(1320, 800), "Blue");
            return w;
        }

        static string Dot(bool on) { return on ? "<color=#5dff73>● ONLINE</color>" : "<color=#8890aa>● offline</color>"; }

        protected override void Build()
        {
            var p = game.player; var D = p.D;
            var ts = TradeSystem.I;

            // ================= stanga: cauta jucatori =================
            var left = Card(content, new Vector2(24, -16), new Vector2(560, 690), new Color(0.2f, 0.22f, 0.34f));
            Label(left, "FIND PLAYERS", 26, new Color(1f, 0.85f, 0.45f), new Vector2(18, -12), new Vector2(400, 36));
            searchField = UIKit.MakeInput(left, "Search", TL, TL, new Vector2(18, -56), new Vector2(380, 56), query, 20, 22);
            var ph = searchField.placeholder as Text; if (ph != null) ph.text = "Player name...";
            searchField.onEndEdit.AddListener(v => { query = v; Rebuild(); });
            UIKit.Button(left, "Go", "SEARCH", "Blue", TL, TL, new Vector2(408, -56), new Vector2(134, 56), () => { query = searchField.text; Rebuild(); }, 20);

            var res = Social.Search(query, 8);
            float y = -128;
            string typed = (query ?? "").Trim();
            bool exact = false;
            foreach (var n in res) if (n.ToLowerInvariant() == typed.ToLowerInvariant()) exact = true;
            if (typed.Length >= 3 && !exact && typed != D.playerName)
            {
                // numele scris nu e in lista (inca) - poti trimite cerere direct
                var r = Card(left, new Vector2(16, y), new Vector2(528, 62), new Color(0.3f, 0.26f, 0.45f));
                Label(r, "Send a request to  <b>" + Esc(typed) + "</b>", 19, Color.white, new Vector2(14, -14), new Vector2(360, 34));
                string tn = typed;
                UIKit.Button(r, "Add", "ADD", "Green", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(110, 46), () => { if (ts != null && ts.AddFriend(tn)) Rebuild(); }, 18);
                y -= 68;
            }
            if (res.Length == 0)
                Label(left, "No players found.", 20, new Color(1, 1, 1, 0.6f), new Vector2(18, y - 10), new Vector2(520, 30));
            foreach (var n in res)
            {
                if (y < -640) break;
                bool isFriend = D.friends.Exists(f => f.ToLowerInvariant() == n.ToLowerInvariant());
                var r = Card(left, new Vector2(16, y), new Vector2(528, 62), new Color(0.26f, 0.27f, 0.33f));
                Label(r, Esc(n), 22, Color.white, new Vector2(14, -6), new Vector2(300, 30));
                Label(r, Dot(Social.IsOnline(n)) + "   <color=#b8c4d6>Lv " + Social.FakeLevel(n) + "</color>", 15, Color.white, new Vector2(14, -34), new Vector2(300, 22));
                string nn = n;
                if (isFriend)
                    Label(r, "FRIEND", 18, new Color(0.5f, 1f, 0.55f), new Vector2(-14, 0), new Vector2(110, 40), TextAnchor.MiddleRight, new Vector2(1, 0.5f)).rectTransform.pivot = new Vector2(1, 0.5f);
                else
                    UIKit.Button(r, "Add", "ADD", "Green", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(110, 46), () => { if (ts != null && ts.AddFriend(nn)) Rebuild(); }, 18);
                y -= 68;
            }

            // ================= dreapta: prietenii mei =================
            var right = Card(content, new Vector2(600, -16), new Vector2(696, 690), new Color(0.2f, 0.22f, 0.34f));
            int online = 0; foreach (var f in D.friends) if (Social.IsOnline(f)) online++;
            Label(right, "MY FRIENDS  <color=#ffd84d>" + D.friends.Count + "/" + TradeSystem.MaxFriends + "</color>   <color=#5dff73>" + online + " online</color>",
                  24, Color.white, new Vector2(18, -12), new Vector2(660, 36));
            var list = new System.Collections.Generic.List<string>(D.friends);
            list.Sort((a, b) => Social.IsOnline(a) != Social.IsOnline(b) ? (Social.IsOnline(a) ? -1 : 1) : string.Compare(a, b, System.StringComparison.OrdinalIgnoreCase));
            const int per = 7;
            int pages = Mathf.Max(1, (list.Count + per - 1) / per);
            fpage = Mathf.Clamp(fpage, 0, pages - 1);
            if (list.Count == 0)
                Label(right, "No friends yet.\nSearch a name on the left and press ADD.", 22, new Color(1, 1, 1, 0.65f), new Vector2(18, -260), new Vector2(660, 80), TextAnchor.MiddleCenter);
            for (int i = 0; i < per && fpage * per + i < list.Count; i++)
            {
                string f = list[fpage * per + i];
                bool on = Social.IsOnline(f);
                bool inv = D.privateInvites.Contains(f);
                var r = Card(right, new Vector2(16, -62 - i * 82), new Vector2(664, 76), on ? new Color(0.24f, 0.36f, 0.3f) : new Color(0.26f, 0.27f, 0.33f));
                Label(r, Esc(f), 24, Color.white, new Vector2(14, -8), new Vector2(260, 32));
                Label(r, Dot(on) + (inv ? "   <color=#ffd84d>INVITED</color>" : ""), 15, Color.white, new Vector2(14, -42), new Vector2(260, 24));
                string ff = f;
                UIKit.Button(r, "Msg", "MESSAGE", "Purple", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-262, 0), new Vector2(132, 50), () =>
                {
                    Close();
                    if (ChatUI.I != null) ChatUI.I.OpenDM(ff);
                }, 17);
                if (D.passPrivate)
                    UIKit.Button(r, "Inv", inv ? "UNINVITE" : "INVITE", inv ? "Grey" : "Yellow", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-136, 0), new Vector2(122, 50), () => { if (ts != null) ts.ToggleInvite(ff); Rebuild(); }, 16);
                UIKit.Button(r, "Rem", "X", "Red", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(50, 50), () => { if (ts != null) ts.RemoveFriend(ff); Rebuild(); }, 20);
            }
            if (pages > 1)
            {
                UIKit.Button(right, "Prev", "<", "Grey", BL, BL, new Vector2(16, 14), new Vector2(60, 50), () => { fpage = (fpage + pages - 1) % pages; Rebuild(); }, 26);
                Label(right, "PAGE " + (fpage + 1) + " / " + pages, 18, Color.white, new Vector2(84, 14), new Vector2(140, 50), TextAnchor.MiddleCenter, BL);
                UIKit.Button(right, "Next", ">", "Grey", BL, BL, new Vector2(232, 14), new Vector2(60, 50), () => { fpage = (fpage + 1) % pages; Rebuild(); }, 26);
            }
        }

        static string Esc(string s) { return (s ?? "").Replace("<", "‹").Replace(">", "›"); }
    }
}
