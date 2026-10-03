// Interactable.cs - componenta salvata in scena (numele fisierului = numele clasei, cerinta Unity)
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        [Tooltip("Ce face obiectul cand apesi E.")]
        public InteractKind kind = InteractKind.Custom;
        [Tooltip("MapPortal / EggAltar: indexul hartii.  PlazaBooth: booth-ul 0..9.")]
        public int target;
        [Tooltip("MapPortal: true = portal de intoarcere (fara nivel minim).")]
        public bool back;
        public float radius = 5f;
        public Color color = new Color(1f, 1f, 0.4f);

        [System.NonSerialized] public System.Func<string> label;
        [System.NonSerialized] public System.Action action;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public string Label()
        {
            if (label == null) Bind();
            return label != null ? label() : "";
        }

        public void Act()
        {
            if (action == null) Bind();
            if (action != null) action();
        }

        void Start()
        {
            // cadoul sta pe loc (nu se mai roteste singur)
            if (kind == InteractKind.Gift) foreach (var sp in GetComponentsInChildren<Spinner>(true)) sp.enabled = false;
        }

        /// <summary>Leaga textul si actiunea in functie de "kind".</summary>
        public void Bind()
        {
            var g = Game.I;
            if (g == null) return;
            Vector3 p = transform.position;
            switch (kind)
            {
                case InteractKind.Forge:
                    label = () => g.player.ForgeLabel();
                    action = () => g.hud.forge.OpenAt(p);
                    break;
                case InteractKind.EggAltar:
                    {
                        int map = target;
                        label = () => g.player.EggLabel(map);
                        action = () => g.player.Hatch(map, p);
                        break;
                    }
                case InteractKind.Rebirth:
                    label = () => g.player.RebirthLabel();
                    action = () => g.player.DoRebirth();
                    break;
                case InteractKind.Shop:
                    label = () => Device.Mobile ? "[E] Shop: sell / equip items" : "[E] Shop: sell / equip items   (potion: Q = " + g.player.PotionCost + " coins)";
                    action = () => g.hud.OpenInventory(true);
                    break;
                case InteractKind.Gift:
                    label = () => g.GiftLabel();
                    action = () => g.ClaimGift(p);
                    break;
                case InteractKind.HeroPad:
                    label = () => "[E] Change hero (" + (g.player.D.hero + 1) + "/" + GameData.Heroes.Length + ")";
                    action = () => g.player.NextHero();
                    break;
                case InteractKind.TradePortal:
                    label = () => "[E] Go to the TRADE PLAZA (booths, trading, shops)";
                    action = () => g.GoPlaza();
                    break;
                case InteractKind.MapPortal:
                    {
                        int t = Mathf.Clamp(target, 0, GameData.Maps.Length - 1);
                        var tm = GameData.Maps[t];
                        bool b = back;
                        label = () => b ? "[E] Back to " + tm.name : "[E] Portal to " + tm.name;
                        action = () => g.Travel(t, b);
                        break;
                    }
                case InteractKind.PlazaReturn:
                    label = () => "[E] Back to " + GameData.Maps[0].name;
                    action = () => g.Travel(0, true);
                    break;
                case InteractKind.PlazaBooth:
                    {
                        int bi = Mathf.Clamp(target, 0, TradeDB.BoothCount - 1);
                        label = () =>
                        {
                            var ts = TradeSystem.I;
                            if (ts == null || ts.Server == null) return "";
                            var D = g.player.D;
                            if (ts.IsMine(bi))
                                return "[E] YOUR BOOTH  |  " + D.booth.Count + "/" + TradeDB.MaxListings + " listings for sale" +
                                       (D.boothEarnings > 0 ? "  |  " + Fmt.Num(D.boothEarnings) + " tokens to collect!" : "");
                            var sb = ts.Server.booths[bi];
                            if (sb != null)
                            {
                                int n = 0; foreach (var l in sb.stock) if (!l.sold) n++;
                                return "[E] Visit " + sb.owner + "'s booth  (" + n + " listings, paid in tokens)";
                            }
                            if (ts.MyBoothHere) return "Free booth  (you already have a booth on this server)";
                            return "[E] CLAIM this free booth and sell your items";
                        };
                        action = () =>
                        {
                            var ts = TradeSystem.I;
                            if (ts == null || ts.Server == null) return;
                            if (ts.IsMine(bi)) { g.hud.trade.OpenMyBooth(); return; }
                            if (ts.Server.booths[bi] != null) { g.hud.trade.Open(bi); return; }
                            if (ts.Claim(bi)) g.hud.trade.OpenMyBooth();
                        };
                        break;
                    }
                case InteractKind.ItemIndex:
                    label = () => "[E] ITEM INDEX  (" + ((g.player.D.seenCat != null ? g.player.D.seenCat.Count : 0) + (g.player.D.seenPets != null ? g.player.D.seenPets.Count : 0)) + "/" + (ItemCatalog.Count + GameData.MapPets.Length * ItemDB.UsedRarities.Length) + " found)";
                    action = () => g.hud.index.Open();
                    break;
                case InteractKind.QuestGiver:
                    {
                        int map = Mathf.Clamp(target, 0, 3);
                        label = () =>
                        {
                            int active = 0;
                            foreach (var q in g.player.D.quests) if (q.map == map) active++;
                            return "[E] Talk to " + QuestDB.NpcNames[map] + (active > 0 ? "  (your quest is from here)" : g.player.D.quests.Count > 0 ? "  (switch your quest)" : "  (pick a quest)");
                        };
                        action = () => g.hud.quests.OpenFor(map);
                        break;
                    }
                default:
                    label = () => "";
                    action = () => { };
                    break;
            }
        }
    }
}
