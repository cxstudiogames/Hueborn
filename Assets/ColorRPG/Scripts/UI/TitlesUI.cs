// =====================================================================
//  TitlesUI.cs - fereastra TITLES (din INVENTORY, click pe titlu):
//  categoriile in stanga, titlurile in dreapta - deblocate (EQUIP) sau
//  blocate (cu lacat si cum le deblochezi). AUTO = titlul dupa putere.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class TitlesUI : Window
    {
        int cat;

        public static TitlesUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("TitlesUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<TitlesUI>();
            w.Init(parent, g, "TitlesDim", "TITLES", new Vector2(1500, 860), "Yellow");
            return w;
        }

        protected override void Build()
        {
            var p = game.player; var D = p.D;
            TitleDB.CheckUnlocks(p);
            var all = TitleDB.All;
            int have = 0; foreach (var d in all) if (D.titles.Contains(d.id)) have++;

            // ---- categorii (stanga)
            var left = Card(content, new Vector2(24, -16), new Vector2(300, 768), new Color(0.3f, 0.3f, 0.32f));
            Label(left, "UNLOCKED  " + have + " / " + all.Count, 22, new Color(1f, 0.85f, 0.4f), new Vector2(0, -12), new Vector2(300, 34), TextAnchor.MiddleCenter, TC);
            for (int i = 0; i < TitleDB.Categories.Length; i++)
            {
                int k = i;
                string c = TitleDB.Categories[i];
                int n = 0, h = 0;
                foreach (var d in all) if (d.cat == c) { n++; if (D.titles.Contains(d.id)) h++; }
                UIKit.Fit(UIKit.Button(left, "C" + i, c + "  " + h + "/" + n, cat == i ? "Yellow" : "Grey", TC, TC, new Vector2(0, -54 - i * 54), new Vector2(264, 48),
                                       () => { cat = k; Rebuild(); }, 21).GetComponentInChildren<Text>(), 12);
            }

            // ---- titlul curent + AUTO
            string curName; Color curCol; int curTier;
            TitleDB.Current(p, out curName, out curCol, out curTier);
            var top = Card(content, new Vector2(344, -16), new Vector2(1132, 84), new Color(0.2f, 0.2f, 0.22f));
            Label(top, "CURRENT TITLE:", 24, new Color(0.8f, 0.82f, 0.9f), new Vector2(20, -22), new Vector2(240, 40));
            var ct = Label(top, curName, 34, curCol, new Vector2(250, -18), new Vector2(560, 48));
            TitleStyle.Apply(ct, curName, curCol, curTier, 34);
            UIKit.Fit(ct, 16);
            bool auto = string.IsNullOrEmpty(D.title);
            UIKit.Button(top, "Auto", auto ? "AUTO (POWER) - ON" : "USE AUTO (POWER)", auto ? "Green" : "Blue", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(290, 60),
                         () => { D.title = ""; p.SaveNow(); Rebuild(); }, 20);

            // ---- titlurile categoriei
            string cname = TitleDB.Categories[Mathf.Clamp(cat, 0, TitleDB.Categories.Length - 1)];
            var list = new List<TitleDef>();
            foreach (var d in all) if (d.cat == cname) list.Add(d);
            int cols = 3;
            float cw = 366, ch = 132;
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];
                bool un = D.titles.Contains(d.id);
                bool eq = !auto && D.title == d.id;
                int r = i / cols, c = i % cols;
                var card = Card(content, new Vector2(344 + c * (cw + 17), -118 - r * (ch + 12)), new Vector2(cw, ch), eq ? new Color(0.3f, 0.6f, 0.35f) : un ? new Color(0.24f, 0.24f, 0.27f) : new Color(0.12f, 0.12f, 0.14f));
                var nm = Label(card, d.name, 28, un ? d.col : new Color(0.55f, 0.55f, 0.6f), new Vector2(16, -10), new Vector2(un ? cw - 32 : cw - 70, 40));
                TitleStyle.Apply(nm, d.name, un ? d.col : new Color(0.55f, 0.55f, 0.6f), d.tier, 28);
                if (!un) { var fx = nm.GetComponent<TitleFx>(); if (fx != null) fx.enabled = false; }
                UIKit.Fit(nm, 14);
                Color tcol = d.tier >= 4 ? ItemDB.Col(5) : d.tier == 3 ? ItemDB.Col(4) : d.tier == 2 ? ItemDB.Col(3) : d.tier == 1 ? ItemDB.Col(2) : new Color(0.7f, 0.72f, 0.78f);
                if (un) Label(card, TitleStyle.TierNames[d.tier], 15, tcol, new Vector2(16, -84), new Vector2(160, 24));
                else Label(card, TitleStyle.TierNames[d.tier], 15, tcol, new Vector2(-16, -84), new Vector2(160, 24), TextAnchor.MiddleRight, TR).rectTransform.pivot = TR;
                var ds = Label(card, d.desc, 17, new Color(1f, 1f, 1f, un ? 0.8f : 0.55f), new Vector2(16, -50), new Vector2(cw - 32, 26));
                UIKit.Fit(ds, 11);
                if (!un)
                {
                    var lk = UIKit.Rect(card, "Lock", TR, TR, new Vector2(-14, -12), new Vector2(38, 38)).gameObject.AddComponent<Image>();
                    lk.sprite = ItemIcons.Get(32); ItemIcons.Art(lk);
                    lk.color = new Color(1f, 1f, 1f, 0.6f); lk.raycastTarget = false;
                    Label(card, "LOCKED", 20, new Color(1f, 0.5f, 0.45f), new Vector2(16, 14), new Vector2(200, 32), TextAnchor.MiddleLeft, BL).rectTransform.pivot = BL;
                }
                else
                {
                    string id = d.id;
                    UIKit.Button(card, "Eq", eq ? "EQUIPPED" : "EQUIP", eq ? "Grey" : "Green", BR, BR, new Vector2(-12, 12), new Vector2(170, 52),
                                 () => { if (D.title != id) { D.title = id; Sfx.Play("Equip", 0.6f); p.SaveNow(); Rebuild(); } }, 22).interactable = !eq;
                }
            }
        }
    }
}
