// =====================================================================
//  SceneComponents.cs - componente care pot sta salvate in scena (Hierarchy):
//   * WorldRoot    - radacina lumii construite (=== WORLD ===), cu versiunea ei
//   * Interactable - orice lucru cu [E] (forja, portal, booth...). Tipul se alege
//                    din Inspector (kind / target), actiunea se leaga la Play.
//   * WorldLabel   - textul care pluteste deasupra unui obiect (ex. "FORGE")
//   * SceneGroups  - folderele din Hierarchy pentru obiectele create in Play
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{

    public enum InteractKind
    {
        Custom,        // label/action puse din cod
        Forge, EggAltar, Rebirth, Shop, Gift, HeroPad,
        TradePortal,   // spre Trade Plaza
        MapPortal,     // spre harta "target" (back = fara verificare de nivel)
        PlazaReturn,   // din piata inapoi in prima harta
        NpcBooth,      // (vechi) nu mai e folosit
        PlayerBooth,   // (vechi) nu mai e folosit
        PlazaBooth,    // unul din cele 10 booth-uri din Trade Plaza (target = 0..9)
        QuestGiver,    // NPC-ul cu questuri de la spawn (target = harta)
        ItemIndex      // cartea ITEM INDEX din Meadow (toate itemele, gasite / negasite)
    }



    /// <summary>
    /// "Folderele" din Hierarchy pentru obiectele care apar doar in Play
    /// (jucator, inamici, loot, pets, sisteme). Asa Hierarchy ramane ordonat.
    /// </summary>
    public static class SceneGroups
    {
        static readonly Dictionary<string, Transform> groups = new Dictionary<string, Transform>();
        static Transform runtimeRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { groups.Clear(); runtimeRoot = null; }

        public static Transform Runtime
        {
            get
            {
                if (runtimeRoot == null) runtimeRoot = new GameObject("=== RUNTIME (Play only) ===").transform;
                return runtimeRoot;
            }
        }

        public static Transform Get(string name)
        {
            Transform t;
            if (groups.TryGetValue(name, out t) && t != null) return t;
            t = new GameObject(name).transform;
            t.SetParent(Runtime, false);
            groups[name] = t;
            return t;
        }

        /// <summary>Un sub-folder simplu (ex. "Decor" in interiorul unei harti).</summary>
        public static Transform Child(Transform parent, string name)
        {
            var e = parent.Find(name);
            if (e != null) return e;
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }
    }
}
