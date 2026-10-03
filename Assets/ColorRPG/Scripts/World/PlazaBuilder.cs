// =====================================================================
//  PlazaBuilder.cs - TRADE PLAZA: piata cu booth-uri (tarabe) NPC,
//  booth-ul jucatorului, cumparatori care se plimba, decor festiv.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public class PlazaBuilder
    {
        public static readonly Vector3 Origin = new Vector3(-300f, 0f, 0f);
        public static Vector3 Spawn { get { return Origin + new Vector3(0, 0.1f, -30f); } }
        public static readonly Color Ground = new Color(0.86f, 0.8f, 0.72f);

        System.Random rng = new System.Random(4242);
        float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

        /// <summary>Locurile din fata booth-urilor (pentru cumparatorii care se plimba). Se umple la Play.</summary>
        public static readonly List<Transform> BoothSpots = new List<Transform>();

        public PlazaBuilder() { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { BoothSpots.Clear(); }

        public void Build(Transform world)
        {
            var o = Origin;
            var plazaRoot = new GameObject("Trade Plaza").transform;
            plazaRoot.SetParent(world, false);
            var root = SceneGroups.Child(plazaRoot, "Terrain");
            var booths = SceneGroups.Child(plazaRoot, "Booths");
            var decor = SceneGroups.Child(plazaRoot, "Decor");
            var npcs = SceneGroups.Child(plazaRoot, "Shoppers (NPC)");

            // sol: iarba in jur + piata pavata
            bool nature = NatureKit.Available;
            var pGrass = nature ? Art.PaintedTex("Ground0") : null;
            var pCob = nature ? Art.PaintedTex("Cobble") : null;
            var grass = Art.Prim(PrimitiveType.Cube, o + new Vector3(0, -0.5f, 0), new Vector3(110, 1, 110), pGrass != null ? Color.white : new Color(0.45f, 0.82f, 0.4f), root, true, false);
            Art.ApplyTexture(grass.GetComponent<Renderer>().sharedMaterial, pGrass != null ? (Texture2D)pGrass : Art.GroundTex(0), pGrass != null ? 9f : 11f);
            var pave = Art.Prim(PrimitiveType.Cylinder, o + new Vector3(0, 0.02f, -2), new Vector3(76, 0.02f, 76), pCob != null ? new Color(1f, 0.97f, 0.92f) : Ground, root, false, false);
            Art.ApplyTexture(pave.GetComponent<Renderer>().sharedMaterial, pCob != null ? (Texture2D)pCob : Art.GroundTex(3), pCob != null ? 22f : 15f);
            pave.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // inel colorat in jurul fantanii
            var ring = Art.Prim(PrimitiveType.Cylinder, o + new Vector3(0, 0.03f, 0), new Vector3(15, 0.02f, 15), new Color(0.95f, 0.72f, 0.55f), root, false, false);
            Art.ApplyTexture(ring.GetComponent<Renderer>().sharedMaterial, pCob != null ? (Texture2D)pCob : Art.GroundTex(3), 4f);
            ring.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // margini
            float W = 54f;
            Color hedge = new Color(0.25f, 0.62f, 0.3f);
            if (nature)
            {
                // margine naturala: tufe si stanci joase + pereti invizibili
                Vector3[] wp = { new Vector3(0, 3, W), new Vector3(0, 3, -W), new Vector3(W, 3, 0), new Vector3(-W, 3, 0) };
                Vector3[] ws = { new Vector3(110, 6, 1.5f), new Vector3(110, 6, 1.5f), new Vector3(1.5f, 6, 110), new Vector3(1.5f, 6, 110) };
                for (int k = 0; k < 4; k++)
                {
                    var w = new GameObject("Wall (invisible)");
                    w.transform.SetParent(root, false);
                    w.transform.position = o + wp[k];
                    w.AddComponent<BoxCollider>().size = ws[k];
                }
                var th = NatureKit.For(0);
                for (int side = 0; side < 4; side++)
                    for (float t = -53f + R(0f, 4f); t <= 53f; t += R(7f, 10f))   // mai rar: muntii din spate inchid piata
                    {
                        bool isRock = rng.NextDouble() < 0.3;
                        float sz = isRock ? R(3f, 4.5f) : R(2.2f, 3.2f);
                        float ins = 55f - sz * 0.62f - R(0f, 6f);   // nimic nu iese peste marginea pietei; asezare neregulata
                        float tj = t + R(-3f, 3f);
                        Vector3 lp = side == 0 ? new Vector3(tj, 0, ins) : side == 1 ? new Vector3(tj, 0, -ins) : side == 2 ? new Vector3(ins, 0, tj) : new Vector3(-ins, 0, tj);
                        if (new Vector2(lp.x, lp.z + 2f).magnitude < 41f) continue;   // nu pe pavaj
                        if (isRock) { var rk = NatureKit.Place(NatureKit.Rocks[rng.Next(3)], o + lp, sz, R(0f, 360f), root, th, true); if (rk != null) { rk.localScale = new Vector3(1, 0.55f, 1); Obstacle.Add(rk.gameObject, sz * 0.42f, sz); } }
                        else NatureKit.Place(NatureKit.Bushes[rng.Next(2)], o + lp, sz, R(0, 360), root, th);
                    }
            }
            else
            {
                Art.Prim(PrimitiveType.Cube, o + new Vector3(0, 1.2f, W), new Vector3(110, 2.4f, 1.5f), hedge, root);
                Art.Prim(PrimitiveType.Cube, o + new Vector3(0, 1.2f, -W), new Vector3(110, 2.4f, 1.5f), hedge, root);
                Art.Prim(PrimitiveType.Cube, o + new Vector3(W, 1.2f, 0), new Vector3(1.5f, 2.4f, 110), hedge, root);
                Art.Prim(PrimitiveType.Cube, o + new Vector3(-W, 1.2f, 0), new Vector3(1.5f, 2.4f, 110), hedge, root);
            }

            // munti verzi in jurul pietei (ca in Meadow) - nu se mai vede golul de dupa margine
            OuterLands.Build(0, o, root, new System.Random(777), 56f, 9000);

            // fantana centrala
            var f = ModelLib.Place(GameData.TOWN + "fountain-round", o, 8f, 0f, root, true);
            if (f == null) Art.Prim(PrimitiveType.Cylinder, o + Vector3.up * 0.5f, new Vector3(7, 0.5f, 7), new Color(0.6f, 0.75f, 1f), root);
            // coliziune solida (perete cilindric inalt) - nu mai poti urca peste fantana
            var wall = new GameObject("FountainCollider");
            wall.transform.SetParent(root, false);
            wall.transform.position = o;
            var cc = wall.AddComponent<CapsuleCollider>();
            cc.radius = Shopper.FountainR - 0.2f; cc.height = 20f; cc.center = new Vector3(0, 10f, 0);
            // apa care curge: jet in sus + stropi care cad in bazin + unde
            var ff = new GameObject("FountainFx").transform;
            ff.SetParent(root, false);
            ff.position = o + Vector3.up * 2.4f;
            ff.gameObject.AddComponent<FountainFx>();
            WorldLabel.Create(ff, o + Vector3.up * 5.5f, "TRADE PLAZA", new Color(1f, 0.85f, 0.35f), 40f, 60f);

            // iarba 3D in jurul pietei pavate
            var gmat = nature ? null : Art.GrassMaterial(new Color(0.18f, 0.42f, 0.12f), new Color(0.6f, 0.88f, 0.35f));
            if (nature)
            {
                // smocuri de iarba si flori in jurul pietei (se ascund cu "3D grass" oprit)
                var det = SceneGroups.Child(plazaRoot, "Details (grass, flowers)");
                det.gameObject.AddComponent<DetailGroup>();
                var th0 = NatureKit.For(0);
                for (int k = 0; k < 60; k++)
                {
                    Vector3 lp = new Vector3(R(-50, 50), 0, R(-50, 50));
                    if (new Vector2(lp.x, lp.z + 2f).magnitude < 40f) continue;   // nu pe pavaj
                    if (Mathf.Abs(lp.x) < 4f && lp.z < -38f) continue;              // drumul spre portal
                    var nm = k % 3 == 0 ? NatureKit.Flowers[rng.Next(4)] : NatureKit.Grass[rng.Next(NatureKit.Grass.Length)];
                    var g = NatureKit.Place(nm, o + lp, R(0.6f, 1.2f), R(0, 360), det, th0);
                    if (g != null) foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            if (gmat != null)
            {
                var gg = new GameObject("Grass (3D)");
                gg.transform.SetParent(root, false);
                var gf = gg.AddComponent<GrassField>();
                gf.material = gmat;
                gf.density = 1.3f;
                gf.height = 0.5f;
                gf.seed = 21;
                gf.areaMin = new Vector2(o.x - 53f, o.z - 53f);
                gf.areaMax = new Vector2(o.x + 53f, o.z + 53f);
                gf.keepOutside = new Vector3(o.x, o.z - 2f, 39.5f);
            }

            // 10 booth-uri pentru jucatori, in cerc in jurul fantanii (intrarea ramane libera la sud)
            for (int i = 0; i < TradeDB.BoothCount; i++)
            {
                float a = Mathf.Lerp(-58f, 238f, i / (float)(TradeDB.BoothCount - 1)) * Mathf.Deg2Rad;
                Vector3 p = o + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 24f;
                BuildPlazaBooth(p, o, i, booths);
            }

            // portal de intoarcere + pavele de la piata pana la el (fara iarba peste ele)
            BuildReturnPortal(o + new Vector3(0, 0, -44f), plazaRoot);
            GrassField gfield = plazaRoot.GetComponentInChildren<GrassField>();
            Color paveCol = new Color(0.86f, 0.76f, 0.62f);
            for (int k = 0; k < 4; k++)
            {
                float z = -37.2f - k * 2.3f;
                for (int sd = -1; sd <= 1; sd += 2)
                {
                    Vector3 sp = o + new Vector3(sd * 1.05f + (k % 2 == 0 ? 0.15f : -0.15f), 0, z);
                    var st = Art.Prim(PrimitiveType.Cylinder, sp + Vector3.up * 0.045f, new Vector3(1.9f, 0.02f, 1.7f), paveCol, root, false, false);
                    Art.ApplyTexture(st.GetComponent<Renderer>().sharedMaterial, Art.GroundTex(3), 1f);
                    st.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    if (gfield != null) gfield.holes.Add(new Vector4(sp.x, 0, sp.z, 1.5f));
                }
            }
            if (gfield != null) gfield.holes.Add(new Vector4(o.x, 0, o.z - 44f, 3.2f));
            root = decor;

            // decor: copaci, felinare, stindarde, butoaie
            string[] trees = { GameData.TOWN + "tree", GameData.TOWN + "tree-high-round", GameData.NAT + "tree_oak", GameData.NAT + "tree_default", GameData.NAT + "tree_fat" };
            for (int k = 0; k < (nature ? 16 : 44); k++)
            {
                float side = R(0, 4);
                float t = R(-50, 50), inset = R(44f, 51f);
                Vector3 p = side < 1 ? new Vector3(t, 0, inset) : side < 2 ? new Vector3(t, 0, -inset) : side < 3 ? new Vector3(inset, 0, t) : new Vector3(-inset, 0, t);
                if (Mathf.Abs(p.x) < 8f && p.z < -40f) continue; // intrarea
                if (nature) { var nt = NatureKit.Place(NatureKit.Common[rng.Next(NatureKit.Common.Length)], o + p, R(7f, 10f), R(0, 360), root, NatureKit.For(0)); if (nt != null) Obstacle.Add(nt.gameObject, 0.7f, 6f); continue; }
                var tr = ModelLib.Place(trees[rng.Next(trees.Length)], o + p, R(5f, 8f), R(0, 360), root);
                if (tr != null) tr.gameObject.AddComponent<Sway>();
            }
            for (int k = 0; k < 12; k++)
            {
                float a = k / 12f * Mathf.PI * 2f;
                Vector3 p = o + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 12.5f;
                ModelLib.Place(GameData.TOWN + "lantern", p, 3f, -a * Mathf.Rad2Deg, root);
                if (k % 2 == 0) ModelLib.Place(GameData.NAT + "flower_" + (k % 4 == 0 ? "redA" : "yellowA"), p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.5f, 1f, 0f, root);
            }
            var cart = ModelLib.Place(GameData.TOWN + "cart", o + new Vector3(20, 0, -26), 2.8f, 60f, root);
            if (cart != null) Obstacle.Add(cart.gameObject, 1.2f, 2f);
            var b1 = ModelLib.Place(GameData.DUN + "barrel", o + new Vector3(22, 0, -22), 1.2f, 0f, root);
            if (b1 != null) Obstacle.Add(b1.gameObject, 0.45f, 1.2f);
            var b2 = ModelLib.Place(GameData.DUN + "barrel", o + new Vector3(23.4f, 0, -23), 1.2f, 40f, root);
            if (b2 != null) Obstacle.Add(b2.gameObject, 0.45f, 1.2f);
            var ch = ModelLib.Place(GameData.DUN + "chest", o + new Vector3(-26, 0, -26), 1.2f, 20f, root);
            if (ch != null) Obstacle.Add(ch.gameObject, 0.6f, 1f);
            if (!nature) ModelLib.Place(GameData.TOWN + "windmill", o + new Vector3(40, 0, 40), 14f, 225f, root);

            // ceilalti jucatori de pe server (max 17 + tu = 18)
            for (int k = 0; k < TradeDB.MaxPlayers - 1; k++)
            {
                var go = new GameObject("Shopper " + (k + 1));
                go.transform.SetParent(npcs, false);
                go.transform.position = o + new Vector3(R(-20, 20), 0, R(-20, 20));
                var s = go.AddComponent<Shopper>();
                s.Init(GameData.Heroes[rng.Next(GameData.Heroes.Length)], k);
            }
        }

        // ---------------------------------------------------------------
        //  Booth-uri
        // ---------------------------------------------------------------
        Transform BoothBase(string n, Vector3 p, Vector3 center, Transform root)
        {
            var t = new GameObject(n).transform;
            t.SetParent(root, false);
            t.position = p;
            Vector3 d = center - p; d.y = 0;
            t.rotation = Quaternion.LookRotation(d.normalized);
            return t;
        }

        /// <summary>Booth de jucator: taraba + vanzator (apare doar cand booth-ul e ocupat) + BoothSlot.</summary>
        void BuildPlazaBooth(Vector3 p, Vector3 center, int idx, Transform root)
        {
            Color col = TradeDB.BoothCols[idx % TradeDB.BoothCols.Length];
            var t = BoothBase("Booth " + (idx + 1), p, center, root);
            BuildStall(t, "stall", col);

            // vanzatorul (alt jucator) in spatele tejghelei
            var seller = new GameObject("Seller").transform;
            seller.SetParent(t, false);
            seller.localPosition = new Vector3(0, 0.16f, -1.05f);
            var npc = seller.gameObject.AddComponent<NpcTrader>();   // animatiile pornesc la Play
            npc.Init(GameData.CHAR + GameData.Heroes[rng.Next(GameData.Heroes.Length)]);

            var slot = t.gameObject.AddComponent<BoothSlot>();
            slot.index = idx;
            slot.col = col;

            var it = t.gameObject.AddComponent<Interactable>();
            it.radius = 5.2f;
            it.kind = InteractKind.PlazaBooth;
            it.target = idx;
            it.color = col;
        }

        /// <summary>
        /// Booth in stil Roblox: tejghea cu panou colorat, 2 stalpi, copertina in dungi,
        /// "franjuri" rotunde si un panou cu numele. Fata booth-ului = +Z local (spre piata).
        /// </summary>
        void BuildStall(Transform t, string stall, Color col)
        {
            Color wood = new Color(0.72f, 0.5f, 0.3f), woodDk = new Color(0.5f, 0.33f, 0.19f), woodLt = new Color(0.82f, 0.62f, 0.4f);
            Color light = Color.Lerp(col, Color.white, 0.75f);
            // podea de scanduri (in loc de covorul plat) + o treapta in fata
            for (int k = 0; k < 6; k++)
                Art.Prim(PrimitiveType.Cube, new Vector3(-2.5f + k * 1f, 0.09f, 0.55f), new Vector3(0.96f, 0.14f, 3.9f), k % 2 == 0 ? wood : woodLt, t, false);
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 0.05f, 2.75f), new Vector3(3.2f, 0.1f, 0.6f), woodDk, t, false);
            // tejghea cu scanduri si blat
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 0.62f, 0), new Vector3(4.2f, 1.0f, 1.3f), wood, t);
            for (int k = 0; k < 7; k++)
                Art.Prim(PrimitiveType.Cube, new Vector3(-1.8f + k * 0.6f, 0.62f, 0.66f), new Vector3(0.05f, 0.95f, 0.04f), woodDk, t, false);
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 1.17f, 0.05f), new Vector3(4.5f, 0.12f, 1.55f), woodDk, t, false);
            // panza colorata care atarna pe fata tejghelei, cu margine deschisa
            var cloth = Art.Prim(PrimitiveType.Cube, new Vector3(0, 0.86f, 0.71f), new Vector3(3.2f, 0.5f, 0.04f), col, t, false, false);
            Art.SetEmission(cloth.GetComponent<Renderer>().sharedMaterial, col * 0.1f);
            for (int k = 0; k < 8; k++)   // colturi triunghiulare la panza
            {
                var tri = Art.Prim(PrimitiveType.Cube, new Vector3(-1.4f + k * 0.4f, 0.6f, 0.71f), new Vector3(0.22f, 0.22f, 0.03f), k % 2 == 0 ? col : light, t, false);
                tri.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            }
            // stalpi
            for (int k = -1; k <= 1; k += 2)
            {
                Art.Prim(PrimitiveType.Cube, new Vector3(k * 2.1f, 1.85f, -0.6f), new Vector3(0.2f, 3.7f, 0.2f), woodDk, t, false);
                Art.Prim(PrimitiveType.Cube, new Vector3(k * 2.1f, 1.6f, 0.62f), new Vector3(0.17f, 3.2f, 0.17f), woodDk, t, false);
            }
            // copertina rotunjita (3 bucati cu unghiuri diferite) in dungi + volan la margine
            const int stripes = 7;
            float w = 4.9f / stripes;
            float[] ang = { -6f, -20f, -38f }, zz = { -0.45f, 0.35f, 1.05f }, yy = { 3.55f, 3.42f, 3.1f };
            for (int seg = 0; seg < 3; seg++)
            {
                var part = new GameObject("Awning" + seg).transform;
                part.SetParent(t, false);
                part.localPosition = new Vector3(0, yy[seg], zz[seg]);
                part.localRotation = Quaternion.Euler(ang[seg], 0, 0);
                for (int k = 0; k < stripes; k++)
                    Art.Prim(PrimitiveType.Cube, new Vector3(-2.45f + w * (k + 0.5f), 0, 0), new Vector3(w + 0.01f, 0.08f, 0.86f), k % 2 == 0 ? col : Color.white, part, false);
                if (seg == 2)
                    for (int k = 0; k < stripes * 2; k++)   // volan (semicercuri mici)
                    {
                        float x = -2.45f + (w * 0.5f) * (k + 0.5f);
                        var sc2 = Art.Prim(PrimitiveType.Sphere, new Vector3(x, -0.06f, 0.44f), Vector3.one, (k / 2) % 2 == 0 ? col : Color.white, part, false);
                        sc2.transform.localScale = new Vector3(w * 0.48f, 0.3f, 0.16f);
                    }
            }
            // felinare atarnate in colturile din fata
            Color lampC = new Color(1f, 0.82f, 0.45f);
            for (int k = -1; k <= 1; k += 2)
            {
                Art.Prim(PrimitiveType.Cube, new Vector3(k * 2.35f, 2.95f, 0.75f), new Vector3(0.5f, 0.07f, 0.07f), woodDk, t, false);
                Art.Prim(PrimitiveType.Cube, new Vector3(k * 2.55f, 2.75f, 0.75f), new Vector3(0.03f, 0.35f, 0.03f), new Color(0.2f, 0.2f, 0.22f), t, false);
                Art.Prim(PrimitiveType.Cube, new Vector3(k * 2.55f, 2.42f, 0.75f), new Vector3(0.3f, 0.06f, 0.3f), new Color(0.2f, 0.2f, 0.22f), t, false);
                var lamp = Art.Prim(PrimitiveType.Cube, new Vector3(k * 2.55f, 2.24f, 0.75f), new Vector3(0.24f, 0.32f, 0.24f), lampC, t, false, false);
                Art.SetEmission(lamp.GetComponent<Renderer>().sharedMaterial, lampC * 1.2f);
            }
            // marfa decorativa pe laterale: lazi, saci, un cos cu fructe colorate (centrul tejghelei ramane pentru obiectele de vanzare)
            Color crate = new Color(0.66f, 0.46f, 0.26f), sack = new Color(0.78f, 0.68f, 0.5f);
            for (int k = -1; k <= 1; k += 2)
            {
                Vector3 b = new Vector3(k * 2.85f, 0, 0.9f);
                Art.Prim(PrimitiveType.Cube, b + new Vector3(0, 0.38f, 0), new Vector3(0.75f, 0.6f, 0.75f), crate, t, false);
                Art.Prim(PrimitiveType.Cube, b + new Vector3(0, 0.69f, 0), new Vector3(0.8f, 0.05f, 0.8f), woodDk, t, false);
                if (k < 0)
                {
                    Color[] fruit = { new Color(1f, 0.3f, 0.25f), new Color(1f, 0.75f, 0.2f), new Color(0.5f, 0.9f, 0.3f) };
                    for (int f = 0; f < 6; f++)
                        Art.Prim(PrimitiveType.Sphere, b + new Vector3(-0.22f + (f % 3) * 0.22f, 0.82f + (f / 3) * 0.1f, -0.1f + (f / 3) * 0.2f), Vector3.one * 0.22f, fruit[f % 3], t, false);
                }
                else
                {
                    var s1 = Art.Prim(PrimitiveType.Sphere, b + new Vector3(0, 0.95f, 0), new Vector3(0.55f, 0.6f, 0.5f), sack, t, false);
                    var s2 = Art.Prim(PrimitiveType.Sphere, b + new Vector3(0.05f, 0.32f, 0.62f), new Vector3(0.5f, 0.55f, 0.45f), sack, t, false);
                    s1.transform.localRotation = Quaternion.Euler(0, 20, 8); s2.transform.localRotation = Quaternion.Euler(0, -15, -6);
                }
            }
            // panou cu numele deasupra (rama de lemn)
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 4.15f, -0.66f), new Vector3(3.3f, 0.95f, 0.1f), woodDk, t, false);
            var board = Art.Prim(PrimitiveType.Cube, new Vector3(0, 4.15f, -0.6f), new Vector3(3.0f, 0.72f, 0.06f), col, t, false, false);
            Art.SetEmission(board.GetComponent<Renderer>().sharedMaterial, col * 0.3f);
            // (fara lumina punctiforma - felinarele sunt emisive)
            var col1 = t.gameObject.AddComponent<BoxCollider>();
            col1.center = new Vector3(0, 1f, -0.1f);
            col1.size = new Vector3(4.4f, 2f, 1.6f);
        }

        void BuildReturnPortal(Vector3 p, Transform root)
        {
            var t = new GameObject("ReturnPortal").transform;
            t.SetParent(root, false);
            t.position = p;
            Color c = new Color(0.5f, 1f, 0.6f);
            PortalArt.Build(t, c, new Color(0.62f, 0.6f, 0.66f));

            var it = t.gameObject.AddComponent<Interactable>();
            it.radius = 4.5f;
            it.kind = InteractKind.PlazaReturn;
            it.color = c;
            WorldLabel.Create(t, p + Vector3.up * PortalArt.LabelY, "BACK TO ADVENTURE", c, 24f, 40f);
        }
    }

    // ------------------------------------------------------------------
    //  Porneste animatiile unui model deja pus in scena (copilul "M_...")
    // ------------------------------------------------------------------
    public static class SceneAnim
    {
        public static SimpleAnimator Attach(Transform owner, string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            Transform model = null;
            for (int i = 0; i < owner.childCount; i++)
                if (owner.GetChild(i).name.StartsWith("M_")) { model = owner.GetChild(i); break; }
            if (model == null) return null;
            return SimpleAnimator.Setup(model, path);
        }
    }



}
