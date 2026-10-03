// =====================================================================
//  Leaderboard.cs - panou de clasament in HUB (harta 1): TOP 10 POWER / TOP 10 TIME PLAYED.
//  Componenta salvata in scena (numele fisierului = numele clasei). Textul se construieste
//  la Play intr-un Canvas in lume. Pana la serverul online, ceilalti jucatori sunt simulati;
//  jucatorul apare in top cand ajunge destul de puternic si vede mereu locul lui jos.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class Leaderboard : MonoBehaviour
    {
        [Tooltip("0 = TOP POWER, 1 = TOP TIME PLAYED")]
        public int kind;
        [Tooltip("Latimea / inaltimea panoului (metri)")]
        public float width = 7f, height = 8.6f;

        const int Rows = 10;
        const float PxPerM = 100f;
        Text[] rank = new Text[Rows], names = new Text[Rows], vals = new Text[Rows];
        Image[] rowBg = new Image[Rows];
        Text youText;
        float refreshT;

        public struct Entry { public string name; public double value; public bool you; }

        void Start()
        {
            BuildCanvas();
            Refresh();
        }

        void BuildCanvas()
        {
            var go = new GameObject("BoardCanvas", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            // fata panoului priveste spre +Z al obiectului; canvas-ul se vede din spatele lui -> rotit 180
            rt.localPosition = new Vector3(0, height * 0.5f + 0.9f, 0.16f);
            rt.localRotation = Quaternion.Euler(0, 180f, 0);
            rt.localScale = Vector3.one / PxPerM;
            rt.sizeDelta = new Vector2(width * PxPerM, height * PxPerM);
            var cv = go.AddComponent<Canvas>();
            cv.renderMode = RenderMode.WorldSpace;
            go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;

            float W = width * PxPerM, H = height * PxPerM;
            Vector2 TC = new Vector2(0.5f, 1), C = new Vector2(0.5f, 0.5f), TL = new Vector2(0, 1);
            var bg = UIKit.Stretch(rt, "Bg");
            UIKit.Img(bg, "panel_dark", Color.white, 26f, 1f);
            Color head = kind == 0 ? new Color(1f, 0.55f, 0.2f) : new Color(0.35f, 0.75f, 1f);
            var hr = UIKit.Rect(rt, "Header", TC, TC, new Vector2(0, -14), new Vector2(W - 60, 110));
            UIKit.Img(hr, "tab_title", Color.white, 22f, 1f);
            var ic = UIKit.Rect(hr, "Icon", new Vector2(0, 0.5f), C, new Vector2(62, 0), new Vector2(76, 76)).gameObject.AddComponent<Image>();
            ic.sprite = ItemIcons.Get(kind == 0 ? 22 : 35); ItemIcons.Art(ic); ic.color = head; ic.raycastTarget = false;
            UIKit.Text(hr, "T", kind == 0 ? "TOP POWER" : "TOP TIME PLAYED", 50, Color.white, TextAnchor.MiddleCenter, C, C, new Vector2(30, 2), new Vector2(W - 200, 100));

            float top = -140f, rowH = (H - 140f - 120f) / Rows;
            for (int i = 0; i < Rows; i++)
            {
                var row = UIKit.Rect(rt, "Row" + i, TL, TL, new Vector2(24, top - i * rowH), new Vector2(W - 48, rowH - 8));
                rowBg[i] = UIKit.Img(row, "flat_Grey", new Color(1f, 1f, 1f, 0.06f), 12f, 2f);
                rank[i] = UIKit.Text(row, "R", "#" + (i + 1), 38, Color.white, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(90, rowH));
                names[i] = UIKit.Text(row, "N", "-", 34, Color.white, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(104, 0), new Vector2(W - 380, rowH));
                vals[i] = UIKit.Text(row, "V", "", 34, head, TextAnchor.MiddleRight, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(260, rowH));
            }
            var yb = UIKit.Rect(rt, "You", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(W - 48, 84));
            UIKit.Img(yb, "flat_Grey", new Color(1f, 0.85f, 0.3f, 0.22f), 12f, 2f);
            youText = UIKit.Text(yb, "T", "", 34, Color.white, TextAnchor.MiddleCenter, C, C, Vector2.zero, new Vector2(W - 60, 80));
            youText.supportRichText = true;
            foreach (var t in names) t.supportRichText = true;
        }

        static string Hours(double h)
        {
            if (h < 1.0) return Mathf.Max(1, Mathf.RoundToInt((float)(h * 60.0))) + "m";
            return h < 100.0 ? h.ToString("0.0") + "h" : Mathf.RoundToInt((float)h) + "h";
        }

        /// <summary>Jucatori simulati (pana la serverul online): valori stabile care cresc incet in timp.</summary>
        static List<Entry> Simulated(int kind)
        {
            var l = new List<Entry>();
            var names = TradeDB.ShopperNames;
            // creste putin in fiecare zi reala, ca clasamentul sa para viu
            double days = (TradeDB.Now() / 86400L) % 3650;
            double grow = 1.0 + days * 0.002;
            for (int i = 0; i < 14; i++)
            {
                string n = names[(i * 7 + 3 + kind * 5) % names.Length];
                double v = kind == 0 ? 3200000.0 * System.Math.Pow(0.78, i) * grow : 520.0 * System.Math.Pow(0.87, i) * grow;
                // mic "zgomot" stabil pe nume
                v *= 1.0 + ((n.GetHashCode() & 0xff) / 255.0 - 0.5) * 0.06;
                l.Add(new Entry { name = n, value = v });
            }
            return l;
        }

        /// <summary>Locul jucatorului in clasament (0 = TOP POWER, 1 = TIMP JUCAT) - folosit si pentru titluri.</summary>
        public static int PlayerRank(int kind, Player p)
        {
            if (NetSync.Online) return NetSync.MyRank[kind] > 0 ? NetSync.MyRank[kind] : 9999;   // clasamentul REAL de pe server
            double mine = kind == 0 ? p.Power : p.D.playTime / 3600.0;
            int better = 0;
            foreach (var e in Simulated(kind)) if (e.value > mine) better++;
            return better + 1;
        }

        void Refresh()
        {
            var g = Game.I;
            if (g == null || g.player == null || g.player.D == null || rank[0] == null) return;
            var p = g.player;
            double mine = kind == 0 ? p.Power : p.D.playTime / 3600.0;
            List<Entry> list;
            int myRank;
            var online = NetSync.Online ? NetSync.TopLists[kind] : null;
            if (online != null)
            {
                // clasamentul REAL (server): top 10 + locul tau
                list = new List<Entry>();
                foreach (var e in online) list.Add(new Entry { name = e.name, value = e.score, you = e.playerId == ColorRPG.Net.Ugs.PlayerId });
                myRank = NetSync.MyRank[kind];
                ShowRows(list, kind);
                youText.text = "YOU: <color=#ffd84d>" + (myRank > 0 ? "#" + Fmt.Num(myRank) : "-") + "</color>   " + (kind == 0 ? "POWER " + Fmt.Num((long)mine) : "PLAYED " + Hours(mine)) + "   <color=#7dff8a>ONLINE</color>";
                return;
            }
            list = Simulated(kind);
            list.Add(new Entry { name = p.D.playerName, value = mine, you = true });
            list.Sort((a, b) => b.value.CompareTo(a.value));
            myRank = list.FindIndex(e => e.you) + 1;
            if (myRank > Rows)
            {
                // in afara topului: estimam locul printre "toti" jucatorii
                double tenth = list[Rows - 1].value;
                myRank = Rows + 1 + Mathf.RoundToInt((float)(System.Math.Log(System.Math.Max(1.0, tenth / System.Math.Max(0.01, mine))) * (kind == 0 ? 420.0 : 260.0)));
            }
            ShowRows(list, kind);
            youText.text = "YOU: <color=#ffd84d>#" + Fmt.Num(myRank) + "</color>   " + (kind == 0 ? "POWER " + Fmt.Num((long)mine) : "PLAYED " + Hours(mine));
        }

        void ShowRows(List<Entry> list, int kind)
        {
            Color[] medal = { new Color(1f, 0.82f, 0.25f), new Color(0.82f, 0.86f, 0.92f), new Color(0.9f, 0.58f, 0.32f) };
            for (int i = 0; i < Rows; i++)
            {
                if (i >= list.Count)
                {
                    rank[i].text = "#" + (i + 1); rank[i].color = new Color(0.75f, 0.78f, 0.85f, 0.5f);
                    names[i].text = "<color=#8890aa>-</color>"; vals[i].text = "";
                    rowBg[i].color = new Color(1f, 1f, 1f, 0.03f);
                    continue;
                }
                var e = list[i];
                rank[i].text = "#" + (i + 1);
                rank[i].color = i < 3 ? medal[i] : new Color(0.75f, 0.78f, 0.85f);
                names[i].text = e.you ? "<color=#ffd84d>" + e.name + " (YOU)</color>" : e.name;
                vals[i].text = kind == 0 ? Fmt.Num((long)e.value) : Hours(e.value);
                rowBg[i].color = e.you ? new Color(1f, 0.85f, 0.3f, 0.25f) : i < 3 ? new Color(medal[i].r, medal[i].g, medal[i].b, 0.12f) : new Color(1f, 1f, 1f, i % 2 == 0 ? 0.07f : 0.03f);
            }
        }

        void Update()
        {
            refreshT -= Time.deltaTime;
            if (refreshT <= 0f) { refreshT = 4f; Refresh(); }
        }
    }
}
