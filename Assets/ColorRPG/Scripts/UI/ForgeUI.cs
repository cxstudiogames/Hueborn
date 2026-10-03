// =====================================================================
//  ForgeUI.cs - fereastra FORJEI: alegi itemul din grila (dreapta), vezi
//  "+N -> +N+1", sansa, costul, optiunea PROTECT (Guardian Cloak) si apesi
//  FORGE sau MAX FORGE (forjeaza pana la limita forjei sau pana nu mai ai materiale).
//  Limite: Meadow +0..+5, Desert +5..+10, Lava +10..+15 (maxim).
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class ForgeUI : Window
    {
        const int Cols = 7, Rows = 6;
        Vector3 anvil;
        Item selected;
        readonly List<Item> view = new List<Item>();
        // MAX FORGE: forjeaza automat pas cu pas (animat)
        bool maxRunning;
        float maxT;
        int maxOk, maxFail;
        // efectul rezultatului peste item
        RectTransform previewRt;
        Text resultText;
        float resultT;

        public static ForgeUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("ForgeUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<ForgeUI>();
            w.Init(parent, g, "ForgeDim", "FORGE", new Vector2(1560, 900), "Orange");
            return w;
        }

        public void OpenAt(Vector3 anvilPos)
        {
            anvil = anvilPos;
            maxRunning = false;
            var p = game.player;
            // implicit: arma echipata (daca se poate forja aici), altfel primul item forjabil
            selected = null;
            var w = p.Equipped(Slot.Weapon);
            if (w != null && p.ForgeBlock(w, game.mapIndex) == null) selected = w;
            Open();
            if (selected == null && view.Count > 0 && p.ForgeBlock(view[0], game.mapIndex) == null) { selected = view[0]; Rebuild(); }
        }

        public override void Close()
        {
            maxRunning = false;
            base.Close();
        }

        protected override void Build()
        {
            var p = game.player; var D = p.D;
            int map = game.mapIndex;
            int mn, mx; Player.ForgeRange(map, out mn, out mx);
            title.text = GameData.Maps[Mathf.Clamp(map, 0, GameData.Maps.Length - 1)].name.ToUpper() + " FORGE  (+" + mn + " to +" + mx + ")";
            if (selected != null && !D.items.Contains(selected)) selected = null;

            // ================= stanga: itemul ales =================
            var left = Card(content, new Vector2(26, -20), new Vector2(720, 800), new Color(0.2f, 0.2f, 0.22f));
            if (selected == null)
            {
                Label(left, "SELECT AN ITEM", 40, Color.white,
                      new Vector2(0, 0), new Vector2(660, 300), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f)).rectTransform.pivot = new Vector2(0.5f, 0.5f);
                BuildGrid(p, map);
                return;
            }
            var it = selected;
            Color rc = ItemDB.Col(it.rarity);
            bool maxed = it.plus >= ItemDB.MaxPlus;

            // previzualizare: iconita mare  ->  +N
            previewRt = UIKit.Rect(left, "Preview", TL, new Vector2(0.5f, 0.5f), new Vector2(150, -150), new Vector2(220, 220));
            UIKit.Img(previewRt, "sq_Grey", Color.Lerp(Color.white, rc, 0.6f), 18f, 1f);
            var ic = UIKit.Rect(previewRt, "Icon", C, C, new Vector2(0, 6), new Vector2(160, 160)).gameObject.AddComponent<Image>();
            ic.sprite = ItemIcons.For(it); ItemIcons.Art(ic); ic.color = rc; ic.raycastTarget = false;
            UIKit.Text(previewRt, "Plus", "+" + it.plus, 40, Color.white, TextAnchor.LowerRight, BR, BR, new Vector2(-10, 4), new Vector2(150, 50));
            resultText = UIKit.Text(previewRt, "Result", "", 46, Color.white, TextAnchor.MiddleCenter, C, C, Vector2.zero, new Vector2(400, 80));
            resultText.enabled = false;

            Label(left, maxed ? "MAX" : "+" + (it.plus + 1), 96, maxed ? new Color(1f, 0.8f, 0.3f) : new Color(0.5f, 1f, 0.55f), new Vector2(330, -76), new Vector2(360, 120));
            var arrow = Label(left, maxed ? "" : "→", 80, Color.white, new Vector2(266, -78), new Vector2(80, 120));
            arrow.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var nm = Label(left, ItemDB.FullName(it), 28, rc, new Vector2(270, -196), new Vector2(430, 40));
            nm.resizeTextForBestFit = true; nm.resizeTextMinSize = 16; nm.resizeTextMaxSize = 30;
            Label(left, ItemDB.RarityNames[Mathf.Clamp(it.rarity, 0, 5)].ToUpper() + "  " + ItemDB.SlotNames[it.slot].ToUpper() + (p.IsEquipped(it) ? "  <color=#7dff8a>(EQUIPPED)</color>" : ""),
                  20, new Color(0.75f, 0.78f, 0.85f), new Vector2(270, -232), new Vector2(430, 30));

            // statistica: acum -> dupa
            string stat = StatLine(it, it.plus) + (maxed ? "" : "   <color=#8a90a0>→</color>   <color=#7dff8a>" + StatLine(it, it.plus + 1) + "</color>");
            Label(left, stat, 24, Color.white, new Vector2(30, -290), new Vector2(660, 34));

            string block = p.ForgeBlock(it, map);
            int chance = ItemDB.ForgeChance(it);
            // sansa
            Label(left, "SUCCESS CHANCE", 24, new Color(0.8f, 0.82f, 0.9f), new Vector2(30, -326), new Vector2(400, 32));
            var bar = UIKit.MakeBar(left, "Chance", TL, TL, new Vector2(30, -360), new Vector2(660, 48), Color.Lerp(new Color(1f, 0.35f, 0.3f), new Color(0.35f, 1f, 0.4f), chance / 100f), 26, false);
            bar.Set(maxed ? 1f : chance / 100f, maxed ? "MAX LEVEL" : chance + "%");

            // cost
            var cost = Card(left, new Vector2(30, -420), new Vector2(660, 112), new Color(0.12f, 0.12f, 0.14f));
            Label(cost, "COST", 22, new Color(0.8f, 0.82f, 0.9f), new Vector2(16, -8), new Vector2(200, 28));
            if (!maxed)
            {
                int c = ItemDB.ForgeCoins(it), o = ItemDB.ForgeOre(it), st = ItemDB.ForgeStones(it), sh = ItemDB.ForgeShards(it);
                float x = 16f;
                x = CostChip(cost, x, 4, new Color(1f, 0.85f, 0.25f), c, D.coins);
                x = CostChip(cost, x, 5, new Color(0.65f, 0.75f, 0.95f), o, D.ore);
                if (st > 0) x = CostChip(cost, x, 16, TradeDB.MatCols[TradeDB.MatStones], st, D.forgeStones);
                if (sh > 0) x = CostChip(cost, x, 9, new Color(0.55f, 0.85f, 1f), sh, D.shards);
            }

            // PROTECT (Guardian Cloak): o forja esuata nu scade nivelul
            int need = it.plus >= ItemDB.ForgeRiskFrom ? ItemDB.ForgeCloaks(it) : 0;   // sub +5 nu e risc -> nu consuma
            var prot = Card(left, new Vector2(30, -544), new Vector2(660, 112), D.forgeProtect ? new Color(0.45f, 0.25f, 0.6f) : new Color(0.14f, 0.14f, 0.16f));
            var pb = UIKit.Rect(prot, "Box", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(70, 70));
            var pbi = UIKit.Img(pb, D.forgeProtect ? "sq_Purple" : "sq_Grey", Color.white, 16f, 1f);
            pbi.raycastTarget = true;
            var pbtn = pb.gameObject.AddComponent<Button>();
            pbtn.targetGraphic = pbi;
            pbtn.onClick.AddListener(() => { D.forgeProtect = !D.forgeProtect; Sfx.Play("Click", 0.5f); Rebuild(); });
            var cki = UIKit.Rect(pb, "Ic", C, C, new Vector2(0, 3), new Vector2(48, 48)).gameObject.AddComponent<Image>();
            cki.sprite = ItemIcons.Get(17); ItemIcons.Art(cki); cki.color = D.forgeProtect ? Color.white : new Color(1f, 1f, 1f, 0.3f); cki.raycastTarget = false;
            Label(prot, "PROTECT " + (D.forgeProtect ? "<color=#7dff8a>ON</color>" : "<color=#ff8a8a>OFF</color>"), 30, Color.white, new Vector2(102, -10), new Vector2(540, 40));
            Label(prot, "Cloaks: " + D.cloaks + "  (uses " + need + " per try)", 22, new Color(0.85f, 0.85f, 0.95f), new Vector2(102, -54), new Vector2(548, 34));

            // butoane
            bool can = block == null && !maxRunning;
            UIKit.Button(left, "Forge", block != null ? block.ToUpper() : "FORGE", can ? "Green" : "Grey", BL, BL, new Vector2(30, 24), new Vector2(390, 96), () => DoForge(), 34).interactable = can;
            UIKit.Button(left, "Max", maxRunning ? "STOP" : "MAX FORGE", maxRunning ? "Red" : (can ? "Purple" : "Grey"), BL, BL, new Vector2(436, 24), new Vector2(254, 96), ToggleMax, 30).interactable = can || maxRunning;

            BuildGrid(p, map);
        }

        static string StatLine(Item it, int plus)
        {
            int old = it.plus;
            it.plus = plus;
            string s;
            if (it.S == Slot.Weapon) s = "DMG " + Fmt.Num(ItemDB.Power(it));
            else if (it.S == Slot.Ring) s = "CRIT +" + Mathf.RoundToInt(ItemDB.CritBonus(it) * 100) + "%";
            else if (it.S == Slot.Armor) s = "HP +" + Fmt.Num(ItemDB.Power(it)) + "  DEF " + Mathf.RoundToInt(ItemDB.Defense(it) * 100) + "%";
            else s = "HP +" + Fmt.Num(ItemDB.Power(it));
            it.plus = old;
            return s;
        }

        float CostChip(RectTransform parent, float x, int icon, Color c, long need, long have)
        {
            var ir = UIKit.Rect(parent, "I", TL, TL, new Vector2(x, -42), new Vector2(54, 54)).gameObject.AddComponent<Image>();
            ir.sprite = ItemIcons.Get(icon); ItemIcons.Art(ir); ir.color = c; ir.raycastTarget = false;
            string txt = Fmt.Num(need);
            var t = Label(parent, txt, 30, have >= need ? Color.white : new Color(1f, 0.4f, 0.4f), new Vector2(x + 58, -46), new Vector2(110, 46));
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return x + 66 + Mathf.Max(56, txt.Length * 19) + 20;
        }

        void BuildGrid(Player p, int map)
        {
            var right = Card(content, new Vector2(766, -20), new Vector2(768, 800), new Color(0.2f, 0.2f, 0.22f));
            Label(right, "YOUR ITEMS", 30, Color.white, new Vector2(24, -14), new Vector2(400, 40));

            view.Clear();
            foreach (var it in p.D.items) view.Add(it);
            // ordinea: echipate, apoi cele forjabile aici, apoi dupa raritate / putere
            view.Sort((a, b) =>
            {
                int ea = p.IsEquipped(a) ? 0 : 1, eb = p.IsEquipped(b) ? 0 : 1;
                if (ea != eb) return ea.CompareTo(eb);
                int fa = p.ForgeBlock(a, map) == null ? 0 : 1, fb = p.ForgeBlock(b, map) == null ? 0 : 1;
                if (fa != fb) return fa.CompareTo(fb);
                if (a.rarity != b.rarity) return b.rarity.CompareTo(a.rarity);
                return Player.Score(b).CompareTo(Player.Score(a));
            });

            float cell = 100f, gap = 6f;
            for (int i = 0; i < Cols * Rows; i++)
            {
                int r = i / Cols, c = i % Cols;
                var rt = UIKit.Rect(right, "C" + i, TL, TL, new Vector2(20 + c * (cell + gap), -66 - r * (cell + gap)), new Vector2(cell, cell));
                if (i >= view.Count)
                {
                    UIKit.Img(rt, "sq_Grey", new Color(0.4f, 0.4f, 0.45f, 0.5f), 16f, 1.2f);
                    continue;
                }
                var it = view[i];
                bool ok = p.ForgeBlock(it, map) == null;
                Color rc = ItemDB.Col(it.rarity);
                var fr = UIKit.Img(rt, "sq_Grey", it == selected ? Color.white : Color.Lerp(new Color(0.35f, 0.35f, 0.4f), rc, ok ? 0.6f : 0.15f), 16f, 1.2f);
                fr.raycastTarget = true;
                var b = rt.gameObject.AddComponent<Button>();
                b.targetGraphic = fr;
                var item = it;
                b.onClick.AddListener(() => { if (maxRunning) return; selected = item; Sfx.Play("Click", 0.4f); Rebuild(); });
                ItemHover.Attach(rt.gameObject, () => item);
                var ic = UIKit.Rect(rt, "Icon", C, C, new Vector2(0, 4), new Vector2(68, 68)).gameObject.AddComponent<Image>();
                ic.sprite = ItemIcons.For(it); ItemIcons.Art(ic); ic.color = ok ? rc : new Color(rc.r, rc.g, rc.b, 0.4f); ic.raycastTarget = false;
                if (it.plus > 0) UIKit.Text(rt, "P", "+" + it.plus, 22, it.plus >= ItemDB.MaxPlus ? new Color(1f, 0.8f, 0.3f) : Color.white, TextAnchor.LowerRight, BR, BR, new Vector2(-6, 2), new Vector2(80, 30));
                if (p.IsEquipped(it)) UIKit.Text(rt, "E", "E", 22, new Color(0.5f, 1f, 0.55f), TextAnchor.UpperLeft, TL, TL, new Vector2(8, -2), new Vector2(30, 30));
            }
            if (view.Count > Cols * Rows)
                Label(right, "+" + (view.Count - Cols * Rows) + " more items (sell some in the INVENTORY)", 18, new Color(0.8f, 0.82f, 0.9f), new Vector2(24, -720), new Vector2(700, 30));
        }

        void DoForge()
        {
            if (selected == null) return;
            var p = game.player;
            int before = selected.plus;
            int r = p.ForgeItem(selected, game.mapIndex, p.D.forgeProtect, anvil, maxRunning);
            if (r < 0) { maxRunning = false; Rebuild(); return; }
            Rebuild();
            ShowResult(r == 1, before);
            if (r == 1) maxOk++; else maxFail++;
            if (!maxRunning)
            {
                if (r == 1) game.hud.Pop(p.transform.position + Vector3.up * 3f, "+" + selected.plus + "!", ItemDB.Col(selected.rarity), 1.6f);
                else if (selected.plus < before) game.hud.Pop(p.transform.position + Vector3.up * 3f, "-1", new Color(1f, 0.4f, 0.4f), 1.4f);
            }
        }

        void ShowResult(bool ok, int before)
        {
            if (resultText == null) return;
            resultText.enabled = true;
            resultText.color = ok ? new Color(0.45f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f);
            resultText.text = ok ? "SUCCESS!" : (selected.plus < before ? "FAILED  -1" : "FAILED");
            resultT = 0.9f;
            if (ok) Fx.Shake(0.15f);
        }

        void ToggleMax()
        {
            if (maxRunning)
            {
                maxRunning = false;
                Rebuild();
                return;
            }
            if (selected == null || game.player.ForgeBlock(selected, game.mapIndex) != null) return;
            maxRunning = true; maxT = 0f; maxOk = maxFail = 0;
            Rebuild();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsOpen) return;
            if (resultT > 0f && resultText != null)
            {
                resultT -= Time.unscaledDeltaTime;
                float k = 1f - resultT / 0.9f;
                resultText.rectTransform.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(1.8f, 1f, k / 0.15f) : 1f);
                var c = resultText.color; c.a = Mathf.Clamp01(resultT / 0.3f); resultText.color = c;
                if (previewRt != null) previewRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * 40f) * 6f * (1f - k));
                if (resultT <= 0f) resultText.enabled = false;
            }
            if (maxRunning)
            {
                maxT -= Time.unscaledDeltaTime;
                if (maxT <= 0f)
                {
                    maxT = 0.35f;
                    if (selected == null || game.player.ForgeBlock(selected, game.mapIndex) != null)
                    {
                        maxRunning = false;
                        game.Say("MAX FORGE done: " + maxOk + " success, " + maxFail + " failed." + (selected != null ? "  " + ItemDB.FullName(selected) : ""), 3.5f);
                        Rebuild();
                    }
                    else
                    {
                        DoForge();
                        if (!maxRunning) game.Say("MAX FORGE stopped - not enough materials. (" + maxOk + " success, " + maxFail + " failed)", 3.5f);
                    }
                }
            }
        }
    }
}
