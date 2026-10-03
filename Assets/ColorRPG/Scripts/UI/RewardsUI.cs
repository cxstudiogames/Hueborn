// =====================================================================
//  RewardsUI.cs - fereastra REWARDS: tab ONLINE (10 cadouri pentru timpul
//  jucat azi) si tab 7 DAYS (cate un cadou pe zi). Logica e in Rewards.cs.
// =====================================================================
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class RewardsUI : Window
    {
        int tab;            // 0 = ONLINE, 1 = 7 DAYS
        float refreshT;
        static readonly Color Gold = new Color(1f, 0.85f, 0.35f);

        public static RewardsUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("RewardsUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<RewardsUI>();
            w.Init(parent, g, "RewardsDim", "REWARDS", new Vector2(1320, 830), "Yellow");
            return w;
        }

        public void OpenTab(int t) { tab = t; if (IsOpen) Rebuild(); else Open(); }

        protected override void Update()
        {
            base.Update();
            if (!IsOpen) return;
            refreshT -= Time.unscaledDeltaTime;
            if (refreshT <= 0f) { refreshT = 1f; Rebuild(); }   // numaratoarea inversa
        }

        protected override void Build()
        {
            // tab-uri (cu punct rosu cand ai ceva de luat acolo)
            bool onReady = false;
            for (int i = 0; i < Rewards.OnlineMin.Length; i++) if (Rewards.OnlineReady(i)) onReady = true;
            Tab(0, "ONLINE", new Vector2(30, -20), onReady);
            Tab(1, "7 DAYS", new Vector2(300, -20), Rewards.WeekReady);
            if (tab == 0) BuildOnline(); else BuildWeek();
        }

        void Tab(int t, string label, Vector2 pos, bool dot)
        {
            var b = UIKit.Button(content, "Tab" + t, label, tab == t ? "Yellow" : "Grey", TL, TL, pos, new Vector2(256, 64), () => { tab = t; Sfx.Play("Click", 0.5f); Rebuild(); }, 26);
            if (dot)
            {
                var d = UIKit.Rect(b.transform, "Dot", TR, C, new Vector2(-8, -8), new Vector2(26, 26));
                UIKit.Img(d, "round_Red", Color.white, 13f).raycastTarget = false;
            }
        }

        static string Clock(float sec)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(sec));
            return s >= 3600 ? (s / 3600) + "h " + (s % 3600 / 60).ToString("00") + "m" : (s / 60) + ":" + (s % 60).ToString("00");
        }

        void BuildOnline()
        {
            float mins = Rewards.OnlineMinutes;
            Label(content, "Online now: <color=#ffd84d>" + Clock(mins * 60f) + "</color>   -   stay online to unlock gifts (resets when you leave the game)", 22, Color.white, new Vector2(590, -24), new Vector2(700, 56));
            for (int i = 0; i < Rewards.OnlineMin.Length; i++)
            {
                int c = i % 5, r = i / 5;
                bool last = i == Rewards.OnlineMin.Length - 1;
                bool claimed = Rewards.OnlineClaimed(i), ready = Rewards.OnlineReady(i);
                var rw = Rewards.Online[i];
                Color main = last ? new Color(1f, 0.55f, 0.85f) : Rewards.Col(rw[0]);
                var card = Card(content, new Vector2(30 + c * 254, -110 - r * 318), new Vector2(240, 304),
                                claimed ? new Color(0.12f, 0.13f, 0.18f) : ready ? Color.Lerp(new Color(0.16f, 0.18f, 0.28f), main, 0.4f) : new Color(0.16f, 0.18f, 0.28f));
                int m = Rewards.OnlineMin[i];
                Label(card, m >= 60 ? (m / 60) + (m % 60 > 0 ? "h " + (m % 60) + "m" : (m >= 120 ? " HOURS" : " HOUR")) : m + " MIN", 26, last ? main : Gold, new Vector2(0, -10), new Vector2(240, 36), TextAnchor.MiddleCenter, TC);
                Icons(card, rw, claimed, last);
                Label(card, Desc(rw), 18, claimed ? new Color(1, 1, 1, 0.4f) : Color.white, new Vector2(0, -170), new Vector2(228, 52), TextAnchor.MiddleCenter, TC);
                if (claimed) Label(card, "CLAIMED", 22, new Color(0.55f, 1f, 0.6f, 0.8f), new Vector2(0, 26), new Vector2(240, 40), TextAnchor.MiddleCenter, BC);
                else if (ready)
                {
                    int k = i;
                    UIKit.Button(card, "Claim", "CLAIM", "Green", BC, BC, new Vector2(0, 16), new Vector2(200, 58), () => { Rewards.ClaimOnline(k); Rebuild(); }, 24);
                }
                else Label(card, "in " + Clock(m * 60f - mins * 60f), 20, new Color(0.75f, 0.8f, 0.95f), new Vector2(0, 26), new Vector2(240, 40), TextAnchor.MiddleCenter, BC);
            }
        }

        void BuildWeek()
        {
            int cur = Rewards.WeekIndex;
            bool ready = Rewards.WeekReady;
            Label(content, ready ? "Your <color=#ffd84d>DAY " + (cur + 1) + "</color> gift is ready!" : "Come back tomorrow for <color=#ffd84d>DAY " + (cur + 1) + "</color>  -  missing a day never resets your progress",
                  22, Color.white, new Vector2(590, -24), new Vector2(700, 56));
            for (int d = 0; d < 7; d++)
            {
                bool big = d == 6;
                bool done = d < cur, today = d == cur;
                var rw = Rewards.Week[d];
                Color main = big ? ItemDB.Col(4) : Rewards.Col(rw[0]);
                float x = 30 + d * 176, w = big ? 200 : 166;
                if (big) x -= 4;
                var card = Card(content, new Vector2(x, -110), new Vector2(w, 620),
                                done ? new Color(0.12f, 0.13f, 0.18f) : today ? Color.Lerp(new Color(0.16f, 0.18f, 0.28f), main, ready ? 0.45f : 0.25f) : new Color(0.16f, 0.18f, 0.28f));
                Label(card, "DAY " + (d + 1), 28, big ? main : Gold, new Vector2(0, -14), new Vector2(w, 40), TextAnchor.MiddleCenter, TC);
                // iconitele una sub alta
                for (int k = 0; k < rw.Length; k++)
                {
                    var bg = UIKit.Rect(card, "IcBg" + k, TC, TC, new Vector2(0, -78 - k * 150), new Vector2(110, 110));
                    UIKit.Img(bg, "round_Grey", Art.Dk(Rewards.Col(rw[k]), done ? 0.25f : 0.5f), 30f).raycastTarget = false;
                    var ic = UIKit.Rect(bg, "Ic", C, C, Vector2.zero, new Vector2(78, 78)).gameObject.AddComponent<Image>();
                    ic.sprite = ItemIcons.Get(Rewards.Icon(rw[k])); ItemIcons.Art(ic); ic.raycastTarget = false;
                    ic.color = done ? new Color(1, 1, 1, 0.35f) : Color.white;
                    Label(card, Rewards.Text(rw[k]), 17, done ? new Color(1, 1, 1, 0.4f) : Color.Lerp(Rewards.Col(rw[k]), Color.white, 0.35f), new Vector2(0, -192 - k * 150), new Vector2(w - 8, 30), TextAnchor.MiddleCenter, TC);
                }
                if (done) Label(card, "CLAIMED", 20, new Color(0.55f, 1f, 0.6f, 0.8f), new Vector2(0, 30), new Vector2(w, 40), TextAnchor.MiddleCenter, BC);
                else if (today && ready) UIKit.Button(card, "Claim", "CLAIM", "Green", BC, BC, new Vector2(0, 18), new Vector2(w - 20, 64), () => { Rewards.ClaimWeek(); Rebuild(); }, 24);
                else if (today) Label(card, "TOMORROW", 18, new Color(0.75f, 0.8f, 0.95f), new Vector2(0, 30), new Vector2(w, 40), TextAnchor.MiddleCenter, BC);
            }
        }

        void Icons(RectTransform card, Rewards.R[] rw, bool dim, bool last)
        {
            int n = rw.Length;
            float s = n > 1 ? 84f : 110f;
            for (int k = 0; k < n; k++)
            {
                float x = n > 1 ? (k == 0 ? -52f : 52f) : 0f;
                var bg = UIKit.Rect(card, "IcBg" + k, TC, TC, new Vector2(x, -54), new Vector2(s, s));
                UIKit.Img(bg, "round_Grey", Art.Dk(last ? new Color(1f, 0.55f, 0.85f) : Rewards.Col(rw[k]), dim ? 0.25f : 0.5f), 30f).raycastTarget = false;
                var ic = UIKit.Rect(bg, "Ic", C, C, Vector2.zero, new Vector2(s * 0.7f, s * 0.7f)).gameObject.AddComponent<Image>();
                ic.sprite = ItemIcons.Get(Rewards.Icon(rw[k])); ItemIcons.Art(ic); ic.raycastTarget = false;
                ic.color = dim ? new Color(1, 1, 1, 0.35f) : Color.white;
            }
        }

        static string Desc(Rewards.R[] rw)
        {
            if (rw.Length == 1 && rw[0].kind == Rewards.GiftBox) return "GIFT BOX\n<size=14>Rare pet / Rare item / Cloaks / Tokens</size>";
            string s = "";
            for (int k = 0; k < rw.Length; k++) s += (k > 0 ? "\n+ " : "") + Rewards.Text(rw[k]);
            return s;
        }
    }
}
