// =====================================================================
//  IndexUI.cs - ITEM INDEX (la cartea de langa spawn in Meadow Fields):
//  toate cele 96 de iteme din joc, pe categorii (arme, coifuri, armuri,
//  inele): un rand pe harta, 6 iteme pe rand (2 Common, 2 Rare, Legendary,
//  Mythic). Itemele pe care le-ai gasit macar o data apar colorate, cu nume;
//  celelalte apar ca o umbra cu "???".
// =====================================================================
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class IndexUI : Window
    {
        int tab;
        static readonly string[] Tabs = { "WEAPONS", "HELMETS", "ARMOR", "RINGS", "PETS" };
        const int PetTab = 4;

        public static IndexUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("IndexUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<IndexUI>();
            w.Init(parent, g, "IndexDim", "ITEM INDEX", new Vector2(1500, 860), "Yellow");
            return w;
        }

        protected override void Build()
        {
            var D = game.player.D;
            game.player.DiscoverPets();
            int petsTotal = GameData.MapPets.Length * ItemDB.UsedRarities.Length;
            int seen = (D.seenCat != null ? D.seenCat.Count : 0) + (D.seenPets != null ? D.seenPets.Count : 0);
            for (int i = 0; i < Tabs.Length; i++)
            {
                int k = i;
                string cnt = i == PetTab ? (D.seenPets != null ? D.seenPets.Count : 0) + "/" + petsTotal : Found(i) + "/24";
                UIKit.Button(content, "Tab" + i, Tabs[i] + "  " + cnt, tab == i ? "Yellow" : "Grey", TL, TL, new Vector2(30 + i * 218, -20), new Vector2(210, 60), () => { tab = k; Sfx.Play("Click", 0.5f); Rebuild(); }, 18);
            }
            Label(content, "FOUND  <color=#7dff8a>" + seen + "</color> / " + (ItemCatalog.Count + petsTotal), 22, Color.white, new Vector2(1140, -30), new Vector2(340, 44));
            if (tab == PetTab) { BuildPets(); return; }

            const float cw = 196f, ch = 160f;
            for (int m = 0; m < 4 && m < GameData.Maps.Length; m++)
            {
                var md = GameData.Maps[m];
                float y = -100 - m * (ch + 14);
                MapLabel(md, y, ch);
                for (int pos = 0; pos < ItemCatalog.PerMap; pos++)
                {
                    int idx = ItemCatalog.IndexOf(tab, m, pos);
                    if (idx < 0) continue;
                    var d = ItemCatalog.All[idx];
                    bool got = D.seenCat != null && D.seenCat.Contains(idx + 1);
                    Color rc = ItemDB.Col(d.rarity);
                    var card = Card(content, new Vector2(190 + pos * (cw + 10), y), new Vector2(cw, ch),
                                    got ? Color.Lerp(new Color(0.16f, 0.18f, 0.26f), rc, 0.35f) : new Color(0.11f, 0.12f, 0.16f));
                    var ic = UIKit.Rect(card, "Ic", TC, TC, new Vector2(0, -6), new Vector2(96, 96)).gameObject.AddComponent<Image>();
                    ic.sprite = ItemIcons.For(Sample(idx)); ItemIcons.Art(ic); ic.preserveAspect = true; ic.raycastTarget = false;
                    // negasit: doar silueta intunecata
                    ic.color = got ? Color.white : new Color(0.06f, 0.06f, 0.09f, 0.95f);
                    var nm = Label(card, got ? d.name : "???", 16, got ? Color.white : new Color(1, 1, 1, 0.45f), new Vector2(0, -104), new Vector2(cw - 10, 26), TextAnchor.MiddleCenter, TC);
                    UIKit.Fit(nm, 10);
                    Label(card, ItemDB.RarityNames[d.rarity].ToUpper(), 14, got ? rc : Color.Lerp(rc, Color.gray, 0.6f), new Vector2(0, -128), new Vector2(cw - 10, 22), TextAnchor.MiddleCenter, TC);
                    if (got)
                    {
                        var it = Sample(idx);
                        ItemHover.Attach(card.gameObject, () => it);
                        card.GetComponent<Image>().raycastTarget = true;
                    }
                }
            }
            Label(content, "Every map drops its own 6 items per category. Find them all - found items stay in the INDEX even if you sell them.", 17, new Color(1, 1, 1, 0.6f),
                  new Vector2(30, -800), new Vector2(1440, 30)).alignment = TextAnchor.MiddleLeft;
        }

        void MapLabel(MapDef md, float y, float ch)
        {
            var l = Label(content, md.name.ToUpper(), 18, Color.Lerp(md.ground, Color.white, 0.45f), new Vector2(30, y - ch * 0.5f + 30), new Vector2(150, 60));
            l.alignment = TextAnchor.MiddleLeft;
            l.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        /// <summary>PETS: un rand pe harta, cate un pet pe raritate (Common, Rare, Legendary, Mythic).</summary>
        void BuildPets()
        {
            var D = game.player.D;
            const float cw = 296f, ch = 160f;
            for (int m = 0; m < GameData.MapPets.Length && m < GameData.Maps.Length; m++)
            {
                float y = -100 - m * (ch + 14);
                MapLabel(GameData.Maps[m], y, ch);
                for (int ri = 0; ri < ItemDB.UsedRarities.Length; ri++)
                {
                    int r = ItemDB.UsedRarities[ri];
                    var pd = new PetData { rarity = r, model = GameData.MapPets[m][r], map = m };
                    bool got = D.seenPets != null && D.seenPets.Contains(m + ":" + r);
                    Color rc = ItemDB.Col(r);
                    var card = Card(content, new Vector2(190 + ri * (cw + 10), y), new Vector2(cw, ch),
                                    got ? Color.Lerp(new Color(0.16f, 0.18f, 0.26f), rc, 0.35f) : new Color(0.11f, 0.12f, 0.16f));
                    var ic = UIKit.Rect(card, "Ic", TC, TC, new Vector2(0, -6), new Vector2(96, 96)).gameObject.AddComponent<Image>();
                    var sp = PetStudio.Icon(pd);
                    ic.sprite = sp != null ? sp : ItemIcons.Get(6); ItemIcons.Art(ic); ic.preserveAspect = true; ic.raycastTarget = false;
                    ic.color = got ? Color.white : new Color(0.06f, 0.06f, 0.09f, 0.95f);
                    Label(card, got ? GameData.PetName(pd.model) + "  <color=#7dff8a>+" + Mathf.RoundToInt(Player.PetPower(pd) * 100) + "%</color>" : "???", 16, got ? Color.white : new Color(1, 1, 1, 0.45f), new Vector2(0, -104), new Vector2(cw - 10, 26), TextAnchor.MiddleCenter, TC);
                    Label(card, ItemDB.RarityNames[r].ToUpper(), 14, got ? rc : Color.Lerp(rc, Color.gray, 0.6f), new Vector2(0, -128), new Vector2(cw - 10, 22), TextAnchor.MiddleCenter, TC);
                }
            }
            Label(content, "Hatch eggs on every map to find all pets. Found pets stay in the INDEX even if you trade or release them.", 17, new Color(1, 1, 1, 0.6f),
                  new Vector2(30, -800), new Vector2(1440, 30)).alignment = TextAnchor.MiddleLeft;
        }

        int Found(int slot)
        {
            var D = game.player.D;
            if (D.seenCat == null) return 0;
            int n = 0;
            foreach (var c in D.seenCat) if (c > 0 && c <= ItemCatalog.Count && ItemCatalog.All[c - 1].slot == slot) n++;
            return n;
        }

        readonly System.Collections.Generic.Dictionary<int, Item> samples = new System.Collections.Generic.Dictionary<int, Item>();
        Item Sample(int idx)
        {
            Item it;
            if (!samples.TryGetValue(idx, out it)) { it = ItemDB.FromCatalog(idx); it.id = -1000 - idx; it.roll = 1f; samples[idx] = it; }
            return it;
        }
    }
}
