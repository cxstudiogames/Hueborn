// =====================================================================
//  QuestUI.cs - fereastra NPC-ului cu questuri: alegi ce quest iei,
//  vezi progresul si recompensele, pornesti / opresti tracking-ul.
//  Questurile se termina AUTOMAT cand ajungi la numarul cerut.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class QuestUI : Window
    {
        int map;

        public static QuestUI Create(RectTransform parent, Game g)
        {
            var go = new GameObject("QuestUI", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<QuestUI>();
            w.Init(parent, g, "QuestDim", "QUESTS", new Vector2(1520, 760), "Yellow");
            return w;
        }

        public void OpenFor(int m)
        {
            map = Mathf.Clamp(m, 0, 3);
            Open();
        }

        protected override void Build()
        {
            var p = game.player; var D = p.D;
            title.text = QuestDB.NpcNames[map].ToUpper() + "  -  " + GameData.Maps[map].name.ToUpper();
            Label(content, "Pick ONE quest. It starts again every time you finish it, until you switch.", 20, new Color(1f, 1f, 1f, 0.75f), new Vector2(30, -14), new Vector2(1460, 30));

            var defs = QuestDB.ForMap(map);
            float cw = (1520f - 48f - (defs.Length - 1) * 14f) / defs.Length;   // latimea unei carti (4 questuri pe harta)
            for (int i = 0; i < defs.Length; i++)
            {
                var def = defs[i];
                var q = p.FindQuest(map, i);
                bool active = q != null;
                Color cc = active ? new Color(0.16f, 0.32f, 0.22f, 1f) : new Color(0.2f, 0.22f, 0.34f, 1f);
                var card = Card(content, new Vector2(24 + i * (cw + 14f), -56), new Vector2(cw, 560), cc);
                var band = UIKit.Rect(card, "Band", new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(cw, 44));
                UIKit.Img(band, "flat_Grey", def.boss ? new Color(1f, 0.45f, 0.4f) : new Color(1f, 0.78f, 0.3f), 16f, 1f);
                UIKit.Text(band, "T", active ? "YOUR QUEST" : def.boss ? "BOSS" : "HUNT", 20, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(cw - 20, 36));

                // titlu + tinta (simplu, alb)
                var tt = Label(card, def.title, 26, Color.white, new Vector2(0, -56), new Vector2(cw - 24, 36), TextAnchor.MiddleCenter, TC);
                UIKit.Fit(tt, 16);
                Label(card, "Kill " + def.count + " " + def.target, 20, new Color(1f, 1f, 1f, 0.7f), new Vector2(0, -94), new Vector2(cw - 24, 30), TextAnchor.MiddleCenter, TC);

                // recompense: iconita + numar (fara texte lungi / culori)
                Label(card, "REWARDS", 16, new Color(1f, 1f, 1f, 0.5f), new Vector2(0, -136), new Vector2(cw, 24), TextAnchor.MiddleCenter, TC);
                var rw = new System.Collections.Generic.List<KeyValuePair<int, string>>();
                rw.Add(new KeyValuePair<int, string>(4, Fmt.Num(def.coins)));
                rw.Add(new KeyValuePair<int, string>(22, Fmt.Num(def.xp) + " XP"));
                for (int k = 0; k < 3; k++) if (def.potions[k] > 0) rw.Add(new KeyValuePair<int, string>(36 + k, "x" + def.potions[k]));
                if (def.stones > 0) rw.Add(new KeyValuePair<int, string>(16, "x" + def.stones));
                if (def.cloaks > 0) rw.Add(new KeyValuePair<int, string>(17, "x" + def.cloaks));
                if (def.shards > 0) rw.Add(new KeyValuePair<int, string>(9, "x" + def.shards));
                float colW = (cw - 40f) / 2f;
                for (int k = 0; k < rw.Count; k++)
                {
                    int c = k % 2, r = k / 2;
                    var chip = UIKit.Rect(card, "R" + k, TL, TL, new Vector2(20 + c * colW, -166 - r * 46), new Vector2(colW - 6, 40));
                    var ic = UIKit.Rect(chip, "Ic", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(36, 36)).gameObject.AddComponent<Image>();
                    ic.sprite = ItemIcons.Get(rw[k].Key); ItemIcons.Art(ic); ic.raycastTarget = false;
                    UIKit.Text(chip, "T", rw[k].Value, 20, Color.white, TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(46, 2), new Vector2(colW - 56, 36), true, true);
                }

                int idx = i;
                if (!active)
                {
                    bool has = D.quests.Count >= QuestDB.MaxActive;
                    UIKit.Button(card, "Acc", has ? "SWITCH TO THIS" : "START", has ? "Grey" : "Green", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(cw - 44, 64),
                                 () => { if (p.AcceptQuest(map, idx)) Rebuild(); }, 22);
                }
                else
                {
                    var bar = UIKit.MakeBar(card, "Prog", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 96), new Vector2(cw - 44, 38), new Color(0.5f, 1f, 0.55f), 20);
                    bar.Set(q.progress / (float)def.count, q.progress + " / " + def.count);
                    var qq = q;
                    UIKit.Button(card, "Ab", "STOP", "Red", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(cw - 44, 56),
                                 () => { p.AbandonQuest(qq); Rebuild(); }, 18);
                }
            }

            Label(content, "COMPLETED: " + D.questsDone, 20, new Color(1f, 1f, 1f, 0.7f), new Vector2(30, -630), new Vector2(1460, 26));
        }
    }
}
