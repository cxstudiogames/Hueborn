// BoothSlot.cs - componenta salvata in scena (numele fisierului = numele clasei, cerinta Unity)
// Unul din cele 10 booth-uri din Trade Plaza. La Play arata cine il ocupa pe serverul curent:
// alt jucator (vanzator + numele lui + itemele lui), booth-ul tau sau "FREE BOOTH".
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class BoothSlot : MonoBehaviour
    {
        [Tooltip("Indexul booth-ului (0..9).")]
        public int index;
        public Color col = Color.white;

        Text label, sub;
        GameObject seller;
        readonly List<GameObject> gems = new List<GameObject>();
        string shownKey = "#";
        float t;

        void Start()
        {
            var npc = GetComponentInChildren<NpcTrader>(true);
            if (npc != null) seller = npc.gameObject;
        }

        void Update()
        {
            var g = Game.I;
            var ts = TradeSystem.I;
            if (g == null || g.hud == null || ts == null || ts.Server == null) return;
            if (label == null)
            {
                label = g.hud.Label(transform.position + Vector3.up * 5.5f, "", Color.white, 26f, 50f);
                sub = g.hud.Label(transform.position + Vector3.up * 4.85f, "", Color.white, 16f, 30f);
            }
            for (int i = 0; i < gems.Count; i++)
                if (gems[i] != null) gems[i].transform.localRotation = Quaternion.Euler(45, Time.time * 60f + i * 30f, 45);
            t -= Time.deltaTime;
            if (t > 0f) return;
            t = 0.3f;

            var D = g.player.D;
            bool mine = ts.IsMine(index);
            var sb = ts.Server.booths[index];
            string key;
            if (mine)
            {
                key = "me";
                foreach (var l in D.booth) key += "," + l.Name + l.price;
                ts.playerBoothPos = transform.position;
            }
            else if (sb != null)
            {
                key = "sim" + ts.Server.id + sb.owner + sb.netOwner;
                foreach (var l in sb.stock) key += l.sold ? ",x" : "," + l.Name;
            }
            else key = "free" + ts.MyBoothHere;

            if (key != shownKey)
            {
                shownKey = key;
                if (mine) { label.text = "YOUR BOOTH"; label.color = new Color(1f, 0.82f, 0.25f); sub.text = D.playerName; }
                else if (sb != null) { label.text = sb.owner.ToUpper(); label.color = Color.Lerp(col, Color.white, 0.3f); sub.text = sb.netOwner != 0 ? "<color=#7dff8a>● online player</color>" : ""; }
                else { label.text = "FREE BOOTH"; label.color = new Color(0.6f, 1f, 0.6f); sub.text = ts.MyBoothHere ? "" : Device.StripKeys("[E] claim"); }
                if (seller != null) seller.SetActive(sb != null && !mine && sb.netOwner == 0);   // jucator real: el insusi e prin piata

                // itemele de pe tejghea (pietre colorate dupa raritate)
                foreach (var x in gems) if (x != null) Destroy(x);
                gems.Clear();
                var cols = new List<Color>();
                if (mine) foreach (var l in D.booth) cols.Add(l.Col);
                else if (sb != null) foreach (var l in sb.stock) if (!l.sold) cols.Add(l.Col);
                for (int i = 0; i < cols.Count && i < 6; i++)
                {
                    var gem = Art.Prim(PrimitiveType.Cube, new Vector3(-1.4f + (i % 3) * 1.4f, 1.55f + (i / 3) * 0.6f, 0.1f), Vector3.one * 0.38f, cols[i], transform, false, false);
                    Art.SetEmission(gem.GetComponent<Renderer>().sharedMaterial, cols[i] * 0.6f);
                    gems.Add(gem);
                }
            }
        }
    }
}
