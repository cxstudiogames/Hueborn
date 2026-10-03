// =====================================================================
//  WorldBuilder.cs - construieste cele 3 harti, zona de start (forja, oua,
//  rebirth, magazin, cadou, portaluri) si decorul cu modele CC0.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public class WorldBuilder
    {
        System.Random rng;
        readonly List<Vector3> blocked = new List<Vector3>();
        readonly List<float> blockedR = new List<float>();

        public WorldBuilder() { }

        /// <summary>
        /// Construieste TOATA lumea (3 harti + Trade Plaza) sub "=== WORLD ===".
        /// Folosit si de jocul in Play (daca scena e goala), si de meniul din editor
        /// ColorRPG > Rebuild World In Scene (ca lumea sa apara in Hierarchy).
        /// </summary>
        public static WorldRoot BuildAll()
        {
            GameData.Init();
            var root = new GameObject("=== WORLD ===").AddComponent<WorldRoot>();
            root.version = GameData.WorldVersion;
            var wb = new WorldBuilder();
            for (int i = 0; i < GameData.Maps.Length; i++) wb.BuildMap(i, root.transform);
            new PlazaBuilder().Build(root.transform);
            Art.MatteGround(root.transform);   // solul / pavajul fara luciu (altfel culoarea "se spala" cand te intorci spre soare)
            return root;
        }

        float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
        T Pick<T>(T[] arr) { return arr[rng.Next(arr.Length)]; }

        // ---------------------------------------------------------------
        //  Harta
        // ---------------------------------------------------------------
        public void BuildMap(int i, Transform world)
        {
            var m = GameData.Maps[i];
            var o = m.origin;
            rng = new System.Random(1234 + i * 77);
            blocked.Clear(); blockedR.Clear(); paveHoles.Clear();
            var mapRoot = new GameObject("Map " + (i + 1) + " - " + m.name).transform;
            mapRoot.SetParent(world, false);
            // "foldere" in Hierarchy
            var terrain = SceneGroups.Child(mapRoot, "Terrain");
            var start = SceneGroups.Child(mapRoot, "Start Area");
            var portals = SceneGroups.Child(mapRoot, i == 0 ? "Teleport Corner" : "Portals");
            var arena = SceneGroups.Child(mapRoot, "Boss Arena");
            var decor = SceneGroups.Child(mapRoot, "Decor");
            var root = terrain;

            // sol + pete de culoare
            nature = NatureKit.Available;
            curMap = i;
            var painted = Art.PaintedTex("Ground" + Mathf.Clamp(i, 0, 3));
            bool usePaint = nature && painted != null;
            var ground = Art.Prim(PrimitiveType.Cube, o + new Vector3(0, -0.5f, 0), new Vector3(140, 1, 140), usePaint ? Color.white : m.ground, root, true, false);
            ground.name = "Ground";
            var gtex = usePaint ? (Texture2D)painted : Art.GroundTex(i);
            Art.ApplyTexture(ground.GetComponent<Renderer>().sharedMaterial, gtex, usePaint ? 11f : 14f);
            for (int k = 0; k < (usePaint ? 0 : 26); k++)
            {
                if (usePaint)
                {
                    // cateva pete mari, abia vizibile (variatie de culoare, nu "covor" de pete)
                    float ps = R(14f, 26f);
                    var pp = Art.Prim(PrimitiveType.Cylinder, o + new Vector3(R(-56, 56), 0.004f + k * 0.0004f, R(-20, 58)), new Vector3(ps, 0.004f, ps * R(0.6f, 1f)),
                                      k % 2 == 0 ? new Color(0.9f, 0.92f, 0.88f) : new Color(1.08f, 1.06f, 1f), root, false, false);
                    pp.transform.rotation = Quaternion.Euler(0, R(0, 180), 0);
                    Art.ApplyTexture(pp.GetComponent<Renderer>().sharedMaterial, gtex, ps / 12f);
                    pp.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    continue;
                }
                float s = R(8f, 22f);
                var patch = Art.Prim(PrimitiveType.Cylinder, o + new Vector3(R(-60, 60), 0.005f + k * 0.0004f, R(-60, 60)), new Vector3(s, 0.005f, s * R(0.6f, 1f)),
                                     Color.Lerp(m.ground, m.ground2, R(0.4f, 1f)), root, false, false);
                patch.transform.rotation = Quaternion.Euler(0, R(0, 180), 0);
                Art.ApplyTexture(patch.GetComponent<Renderer>().sharedMaterial, gtex, s / 10f);
                patch.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // margini (ziduri joase colorate + decor dens)
            float W = 69f;
            if (nature) NatureBorder(i, o, SceneGroups.Child(mapRoot, "Border"));
            else
            {
                Art.Prim(PrimitiveType.Cube, o + new Vector3(0, 1.5f, W + 0.5f), new Vector3(142, 3, 1), m.border, root);
                Art.Prim(PrimitiveType.Cube, o + new Vector3(0, 1.5f, -W - 0.5f), new Vector3(142, 3, 1), m.border, root);
                Art.Prim(PrimitiveType.Cube, o + new Vector3(W + 0.5f, 1.5f, 0), new Vector3(1, 3, 142), m.border, root);
                Art.Prim(PrimitiveType.Cube, o + new Vector3(-W - 0.5f, 1.5f, 0), new Vector3(1, 3, 142), m.border, root);
            }
            // peisajul de dupa margine: sol larg + dealuri + munti in tema hartii (nu se mai vede golul)
            OuterLands.Build(i, o, SceneGroups.Child(mapRoot, "Border"), rng);

            // zona de start (platforma) + drum spre arena boss-ului
            // harta 1 = HUB-ul principal (platforma mai mare, totul aranjat); celelalte harti sunt doar pentru farmat
            bool hub = i == 0;
            // platforma de start: un cerc ordonat (bordura de piatra + inel interior + rozeta in centru)
            float PR = hub ? 21f : 15f;
            Color pave = Color.Lerp(m.ground, Color.white, 0.55f), rim = Color.Lerp(m.ground, new Color(0.45f, 0.45f, 0.5f), 0.65f);
            var plazaRim = Art.Prim(PrimitiveType.Cylinder, o + new Vector3(0, 0.015f, -44), new Vector3(PR * 2f + 1.6f, 0.02f, PR * 2f + 1.6f), rim, root, false);
            plazaRim.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var plaza = Art.Prim(PrimitiveType.Cylinder, o + new Vector3(0, 0.025f, -44), new Vector3(PR * 2f, 0.02f, PR * 2f), pave, root, false);
            plaza.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var cob = nature ? Art.PaintedTex("Cobble") : null;
            if (cob != null)
            {
                // pavaj de piatra pe cercul de spawn (placut de calcat), nuantat dupa harta
                Color cobCol = CobCol(i);
                var pm = new Material(plaza.GetComponent<Renderer>().sharedMaterial);
                Art.SetColor(pm, cobCol);
                Art.ApplyTexture(pm, cob, PR / 3.2f);
                plaza.GetComponent<Renderer>().sharedMaterial = pm;
                var rm = new Material(plazaRim.GetComponent<Renderer>().sharedMaterial);
                Art.SetColor(rm, cobCol * 0.72f);
                Art.ApplyTexture(rm, cob, PR / 2.4f);
                plazaRim.GetComponent<Renderer>().sharedMaterial = rm;
            }
            if (cob != null) StoneDisc(o + new Vector3(0, 0, -46), 3.9f, root);   // centrul: piatra pavata + bordura de pietre (fara discuri verzi)
            else
            {
            var ring = Art.Prim(PrimitiveType.Cylinder, o + new Vector3(0, 0.03f, -46), new Vector3(9f, 0.02f, 9f), rim, root, false);
            ring.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var rose = Art.Prim(PrimitiveType.Cylinder, o + new Vector3(0, 0.035f, -46), new Vector3(7.6f, 0.02f, 7.6f), Color.Lerp(pave, Color.white, 0.35f), root, false);
            rose.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            for (int k = 0; k < (cob != null ? 0 : 4); k++)
            {
                var ray = Art.Prim(PrimitiveType.Cube, o + new Vector3(0, 0.04f, -46), new Vector3(0.35f, 0.02f, 7.4f), rim, root, false);
                ray.transform.rotation = Quaternion.Euler(0, k * 45f, 0);
                ray.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            float pathStart = -44f + PR + 0.8f;   // drumul incepe exact de la marginea cercului de spawn
            for (float z = pathStart; z < 36f; z += 3.2f)
                Paver(o + new Vector3(Mathf.Sin(z * 0.12f) * 3f + R(-0.4f, 0.4f), 0, z), R(1.6f, 2.2f), R(1.4f, 2f), Color.Lerp(m.ground, new Color(0.75f, 0.72f, 0.68f), 0.6f), root);
            Block(o + new Vector3(0, 0, -44), PR + 1.5f);
            for (float z = pathStart; z < 36f; z += 4f) Block(o + new Vector3(Mathf.Sin(z * 0.12f) * 3f, 0, z), 4f);

            BuildBossArena(i, o + new Vector3(0, 0, 48), arena);

            // NPC-ul cu questuri: pe iarba, chiar la inceputul pavelelor care duc spre monstri (zona din jur ramane libera)
            {
                // NPC-ul cu questuri: pe iarba, in stanga drumului, cu aleea lui (ca oul / forja / portalul)
                Vector3 qc = o + new Vector3(0, 0, -44);
                float qa = 112.5f * Mathf.Deg2Rad;
                Vector3 qd = new Vector3(Mathf.Cos(qa), 0, Mathf.Sin(qa));
                Vector3 qp = qc + qd * (PR + 8.5f);
                BuildQuestGiver(qp, start, i, Mathf.Atan2(qc.x - qp.x, qc.z - qp.z) * Mathf.Rad2Deg);
                BuildAlley(qc + qd * (PR + 0.6f), qp - qd * 3.4f, Color.Lerp(m.ground, new Color(0.75f, 0.72f, 0.68f), 0.6f), new Color(1f, 0.82f, 0.3f), start);
            }

            // OUL (dreapta-fata) si FORJA (stanga-fata) au fiecare locul lor in afara cercului de spawn,
            // legat de cerc printr-o alee cu pavele si felinare (pe fiecare harta)
            Vector3 pc = o + new Vector3(0, 0, -44);
            float spotX = hub ? 28f : 23f, spotZ = hub ? -17f : -22f;
            Vector3 eggP = o + new Vector3(spotX, 0, spotZ), forgeP = o + new Vector3(-spotX, 0, spotZ);
            Color eggLamp = new Color(1f, 0.6f, 0.95f), forgeLamp = new Color(1f, 0.6f, 0.25f);
            Color paveCol = Color.Lerp(m.ground, new Color(0.75f, 0.72f, 0.68f), 0.6f);
            BuildSpotPad(eggP, 4.8f, m, start);
            BuildEgg(eggP, start, i);
            BuildAlley(pc + (eggP - pc).normalized * (PR + 0.6f), eggP - (eggP - pc).normalized * 5.2f, paveCol, eggLamp, start);
            if (hub || i == 1 || i == 3)
            {
                BuildForge(forgeP, start, Mathf.Atan2(pc.x - forgeP.x, pc.z - forgeP.z) * Mathf.Rad2Deg);
                BuildAlley(pc + (forgeP - pc).normalized * (PR + 0.6f), forgeP - (forgeP - pc).normalized * 5.4f, paveCol, forgeLamp, start);
            }
            if (hub)
            {
                // ---- ITEM INDEX: in dreapta drumului, simetric cu NPC-ul de questuri, cu aleea lui
                Vector3 ic = o + new Vector3(0, 0, -44);
                float ia = 67.5f * Mathf.Deg2Rad;
                Vector3 idir = new Vector3(Mathf.Cos(ia), 0, Mathf.Sin(ia));
                Vector3 ip = ic + idir * (PR + 8.5f);
                BuildSpotPad(ip, 3.6f, m, start);
                BuildIndex(ip, start, Mathf.Atan2(ic.x - ip.x, ic.z - ip.z) * Mathf.Rad2Deg);
                BuildAlley(ic + idir * (PR + 0.6f), ip - idir * 4.2f, paveCol, new Color(0.75f, 0.85f, 1f), start);
            }
            if (hub)
            {
                // ---- HUB: fantana + cadou in cerc; pereti: stanga CLASAMENTE, dreapta PORTALURI; REBIRTH langa boss
                BuildGift(o + new Vector3(0, 0, -59), start);
                BuildHubDecor(o, start);
                BuildRebirthShrine(o, start, arena);
            }
            curMap = i;
            if (i == 0)
            {
                BuildTeleportCorner(o, portals);
                BuildLeaderboards(o, start);
            }
            else
            {
                // hartile de farm au doar drumul inapoi spre hub (in dreapta, ca in hub)
                // portalul spre hub: pe platforma lui, in dreapta cercului, cu alee de acces din cerc
                Vector3 bp = o + new Vector3(PR + 9f, 0, -44f);
                if (!nature)
                {
                    var pad = Art.Prim(PrimitiveType.Cube, bp + new Vector3(0, 0.03f, 0), new Vector3(6.5f, 0.06f, 8f), new Color(0.72f, 0.72f, 0.8f), portals, false);
                    pad.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                paveHoles.Add(new Vector4(bp.x, 0, bp.z, 5.2f));
                BuildPortal(bp, 0, portals, "Back to ", -90f);
                Block(bp, 5.5f);
                BuildAlley(o + new Vector3(PR + 0.6f, 0, -44f), bp - new Vector3(3.6f, 0, 0), Color.Lerp(m.ground, new Color(0.75f, 0.72f, 0.68f), 0.6f), new Color(0.75f, 0.85f, 1f), portals);
            }

            if (nature) NatureDecor(i, o, decor, SceneGroups.Child(mapRoot, "Details (grass, flowers)"));
            else if (i == 0) DecorMeadow(o, decor);
            else if (i == 1) DecorDesert(o, decor);
            else if (i == 2) DecorFrozen(o, decor);
            else DecorLava(o, decor);

            // iarba 3D (verde pe pajiste, uscata in desert, rara si inghetata la munte) - inlocuita de smocurile din NatureDecor
            if (nature) { }
            else if (i == 0) AddGrass(terrain, o, new Color(0.16f, 0.4f, 0.1f), new Color(0.62f, 0.88f, 0.34f), 1.5f, 0.6f, 11);
            else if (i == 1) AddGrass(terrain, o, new Color(0.45f, 0.36f, 0.16f), new Color(0.95f, 0.86f, 0.5f), 0.16f, 0.5f, 12);
            else if (i == 2) AddGrass(terrain, o, new Color(0.35f, 0.45f, 0.45f), new Color(0.88f, 0.96f, 1f), 0.08f, 0.4f, 13);
        }

        void Block(Vector3 p, float r) { blocked.Add(p); blockedR.Add(r); }

        void AddGrass(Transform parent, Vector3 o, Color root, Color tip, float density, float height, int seed)
        {
            var mat = Art.GrassMaterial(root, tip);
            if (mat == null) return;
            var go = new GameObject("Grass (3D)");
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<GrassField>();
            g.material = mat;
            g.density = density;
            g.height = height;
            g.seed = seed;
            g.areaMin = new Vector2(o.x - 67f, o.z - 67f);
            g.areaMax = new Vector2(o.x + 67f, o.z + 67f);
            // fara iarba pe drumuri, platforme, arene si sub cladiri mari
            for (int k = 0; k < blocked.Count; k++)
                if (blockedR[k] >= 5f) g.holes.Add(new Vector4(blocked[k].x, 0, blocked[k].z, blockedR[k] * 0.85f));
            g.holes.Add(new Vector4(o.x, 0, o.z - 44f, (o.x == GameData.Maps[0].origin.x && o.z == GameData.Maps[0].origin.z ? 21f : 15f) + 1.2f));
            foreach (var h in paveHoles) if (Mathf.Abs(h.x - o.x) < 75f && Mathf.Abs(h.z - o.z) < 75f) g.holes.Add(h);
        }

        int curMap;
        bool nature;   // pachetul "Stylized Nature MegaKit" e in proiect -> lumea noua
        // Cele doua "pereti" ale hub-ului, simetrici fata de spawn: dreapta = PORTALURI, stanga = CLASAMENTE.
        // Fiecare e o platforma de piatra lunga (de-a lungul lui Z) intoarsa cu fata spre spawn.
        const float CornerX = 46f, CornerZ = -46f, CornerLen = 34f, CornerDepth = 11f;

        /// <summary>Platforma de piatra: bordura in spate, felinare simetrice in fata. yaw = incotro priveste (spre spawn).</summary>
        Transform BuildCornerPlatform(string n, Vector3 c, float yaw, Transform root)
        {
            var t = Anchor(n, c, root);
            t.rotation = Quaternion.Euler(0, yaw, 0);
            Color st = new Color(0.72f, 0.72f, 0.8f), dk = new Color(0.55f, 0.55f, 0.63f), post = new Color(0.45f, 0.45f, 0.52f);
            var floor = Art.Prim(PrimitiveType.Cube, new Vector3(0, 0.04f, 0), new Vector3(CornerLen, 0.08f, CornerDepth), st, t, false);
            floor.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // margine (bordura joasa pe tot conturul, mai inalta in spate)
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 0.45f, -CornerDepth * 0.5f + 0.4f), new Vector3(CornerLen, 0.9f, 0.8f), dk, t);
            for (int s = -1; s <= 1; s += 2)
            {
                Art.Prim(PrimitiveType.Cube, new Vector3(s * (CornerLen * 0.5f - 0.3f), 0.15f, 0), new Vector3(0.6f, 0.3f, CornerDepth), dk, t, false);
            }
            // fara copaci / iarba pe platforma
            for (float x = -CornerLen * 0.5f + 3f; x <= CornerLen * 0.5f - 3f; x += 5f)
                Block(c + t.rotation * new Vector3(x, 0, 0), 6.5f);
            return t;
        }

        void BuildTeleportCorner(Vector3 o, Transform root)
        {
            Vector3 c = o + new Vector3(CornerX, 0, CornerZ);
            var t = BuildCornerPlatform("Teleport Corner", c, -90f, root);
            int n = GameData.Maps.Length;   // hartile 1..n-1 + Trade Plaza
            float step = 8.2f;
            float x0 = -step * (n - 1) * 0.5f;
            for (int k = 1; k < n; k++) BuildPortal(c + t.rotation * new Vector3(x0 + (k - 1) * step, 0, 0.6f), k, root, "", -90f);
            BuildTradePortal(c + t.rotation * new Vector3(x0 + (n - 1) * step, 0, 0.6f), root, -90f);
            WorldLabel.Create(root, c + new Vector3(0, 8.6f, 0), "TELEPORT", new Color(0.75f, 0.9f, 1f), 32f, 70f);
        }

        bool Free(Vector3 p, float r)
        {
            for (int k = 0; k < blocked.Count; k++)
            {
                Vector3 d = p - blocked[k]; d.y = 0;
                if (d.magnitude < r + blockedR[k]) return false;
            }
            return true;
        }

        /// <summary>Plaseaza un obiect decorativ pe un loc liber. solid = are coliziune.</summary>
        Transform Scatter(Vector3 o, string path, float size, float minR, bool solid, Transform root, float zMin = -66f, float zMax = 66f, float xAbs = 66f)
        {
            for (int tries = 0; tries < 12; tries++)
            {
                Vector3 p = o + new Vector3(R(-xAbs, xAbs), 0, R(zMin, zMax));
                if (!Free(p, minR)) continue;
                var t = ModelLib.Place(path, p, size, R(0, 360), root);
                if (t == null) return null;
                Block(p, minR);
                if (solid)
                {
                    if (path.Contains("snowman")) Obstacle.Add(t.gameObject, size * 0.28f, size);   // omul de zapada e lat
                    else AddCollider(t, size);
                }
                if (path.Contains("tree") || path.Contains("palm") || path.Contains("pine")) t.gameObject.AddComponent<Sway>();
                return t;
            }
            return null;
        }

        static void AddCollider(Transform t, float size)
        {
            Obstacle.Add(t.gameObject, Mathf.Max(0.3f, size * 0.16f), size);
        }

        /// <summary>Decor dens pe margini (padure/stanci).</summary>
        void Rim(Vector3 o, string[] paths, float minS, float maxS, Transform root, int count)
        {
            for (int k = 0; k < count; k++)
            {
                float a = k / (float)count * Mathf.PI * 2f;
                // punct pe perimetrul patratului
                float side = R(0, 4);
                Vector3 p;
                float t = R(-64, 64), inset = R(62f, 67f);
                if (side < 1) p = new Vector3(t, 0, inset);
                else if (side < 2)
                {
                    if (Mathf.Abs(t) < 42f) continue;   // sudul ramane liber (camera priveste de acolo)
                    if (t > 22f) continue;               // coltul teleportarilor ramane vizibil
                    p = new Vector3(t, 0, -inset);
                }
                else if (side < 3) p = new Vector3(inset, 0, t);
                else p = new Vector3(-inset, 0, t);
                var rt = ModelLib.Place(Pick(paths), o + p, R(minS, maxS), R(0, 360), root);
                if (rt != null && rng.NextDouble() < 0.5) rt.gameObject.AddComponent<Sway>();
            }
        }

        // ---------------------------------------------------------------
        //  Natura procedurala (copaci / tufe / stanci realiste)
        // ---------------------------------------------------------------
        TreeKind PickKind(TreeKind[] k) { return k[rng.Next(0, k.Length)]; }

        Transform ScatterTree(Vector3 o, TreeKind[] kinds, float minS, float maxS, float minR, Transform root, float zMin = -66f, float zMax = 66f, bool solid = true)
        {
            for (int tries = 0; tries < 12; tries++)
            {
                Vector3 p = o + new Vector3(R(-66, 66), 0, R(zMin, zMax));
                if (!Free(p, minR)) continue;
                float size = R(minS, maxS);
                var t = ProcNature.Place(PickKind(kinds), p, size, R(0, 360), root, rng.Next());
                Block(p, minR);
                if (solid)
                {
                    // coliziune doar pe trunchi (in unitatile locale ale copacului, care e scalat)
                    var c = t.gameObject.AddComponent<CapsuleCollider>();
                    c.radius = 0.45f; c.height = 5f; c.center = new Vector3(0, 2.5f, 0);
                }
                return t;
            }
            return null;
        }

        void RimTrees(Vector3 o, TreeKind[] kinds, float minS, float maxS, Transform root, int count)
        {
            for (int k = 0; k < count; k++)
            {
                float side = R(0, 4);
                Vector3 p;
                float t = R(-64, 64), inset = R(61f, 67f);
                if (side < 1) p = new Vector3(t, 0, inset);
                else if (side < 2)
                {
                    if (Mathf.Abs(t) < 42f) continue;
                    if (t > 22f) continue;
                    p = new Vector3(t, 0, -inset);
                }
                else if (side < 3) p = new Vector3(inset, 0, t);
                else p = new Vector3(-inset, 0, t);
                ProcNature.Place(PickKind(kinds), o + p, R(minS, maxS), R(0, 360), root, rng.Next());
            }
        }

        void ScatterRocks(Vector3 o, int count, float minS, float maxS, Color tint, bool snowy, Transform root, bool rim)
        {
            for (int k = 0; k < count; k++)
            {
                Vector3 p;
                if (rim)
                {
                    float a = R(0, Mathf.PI * 2f);
                    float side = R(0, 4), t = R(-64, 64), inset = R(60f, 67f);
                    if (side < 1) p = new Vector3(t, 0, inset);
                    else if (side < 2) { if (Mathf.Abs(t) < 42f || t > 22f) continue; p = new Vector3(t, 0, -inset); }
                    else if (side < 3) p = new Vector3(inset, 0, t);
                    else p = new Vector3(-inset, 0, t);
                }
                else
                {
                    p = new Vector3(R(-64, 64), 0, R(-24, 64));
                    if (!Free(o + p, maxS * 0.6f)) continue;
                    Block(o + p, maxS * 0.6f);
                }
                float size = R(minS, maxS);
                Color c = Color.Lerp(tint, tint * 0.8f, R(0f, 1f));
                var rt = ProcNature.Rock(o + p - Vector3.up * size * 0.08f, size, R(0, 360), c, root, rng.Next(), snowy);
                if (!rim && size > 1.2f)
                {
                    var col = rt.gameObject.AddComponent<SphereCollider>();
                    col.radius = 0.45f;
                    col.center = new Vector3(0, 0.35f, 0);
                }
            }
        }

        // ---------------------------------------------------------------
        //  Decor per harta
        // ---------------------------------------------------------------
        void DecorMeadow(Vector3 o, Transform root)
        {
            string N = GameData.NAT;
            string[] trees = { N + "tree_default", N + "tree_oak", N + "tree_fat", N + "tree_detailed", N + "tree_blocks", N + "tree_cone", N + "tree_simple", N + "tree_small", N + "tree_default_fall", N + "tree_oak_fall", N + "tree_plateau", N + "tree_tall" };
            string[] flowers = { N + "flower_redA", N + "flower_yellowA", N + "flower_purpleA", N + "flower_redB", N + "flower_yellowB", N + "flower_purpleB" };
            string[] small = { N + "plant_bush", N + "plant_bushLarge", N + "plant_bushSmall", N + "mushroom_red", N + "mushroom_redGroup", N + "mushroom_tanGroup", N + "grass", N + "grass_large", N + "stump_round", N + "log" };
            string[] rocks = { N + "rock_largeA", N + "rock_largeB", N + "rock_tallA", N + "stone_largeA" };

            bool anyModel = ModelLib.Exists(trees[0]);
            if (!anyModel) { FallbackTrees(o, root, false); return; }

            if (ProcNature.Available)
            {
                // padure realista: stejari, mesteceni, cativa copaci de toamna si brazi pe margini
                var forest = new[] { TreeKind.Oak, TreeKind.Oak, TreeKind.Oak, TreeKind.Birch, TreeKind.OakAutumn, TreeKind.Pine };
                RimTrees(o, forest, 7f, 11f, root, 110);
                for (int k = 0; k < 40; k++) ScatterTree(o, new[] { TreeKind.Oak, TreeKind.Oak, TreeKind.Birch, TreeKind.OakAutumn }, 6f, 9.5f, 3f, root, -24f, 64f);
                for (int k = 0; k < 70; k++) ScatterTree(o, new[] { TreeKind.Bush }, 1f, 1.9f, 1.3f, root, -30f, 64f, false);
                ScatterRocks(o, 14, 1.2f, 3.2f, new Color(0.52f, 0.52f, 0.5f), false, root, false);
                ScatterRocks(o, 30, 2f, 5f, new Color(0.5f, 0.5f, 0.48f), false, root, true);
                small = new[] { N + "mushroom_red", N + "mushroom_redGroup", N + "mushroom_tanGroup", N + "stump_round", N + "log" };
            }
            else
            {
                Rim(o, trees, 5f, 8f, root, 90);
                for (int k = 0; k < 40; k++) Scatter(o, Pick(trees), R(4.5f, 7.5f), 3f, true, root, -24f, 64f);
                for (int k = 0; k < 10; k++) Scatter(o, Pick(rocks), R(1.5f, 3f), 2.5f, true, root, -24f, 64f);
            }
            for (int k = 0; k < 90; k++) Scatter(o, Pick(flowers), R(0.6f, 1.1f), 0.8f, false, root);
            for (int k = 0; k < 40; k++) Scatter(o, Pick(small), R(0.8f, 1.8f), 1.2f, false, root);

            // detalii langa zona de start
            ModelLib.Place(GameData.TOWN + "windmill", o + new Vector3(-52, 0, 4), 14f, 60f, root);   // departe de clasamente
            Block(o + new Vector3(-52, 0, 4), 7f);
        }

        void DecorDesert(Vector3 o, Transform root)
        {
            string N = GameData.NAT;
            string[] cactus = { N + "cactus_short", N + "cactus_tall" };
            string[] palms = { N + "tree_palm", N + "tree_palmTall", N + "tree_palmBend" };
            string[] rocks = { N + "rock_largeA", N + "rock_largeB", N + "rock_largeC", N + "rock_tallA", N + "rock_tallB", N + "rock_smallA", N + "rock_smallC" };
            string G = GameData.GRAVE;

            // piramide colorate (pastrate din versiunea originala)
            Vector3[] pyr = { new Vector3(-40, 0, 40), new Vector3(38, 0, 22), new Vector3(-42, 0, -8) };
            foreach (var pp in pyr)
            {
                for (int t = 0; t < 6; t++)
                {
                    float size = 16f - t * 2.6f;
                    Color pc = (t % 2 == 0) ? new Color(1f, 0.75f, 0.25f) : new Color(1f, 0.62f, 0.2f);
                    Art.Prim(PrimitiveType.Cube, o + pp + Vector3.up * (t * 1.5f + 0.75f), new Vector3(size, 1.5f, size), pc, root);
                }
                Block(o + pp, 11f);
            }

            if (!ModelLib.Exists(cactus[0])) { FallbackTrees(o, root, true); return; }

            if (ProcNature.Available)
            {
                Color sandstone = new Color(0.8f, 0.6f, 0.4f);
                ScatterRocks(o, 60, 2.5f, 6.5f, sandstone, false, root, true);
                RimTrees(o, new[] { TreeKind.Palm }, 7f, 10f, root, 25);
                for (int k = 0; k < 36; k++) ScatterTree(o, new[] { TreeKind.Cactus }, 2.2f, 4.2f, 2.5f, root, -24f, 64f);
                for (int k = 0; k < 14; k++) ScatterTree(o, new[] { TreeKind.Palm }, 6f, 9f, 3f, root, -24f, 64f);
                for (int k = 0; k < 8; k++) ScatterTree(o, new[] { TreeKind.Dead }, 3f, 4.5f, 2.5f, root, -24f, 64f);
                ScatterRocks(o, 24, 1f, 3.5f, sandstone, false, root, false);
            }
            else
            {
                Rim(o, rocks, 3f, 6f, root, 60);
                Rim(o, palms, 6f, 9f, root, 25);
                for (int k = 0; k < 36; k++) Scatter(o, Pick(cactus), R(2f, 4.2f), 2.5f, true, root, -24f, 64f);
                for (int k = 0; k < 14; k++) Scatter(o, Pick(palms), R(5f, 8f), 3f, true, root, -24f, 64f);
                for (int k = 0; k < 22; k++) Scatter(o, Pick(rocks), R(1f, 3.5f), 2f, true, root, -24f, 64f);
            }
            for (int k = 0; k < 6; k++) Scatter(o, G + "pillar-obelisk", R(4f, 6f), 3f, true, root, -24f, 64f);
            for (int k = 0; k < 5; k++) Scatter(o, G + "crypt-small", R(3.5f, 4.5f), 4f, true, root, -24f, 64f);
            for (int k = 0; k < 10; k++) Scatter(o, G + "gravestone-broken", R(0.9f, 1.4f), 1f, false, root);
            ModelLib.Place(GameData.TOWN + "stall-red", o + new Vector3(24, 0, -56), 4f, 200f, root);
            ModelLib.Place(GameData.TOWN + "cart", o + new Vector3(-24, 0, -56), 2.6f, 20f, root);
        }

        void DecorFrozen(Vector3 o, Transform root)
        {
            string H = GameData.HOL, N = GameData.NAT;
            string[] trees = { H + "tree-snow-a", H + "tree-snow-b", H + "tree-snow-c", N + "tree_pineDefaultA", N + "tree_pineRoundB", N + "tree_pineTallA" };
            string[] rocks = { H + "rocks-large", H + "rocks-medium", H + "rocks-small", H + "snow-pile" };
            string[] fun = { H + "present-a-cube", H + "present-b-round", H + "candy-cane-red", H + "candy-cane-green", H + "gingerbread-man" };

            // cristale de gheata stralucitoare (primitive)
            for (int k = 0; k < 0; k++)   // scos: cuburi/cristale fara rost pe harta
            {
                Vector3 p = o + new Vector3(R(-58, 58), 0, R(-24, 60));
                if (!Free(p, 2f)) continue;
                Block(p, 2f);
                float h = R(2f, 6f);
                Color cc = Color.HSVToRGB(Mathf.Floor(R(5f, 7f)) / 10f + 0.02f, 0.45f, 1f);
                var cr = Art.Prim(PrimitiveType.Cube, p + Vector3.up * (h * 0.5f), new Vector3(R(0.8f, 1.6f), h, R(0.8f, 1.6f)), cc, root, true, false);
                cr.transform.rotation = Quaternion.Euler(R(-12, 12), R(0, 90), R(-12, 12));
                Art.SetEmission(cr.GetComponent<Renderer>().sharedMaterial, cc * 0.35f);
            }

            if (!ModelLib.Exists(trees[0])) { for (int k = 0; k < 8; k++) Snowman(o + new Vector3(R(-55, 55), 0, R(-25, 58)), root); return; }

            if (ProcNature.Available)
            {
                RimTrees(o, new[] { TreeKind.SnowPine, TreeKind.SnowPine, TreeKind.Pine }, 8f, 12f, root, 110);
                for (int k = 0; k < 34; k++) ScatterTree(o, new[] { TreeKind.SnowPine, TreeKind.SnowPine, TreeKind.Pine }, 6f, 10f, 3f, root, -24f, 64f);
                ScatterRocks(o, 20, 1f, 3f, new Color(0.55f, 0.58f, 0.64f), true, root, false);
                ScatterRocks(o, 26, 2.5f, 5.5f, new Color(0.5f, 0.53f, 0.6f), true, root, true);
            }
            else
            {
                Rim(o, trees, 6f, 9f, root, 90);
                for (int k = 0; k < 34; k++) Scatter(o, Pick(trees), R(5f, 8f), 3f, true, root, -24f, 64f);
                for (int k = 0; k < 20; k++) Scatter(o, Pick(rocks), R(1f, 2.5f), 2f, true, root, -24f, 64f);
            }
            for (int k = 0; k < 8; k++) Scatter(o, H + "snowman", R(2.5f, 3.5f), 2f, true, root, -24f, 64f);
            for (int k = 0; k < 18; k++) Scatter(o, Pick(fun), R(0.8f, 1.6f), 1.2f, false, root);
            ModelLib.Place(H + "reindeer", o + new Vector3(-24, 0, -55), 2.4f, 40f, root);
            ModelLib.Place(H + "sled", o + new Vector3(-21, 0, -57), 1.6f, 40f, root);
            ModelLib.Place(H + "snowman-hat", o + new Vector3(24, 0, -56), 3f, 200f, root);
            ModelLib.Place(H + "lantern", o + new Vector3(6, 0, -30), 2.2f, 0f, root);
            ModelLib.Place(H + "lantern", o + new Vector3(-6, 0, -30), 2.2f, 0f, root);
        }

        /// <summary>Harta 4: vulcan - sol de bazalt, rauri de lava, conuri de vulcan, stanci si focuri.</summary>
        void DecorLava(Vector3 o, Transform root)
        {
            string N = GameData.NAT, G = GameData.GRAVE;
            Color lava = new Color(1f, 0.42f, 0.08f), basalt = new Color(0.22f, 0.17f, 0.17f), rock = new Color(0.32f, 0.24f, 0.22f);

            // doi vulcani mari (conuri din cilindri) cu crater de lava
            Vector3[] volc = { new Vector3(-44, 0, 40), new Vector3(46, 0, 30) };
            foreach (var vp in volc)
            {
                for (int t = 0; t < 7; t++)
                {
                    float r = 18f - t * 2.2f;
                    Art.Prim(PrimitiveType.Cylinder, o + vp + Vector3.up * (t * 1.6f + 0.8f), new Vector3(r, 0.8f, r), t % 2 == 0 ? basalt : rock, root);
                }
                var crater = Art.Prim(PrimitiveType.Cylinder, o + vp + Vector3.up * 11.3f, new Vector3(4.5f, 0.1f, 4.5f), lava, root, false, false);
                Art.SetEmission(crater.GetComponent<Renderer>().sharedMaterial, lava * 2.2f);
                var fx = Anchor("VolcanoFx", o + vp + Vector3.up * 11.6f, root).gameObject.AddComponent<FireFx>();
                fx.col = lava; fx.rate = 16f;
                Block(o + vp, 11f);
            }

            // rauri si balti de lava (stralucesc)
            for (int k = 0; k < 16; k++)
            {
                Vector3 p = o + new Vector3(R(-58, 58), 0, R(-20, 60));
                if (!Free(p, 4f)) continue;
                Block(p, 4f);
                float w = R(3f, 7f);
                var pool = Art.Prim(PrimitiveType.Cylinder, p + Vector3.up * 0.03f, new Vector3(w, 0.02f, w * R(0.5f, 1f)), lava, root, false, false);
                pool.transform.rotation = Quaternion.Euler(0, R(0, 180), 0);
                Art.SetEmission(pool.GetComponent<Renderer>().sharedMaterial, lava * 1.6f);
                pool.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var rim = Art.Prim(PrimitiveType.Cylinder, p + Vector3.up * 0.02f, new Vector3(w + 1.2f, 0.02f, w * 0.9f + 1.2f), basalt, root, false);
                rim.transform.rotation = pool.transform.rotation;
                if (k % 3 == 0) Anchor("LavaFx", p + Vector3.up * 0.3f, root).gameObject.AddComponent<FireFx>().col = new Color(1f, 0.5f, 0.1f);
            }

            // cristale de obsidian (primitive)
            for (int k = 0; k < 0; k++)   // scos: cuburi/cristale fara rost pe harta
            {
                Vector3 p = o + new Vector3(R(-58, 58), 0, R(-24, 60));
                if (!Free(p, 2f)) continue;
                Block(p, 2f);
                float h = R(2f, 5.5f);
                var cr = Art.Prim(PrimitiveType.Cube, p + Vector3.up * (h * 0.5f), new Vector3(R(0.8f, 1.5f), h, R(0.8f, 1.5f)), new Color(0.2f, 0.12f, 0.22f), root, true, false);
                cr.transform.rotation = Quaternion.Euler(R(-12, 12), R(0, 90), R(-12, 12));
                Art.SetEmission(cr.GetComponent<Renderer>().sharedMaterial, new Color(0.6f, 0.15f, 0.05f) * 0.5f);
            }

            string[] rocks = { N + "rock_largeA", N + "rock_largeB", N + "rock_largeC", N + "rock_tallA", N + "rock_tallB", N + "stone_largeA" };
            if (ProcNature.Available)
            {
                Color bas = new Color(0.2f, 0.16f, 0.15f);
                ScatterRocks(o, 90, 3f, 8f, bas, false, root, true);
                ScatterRocks(o, 30, 1.5f, 4.5f, bas, false, root, false);
                for (int k = 0; k < 22; k++) ScatterTree(o, new[] { TreeKind.Dead }, 4f, 6.5f, 2.5f, root, -24f, 64f);
            }
            else
            {
                if (!ModelLib.Exists(rocks[0])) return;
                Rim(o, rocks, 4f, 8f, root, 90);
                for (int k = 0; k < 30; k++) Scatter(o, Pick(rocks), R(1.5f, 4.5f), 2.5f, true, root, -24f, 64f);
            }
            for (int k = 0; k < 8; k++) Scatter(o, G + "pillar-obelisk", R(4f, 6f), 3f, true, root, -24f, 64f);
            for (int k = 0; k < 6; k++) Scatter(o, G + "crypt-small", R(3.5f, 4.5f), 4f, true, root, -24f, 64f);
            for (int k = 0; k < 14; k++) Scatter(o, G + "gravestone-broken", R(0.9f, 1.4f), 1f, false, root);
        }

        // ---------------------------------------------------------------
        //  LUMEA NOUA (Stylized Nature MegaKit): acelasi stil pe toate hartile,
        //  mai putine obiecte, margine naturala (stanci + copaci), poteci de piatra
        // ---------------------------------------------------------------
        static readonly Vector4[] FightZones =
        {
            new Vector4(-54f, -20f, -20f, 30f), new Vector4(20f, 54f, -20f, 30f), new Vector4(-17f, 17f, 6f, 30f),
        };

        static bool InFightZone(Vector3 local, float pad)
        {
            foreach (var z in FightZones)
                if (local.x > z.x - pad && local.x < z.y + pad && local.z > z.z - pad && local.z < z.w + pad) return true;
            return false;
        }

        /// <summary>Pune un model din pachetul de natura pe un loc liber (local = fata de originea hartii).</summary>
        Transform NatureScatter(Vector3 o, string name, float minS, float maxS, float minR, bool solid, Transform root, NatureKit.Theme th,
                                bool avoidZones, bool block = true, bool byWidth = false, float zMin = -26f, float zMax = 62f, float xAbs = 60f, bool shadows = true)
        {
            for (int tries = 0; tries < 14; tries++)
            {
                Vector3 lp = new Vector3(R(-xAbs, xAbs), 0, R(zMin, zMax));
                if (avoidZones && InFightZone(lp, 2f)) continue;
                Vector3 p = o + lp;
                float size = R(minS, maxS);
                float need = byWidth && solid ? Mathf.Max(minR, size * 0.6f + 0.8f) : minR;   // stancile mari cer mai mult loc liber
                if (!Free(p, need)) continue;
                var t = NatureKit.Place(name, p, size, R(0, 360), root, th, byWidth);
                if (t == null) return null;
                if (block) Block(p, need);
                if (!shadows) foreach (var r in t.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (solid) Obstacle.Add(t.gameObject, byWidth ? size * 0.4f : Mathf.Clamp(size * 0.07f, 0.45f, 0.8f), byWidth ? size * 0.8f : size * 0.6f);
                return t;
            }
            return null;
        }

        /// <summary>Marginea hartii: stanci mari pe tot conturul + un rand de copaci in spate (lumea pare ca merge mai departe).
        /// Peretii reali sunt invizibili (doar coliziune).</summary>
        void NatureBorder(int i, Vector3 o, Transform root)
        {
            var th = NatureKit.For(i);
            float W = 69f;
            // pereti invizibili (nu iesi din harta)
            Vector3[] wp = { new Vector3(0, 3, W + 0.5f), new Vector3(0, 3, -W - 0.5f), new Vector3(W + 0.5f, 3, 0), new Vector3(-W - 0.5f, 3, 0) };
            Vector3[] ws = { new Vector3(142, 6, 1), new Vector3(142, 6, 1), new Vector3(1, 6, 142), new Vector3(1, 6, 142) };
            for (int k = 0; k < 4; k++)
            {
                var w = new GameObject("Wall (invisible)");
                w.transform.SetParent(root, false);
                w.transform.position = o + wp[k];
                w.AddComponent<BoxCollider>().size = ws[k];
            }
            string[] trees = i == 1 ? new[] { "DeadTree_1", "DeadTree_2", "DeadTree_3", "TwistedTree_1", "TwistedTree_3" }
                           : i == 2 ? NatureKit.Pines
                           : i == 3 ? NatureKit.Dead
                           : new[] { "CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5", "TwistedTree_2", "Pine_2" };
            for (int side = 0; side < 4; side++)
            {
                // rar: muntii din spate inchid harta, aici doar cateva stanci / copaci (nu un zid de obiecte)
                for (float t = -68f + R(0f, 8f); t <= 68f; t += R(16f, 24f))
                {
                    bool south = side == 1;
                    bool low = south && Mathf.Abs(t) < 44f;            // in spatele spawn-ului: stanci joase (camera priveste peste ele)
                    if (south && t > 22f && t < 44f) low = true;       // coltul teleportarilor ramane vizibil
                    float size = low ? R(3f, 4.5f) : R(5f, 9f);
                    // asezare neregulata: mai in fata / mai in spate si putin intr-o parte (nu in linie dreapta)
                    float inset = 70f - size * 0.62f - R(0f, low ? 3f : 10f);
                    float tj = t + R(-4f, 4f);
                    Vector3 lp = side == 0 ? new Vector3(tj, 0, inset) : side == 1 ? new Vector3(tj, 0, -inset) : side == 2 ? new Vector3(inset, 0, tj) : new Vector3(-inset, 0, tj);
                    if (!Free(o + lp, size * 0.6f + 1.5f)) continue;
                    float ry = (side == 0 || side == 1 ? 0f : 90f) + rng.Next(2) * 180f + R(-10f, 10f);
                    var rk = NatureKit.Place(Pick(NatureKit.Rocks), o + lp - Vector3.up * size * 0.12f, size, ry + R(0f, 360f), root, th, true);
                    if (rk != null)
                    {
                        rk.localScale = new Vector3(1f, low ? R(0.35f, 0.5f) : R(0.55f, 1.05f), 1f);
                        Obstacle.Add(rk.gameObject, size * 0.42f, size);
                        Block(o + lp, size * 0.6f + 1f);
                    }
                    // copaci in spatele stancilor (nu si in spatele spawn-ului)
                    if (!low && rng.NextDouble() < 0.3)
                    {
                        float ti = R(52f, 63f), tt = t + R(-6f, 6f);
                        Vector3 tp = side == 0 ? new Vector3(tt, 0, ti) : side == 1 ? new Vector3(tt, 0, -ti) : side == 2 ? new Vector3(ti, 0, tt) : new Vector3(-ti, 0, tt);
                        if (i == 0 && side == 1 && Mathf.Abs(t) > 22f) continue;   // hub: colturile cu portaluri / clasamente
                        if (!Free(o + tp, 4.5f)) continue;
                        var tr = NatureKit.Place(Pick(trees), o + tp, R(9f, 14f), R(0, 360), root, th);
                        if (tr != null) { Obstacle.Add(tr.gameObject, 0.7f, 6f); Block(o + tp, 4.5f); }
                    }
                }
            }
        }

        /// <summary>Decorul unei harti (acelasi pachet, alte nuante): putini copaci si stanci, smocuri de iarba si flori, repere ale hartii.</summary>
        void NatureDecor(int i, Vector3 o, Transform root, Transform details)
        {
            var th = NatureKit.For(i);
            details.gameObject.AddComponent<DetailGroup>();
            // 1) detalii mici (nu blocheaza nimic): iarba, flori, pietricele
            int grass = i == 0 ? 70 : i == 1 ? 40 : i == 2 ? 16 : 10;
            string[] gset = i == 0 ? NatureKit.Grass : NatureKit.DryGrass;
            for (int k = 0; k < grass; k++) NatureScatter(o, Pick(gset), 0.6f, 1.3f, 0.6f, false, details, th, false, false, false, -30f, 64f, 64f, false);
            if (i == 0)
            {
                for (int k = 0; k < 32; k++) NatureScatter(o, Pick(NatureKit.Flowers), 0.5f, 1.1f, 0.6f, false, details, th, false, false, false, -30f, 64f, 64f, false);
                for (int k = 0; k < 8; k++) NatureScatter(o, Pick(NatureKit.Mushrooms), 0.5f, 0.9f, 0.6f, false, details, th, true, false);
            }
            for (int k = 0; k < 14; k++) NatureScatter(o, Pick(NatureKit.Pebbles), 0.4f, 0.9f, 0.6f, false, details, th, false, false, true, -30f, 64f, 64f, false);

            // 2) repere ale hartii
            if (i == 0) { }   // (moara de vant scoasa - aveam doar elicea)
            else if (i == 1)
            {
                var sand = Art.PaintedTex("Ground1");
                Vector3[] pyr = { new Vector3(-40, 0, 40), new Vector3(38, 0, 22), new Vector3(-42, 0, -8) };
                foreach (var pp in pyr)
                {
                    for (int t = 0; t < 6; t++)
                    {
                        float size = 16f - t * 2.6f;
                        var st = Art.Prim(PrimitiveType.Cube, o + pp + Vector3.up * (t * 1.5f + 0.75f), new Vector3(size, 1.5f, size), (t % 2 == 0) ? new Color(1f, 0.9f, 0.7f) : new Color(0.92f, 0.8f, 0.6f), root, true, false);
                        if (sand != null) Art.ApplyTexture(st.GetComponent<Renderer>().sharedMaterial, sand, size / 8f);
                    }
                    Block(o + pp, 11f);
                    Obstacle.Marker(root, o + pp, 8.2f);
                }
            }
            else if (i == 2)
            {
                string H = GameData.HOL;
                for (int k = 0; k < 3; k++) Scatter(o, H + "snowman", R(2.5f, 3.5f), 2f, true, root, -24f, 60f, 56f);
                var rd = ModelLib.Place(H + "reindeer", o + new Vector3(-24, 0, -55), 2.4f, 40f, root);
                if (rd != null) Obstacle.Add(rd.gameObject, 0.7f, 2.4f);
                var sl = ModelLib.Place(H + "sled", o + new Vector3(-21, 0, -57), 1.6f, 40f, root);
                if (sl != null) Obstacle.Add(sl.gameObject, 0.9f, 1.2f);
                Block(o + new Vector3(-22.5f, 0, -56), 3f);
            }
            else
            {
                Color lava = new Color(1f, 0.42f, 0.08f);
                var rockTex = Art.PaintedTex("Ground3");
                Vector3[] volc = { new Vector3(-44, 0, 40), new Vector3(46, 0, 30) };
                foreach (var vp in volc)
                {
                    for (int t = 0; t < 7; t++)
                    {
                        float r = 18f - t * 2.2f;
                        var cy = Art.Prim(PrimitiveType.Cylinder, o + vp + Vector3.up * (t * 1.6f + 0.8f), new Vector3(r, 0.8f, r), t % 2 == 0 ? new Color(0.62f, 0.55f, 0.55f) : new Color(0.75f, 0.66f, 0.64f), root, true, false);
                        if (rockTex != null) Art.ApplyTexture(cy.GetComponent<Renderer>().sharedMaterial, rockTex, r / 6f);
                    }
                    var crater = Art.Prim(PrimitiveType.Cylinder, o + vp + Vector3.up * 11.3f, new Vector3(4.5f, 0.1f, 4.5f), lava, root, false, false);
                    Art.SetEmission(crater.GetComponent<Renderer>().sharedMaterial, lava * 2.2f);
                    var fx = Anchor("VolcanoFx", o + vp + Vector3.up * 11.6f, root).gameObject.AddComponent<FireFx>();
                    fx.col = lava; fx.rate = 16f;
                    Block(o + vp, 11f);
                    Obstacle.Marker(root, o + vp, 9.2f);
                }
                for (int k = 0; k < 8; k++)
                {
                    Vector3 lp = new Vector3(R(-56, 56), 0, R(-20, 58));
                    if (InFightZone(lp, 0f)) continue;
                    Vector3 p = o + lp;
                    if (!Free(p, 4f)) continue;
                    Block(p, 4f);
                    float w = R(3f, 6f);
                    var pool = Art.Prim(PrimitiveType.Cylinder, p + Vector3.up * 0.03f, new Vector3(w, 0.02f, w * R(0.5f, 1f)), lava, root, false, false);
                    pool.transform.rotation = Quaternion.Euler(0, R(0, 180), 0);
                    Art.SetEmission(pool.GetComponent<Renderer>().sharedMaterial, lava * 1.6f);
                    pool.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    if (k % 3 == 0) Anchor("LavaFx", p + Vector3.up * 0.3f, root).gameObject.AddComponent<FireFx>().col = new Color(1f, 0.5f, 0.1f);
                }
            }

            // 3) copaci (in afara zonelor de lupta, ca monstrii sa aiba loc), tufe, stanci
            string[] trees = i == 1 ? new[] { "DeadTree_1", "DeadTree_3", "DeadTree_5", "TwistedTree_1", "TwistedTree_4" }
                           : i == 2 ? NatureKit.Pines
                           : i == 3 ? NatureKit.Dead
                           : new[] { "CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5", "TwistedTree_2", "TwistedTree_5" };
            int treeN = i == 0 ? 18 : i == 2 ? 16 : i == 1 ? 12 : 13;
            for (int k = 0; k < treeN; k++) NatureScatter(o, Pick(trees), 7f, 11.5f, 4.8f, true, root, th, true);   // copacii mai rari (min. ~9 m intre ei)
            int bushN = i == 0 ? 14 : i == 1 ? 4 : i == 2 ? 4 : 0;
            for (int k = 0; k < bushN; k++) NatureScatter(o, Pick(NatureKit.Bushes), 1.4f, 2.4f, 2.4f, false, root, th, true);
            for (int k = 0; k < 14; k++) NatureScatter(o, Pick(NatureKit.Rocks), 2.2f, 4.8f, 2.6f, true, root, th, true, true, true);
            if (i == 1)
                for (int k = 0; k < 12; k++)
                    for (int tries = 0; tries < 12; tries++)
                    {
                        Vector3 lp = new Vector3(R(-56f, 56f), 0, R(-24f, 60f));
                        if (InFightZone(lp, 1.5f) || !Free(o + lp, 2.2f)) continue;
                        Block(o + lp, 1.8f);
                        Obstacle.Pillar(root, o + lp, 0.85f);
                        BuildCactus(o + lp, k % 3 == 0 ? R(1.6f, 2.2f) : R(2.8f, 4.2f), root);
                        break;
                    }
        }

        void FallbackTrees(Vector3 o, Transform root, bool desert)
        {
            for (int k = 0; k < 38; k++)
            {
                Vector3 p = o + new Vector3(R(-58, 58), 0, R(-24, 60));
                if (!Free(p, 2.5f)) continue;
                Block(p, 2.5f);
                float h = R(1.2f, 2.2f);
                if (desert)
                {
                    Color cactus = new Color(0.25f, 0.75f, 0.3f);
                    Art.Prim(PrimitiveType.Capsule, p + Vector3.up * h, new Vector3(0.9f, h, 0.9f), cactus, root);
                    Art.Prim(PrimitiveType.Capsule, p + new Vector3(0.9f, h * 1.1f, 0), new Vector3(0.5f, 0.6f, 0.5f), cactus, root, false);
                }
                else
                {
                    Art.Prim(PrimitiveType.Cylinder, p + Vector3.up * h, new Vector3(0.7f, h, 0.7f), new Color(0.45f, 0.3f, 0.15f), root);
                    Color leaf = Color.HSVToRGB(Mathf.Floor(R(2f, 3.4f)) / 10f + 0.05f, 0.65f, 0.85f);
                    Art.Prim(PrimitiveType.Sphere, p + Vector3.up * (h * 2f + 1.2f), Vector3.one * R(3f, 4.5f), leaf, root, false);
                }
            }
        }

        void Snowman(Vector3 p, Transform root)
        {
            Art.Prim(PrimitiveType.Sphere, p + Vector3.up * 1.2f, Vector3.one * 2.4f, Color.white, root);
            Art.Prim(PrimitiveType.Sphere, p + Vector3.up * 3.0f, Vector3.one * 1.7f, Color.white, root, false);
            Art.Prim(PrimitiveType.Sphere, p + Vector3.up * 4.3f, Vector3.one * 1.1f, Color.white, root, false);
            Art.Prim(PrimitiveType.Cube, p + new Vector3(0, 4.3f, 0.7f), new Vector3(0.2f, 0.2f, 0.7f), new Color(1f, 0.5f, 0.1f), root, false);
        }

        // ---------------------------------------------------------------
        //  Arena boss-ului
        // ---------------------------------------------------------------
        void BuildBossArena(int i, Vector3 c, Transform root)
        {
            var m = GameData.Maps[i];
            var ring = Art.Prim(PrimitiveType.Cylinder, c + Vector3.up * 0.015f, new Vector3(26, 0.015f, 26), Color.Lerp(m.border, Color.white, 0.25f), root, false);
            ring.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var inner = Art.Prim(PrimitiveType.Cylinder, c + Vector3.up * 0.02f, new Vector3(22, 0.015f, 22), Color.Lerp(m.ground, m.border, 0.35f), root, false);
            inner.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var gtx = nature ? Art.PaintedTex("Ground" + Mathf.Clamp(i, 0, 3)) : null;
            if (gtx != null)
            {
                // podeaua arenei: aceeasi textura ca solul, doar putin mai inchisa/deschisa (nu discuri de culoare plata)
                var rmat = new Material(ring.GetComponent<Renderer>().sharedMaterial); Art.SetColor(rmat, new Color(0.78f, 0.76f, 0.74f)); Art.ApplyTexture(rmat, gtx, 2.4f); ring.GetComponent<Renderer>().sharedMaterial = rmat;
                var imat = new Material(inner.GetComponent<Renderer>().sharedMaterial); Art.SetColor(imat, new Color(1.08f, 1.06f, 1.02f)); Art.ApplyTexture(imat, gtx, 2f); inner.GetComponent<Renderer>().sharedMaterial = imat;
            }
            Block(c, 15f);
            // cerc de stanci inalte (menhire), nuantate dupa harta; intrarea (spre spawn) ramane libera
            var th = NatureKit.For(i);
            int n = 11;
            for (int k = 0; k < n; k++)
            {
                float a = k / (float)n * Mathf.PI * 2f + 0.15f;
                if (Mathf.Abs(Mathf.Cos(a)) < 0.34f && Mathf.Sin(a) < 0) continue; // intrarea
                Vector3 p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * R(13.6f, 14.6f);
                float hgt = k % 2 == 0 ? R(5f, 6.2f) : R(3.4f, 4.4f);
                Transform t = nature ? NatureKit.Place(NatureKit.Rocks[rng.Next(NatureKit.Rocks.Length)], p - Vector3.up * 0.3f, hgt, R(0, 360), root, th) : null;
                if (t != null)
                {
                    t.localScale = new Vector3(0.45f, 1f, 0.45f);
                    AddCollider(t, hgt);
                    if (k % 2 == 1)
                    {
                        float ss = R(1.6f, 2.2f);
                        var sr = NatureKit.Place(NatureKit.Rocks[rng.Next(NatureKit.Rocks.Length)], p + new Vector3(R(-1f, 1f), 0, R(-1f, 1f)), ss, R(0, 360), root, th, true);
                        if (sr != null) Obstacle.Add(sr.gameObject, ss * 0.42f, ss);
                    }
                }
                else if (!nature)
                {
                    var t2 = ModelLib.Place(GameData.ARENA + "column", p, 5f, a * Mathf.Rad2Deg, root);
                    if (t2 == null) Art.Prim(PrimitiveType.Cylinder, p + Vector3.up * 2.5f, new Vector3(1.2f, 2.5f, 1.2f), m.border, root);
                    else AddCollider(t2, 5f);
                }
            }
            WorldLabel.Create(root, c + Vector3.up * 1f, "BOSS: " + m.boss.name, new Color(1f, 0.45f, 0.4f), 34f);
        }

        // ---------------------------------------------------------------
        //  Cladiri interactive
        // ---------------------------------------------------------------
        Transform Anchor(string n, Vector3 p, Transform parent)
        {
            var t = new GameObject(n).transform;
            t.SetParent(parent, false);
            t.position = p;
            return t;
        }

        /// <summary>NPC-ul cu questuri de la spawn (2 questuri cu monstri + 1 cu boss-ul hartii).</summary>
        void BuildQuestGiver(Vector3 p, Transform mapRoot, int map, float yaw = 165f)
        {
            var root = Anchor("Quest Giver", p, mapRoot);
            root.rotation = Quaternion.Euler(0, yaw, 0);   // priveste spre spawn
            // podium mic
            var qpod = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.12f, 0), new Vector3(2.6f, 0.12f, 2.6f), new Color(0.62f, 0.5f, 0.36f), root, true, false);
            var qtex = nature ? Art.PaintedTex("Cobble") : null;
            if (qtex != null) { Art.SetColor(qpod.GetComponent<Renderer>().sharedMaterial, CobCol(map) * 0.9f); Art.ApplyTexture(qpod.GetComponent<Renderer>().sharedMaterial, qtex, 0.9f); }
            var npc = Anchor("NPC", p + Vector3.up * 0.24f, root);
            npc.localRotation = Quaternion.identity;
            var tr = npc.gameObject.AddComponent<NpcTrader>();
            tr.Init(GameData.CHAR + GameData.Heroes[(map * 3 + 5) % GameData.Heroes.Length]);
            // semnul "!" auriu care pluteste deasupra
            var mark = Anchor("QuestMark", p + Vector3.up * 3.3f, root);
            Color gold = new Color(1f, 0.82f, 0.2f);
            var bar = Art.Prim(PrimitiveType.Cube, new Vector3(0, 0.35f, 0), new Vector3(0.22f, 0.6f, 0.22f), gold, mark, false, false);
            var dot = Art.Prim(PrimitiveType.Cube, new Vector3(0, -0.15f, 0), new Vector3(0.22f, 0.2f, 0.22f), gold, mark, false, false);
            Art.SetEmission(bar.GetComponent<Renderer>().sharedMaterial, gold * 0.9f);
            Art.SetEmission(dot.GetComponent<Renderer>().sharedMaterial, gold * 0.9f);
            var sp = mark.gameObject.AddComponent<Spinner>();
            sp.rot = new Vector3(0, 90f, 0); sp.bobAmp = 0.15f; sp.bobSpeed = 2.5f;
            // panou cu questuri
            var board = Art.Prim(PrimitiveType.Cube, new Vector3(1.6f, 1.1f, 0.4f), new Vector3(1.4f, 1f, 0.12f), new Color(0.55f, 0.38f, 0.22f), root, false);
            Art.Prim(PrimitiveType.Cube, new Vector3(1.6f, 1.1f, 0.33f), new Vector3(1.2f, 0.8f, 0.02f), new Color(0.95f, 0.9f, 0.75f), root, false);
            Art.Prim(PrimitiveType.Cylinder, new Vector3(1.6f, 0.35f, 0.4f), new Vector3(0.12f, 0.35f, 0.12f), new Color(0.45f, 0.3f, 0.18f), root, false);
            // zona din jurul NPC-ului ramane curata: fara copaci / tufisuri / pietre si fara iarba inalta
            Block(p, 6f);
            if (nature && Art.PaintedTex("Cobble") != null) StoneDisc(p, 2.7f, mapRoot);
            else
            {
            var dirt = Art.Prim(PrimitiveType.Cylinder, p + Vector3.up * 0.012f, new Vector3(5.2f, 0.01f, 5.2f), Color.Lerp(GameData.Maps[map].ground, new Color(0.55f, 0.45f, 0.32f), 0.35f), mapRoot, false);
            dirt.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            paveHoles.Add(new Vector4(p.x, 0, p.z, 3.3f));

            var it = root.gameObject.AddComponent<Interactable>();
            it.radius = 4.5f;
            it.kind = InteractKind.QuestGiver;
            it.target = map;
            it.color = gold;
            WorldLabel.Create(root, p + Vector3.up * 4.4f, QuestDB.NpcNames[map] + " - QUESTS", gold);
        }

        /// <summary>Peretele din stanga hub-ului (simetric cu portalurile): 2 clasamente TOP POWER / TOP TIME PLAYED, cu fata spre spawn.</summary>
        void BuildLeaderboards(Vector3 o, Transform mapRoot)
        {
            Vector3 c = o + new Vector3(-CornerX, 0, CornerZ);
            var t = BuildCornerPlatform("Leaderboards", c, 90f, mapRoot);
            Color wood = new Color(0.36f, 0.24f, 0.15f), stone = new Color(0.52f, 0.52f, 0.58f), gold = new Color(1f, 0.78f, 0.3f);
            float W = 7f, H = 8.6f;
            for (int k = 0; k < 2; k++)
            {
                var root = new GameObject(k == 0 ? "Leaderboard - Power" : "Leaderboard - Time").transform;
                root.SetParent(t, false);
                root.localPosition = new Vector3(k == 0 ? 5f : -5f, 0, -0.8f);   // TOP POWER in stanga (vazut de la spawn)
                root.localRotation = Quaternion.identity;
                Art.Prim(PrimitiveType.Cube, new Vector3(0, 0.3f, 0), new Vector3(W + 1.2f, 0.6f, 1.4f), stone, root);
                for (int s = -1; s <= 1; s += 2)
                {
                    Art.Prim(PrimitiveType.Cube, new Vector3(s * (W * 0.5f + 0.3f), H * 0.5f + 0.6f, 0), new Vector3(0.5f, H + 0.6f, 0.5f), wood, root, false);
                    var cap = Art.Prim(PrimitiveType.Sphere, new Vector3(s * (W * 0.5f + 0.3f), H + 1.05f, 0), Vector3.one * 0.6f, gold, root, false, false);
                    Art.SetEmission(cap.GetComponent<Renderer>().sharedMaterial, gold * 0.5f);
                }
                Art.Prim(PrimitiveType.Cube, new Vector3(0, H * 0.5f + 0.9f, -0.05f), new Vector3(W + 0.2f, H + 0.2f, 0.25f), new Color(0.12f, 0.12f, 0.14f), root, false);
                Art.Prim(PrimitiveType.Cube, new Vector3(0, H + 1.05f, 0), new Vector3(W + 0.9f, 0.3f, 0.6f), wood, root, false);
                var b = new GameObject("Board").transform;
                b.SetParent(root, false);
                var lb = b.gameObject.AddComponent<Leaderboard>();
                lb.kind = k;
                lb.width = W; lb.height = H;
            }
            WorldLabel.Create(mapRoot, c + new Vector3(0, 11.8f, 0), "LEADERBOARDS", gold, 32f, 70f);
        }

        /// <summary>Decor simetric in hub: fantana in centru, felinare pe marginea platformei, semn la drum.</summary>
        void BuildHubDecor(Vector3 o, Transform root)
        {
            // fantana: langa cadou, pe un pavaj rotund (ca inelul din Trade Plaza, dar mai mic)
            Vector3 fp = o + new Vector3(7.5f, 0, -57.5f);
            var fpave = Art.Prim(PrimitiveType.Cylinder, fp + Vector3.up * 0.05f, new Vector3(8.4f, 0.02f, 8.4f), new Color(0.95f, 0.72f, 0.55f), root, false, false);
            var fcob = Art.PaintedTex("Cobble");
            if (fcob != null) Art.ApplyTexture(fpave.GetComponent<Renderer>().sharedMaterial, fcob, 2.4f);
            fpave.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var f = ModelLib.Place(GameData.TOWN + "fountain-round", fp, 4.2f, 0f, root, true);
            if (f != null)
            {
                // coliziune pe toata fantana (nu poti urca peste ea) + apa care curge
                var cc = f.gameObject.AddComponent<CapsuleCollider>();
                cc.radius = 2.2f; cc.height = 12f; cc.center = new Vector3(0, 6f, 0);
                var fx = new GameObject("FountainFx");
                fx.transform.SetParent(f, false);
                fx.transform.localPosition = new Vector3(0, 1.5f, 0);
                var ffx = fx.AddComponent<FountainFx>();
                ffx.waterLevel = -0.8f; ffx.basinRadius = 1.3f;
            }
            Block(fp, 4.5f);
            Color post = new Color(0.4f, 0.36f, 0.32f), glow = new Color(1f, 0.82f, 0.4f);
            // (felinarele de pe marginea cercului de spawn au fost scoase - erau prea multe)
        }

        /// <summary>Forja: platforma rotunda de piatra, cuptor cu cos si lava care straluceste, nicovala in centru,
        /// doua brazere cu foc, raft cu arme, jgheab de lava in fata. yaw = incotro priveste (spre spawn).</summary>
        void BuildForge(Vector3 p, Transform mapRoot, float yaw = 0f)
        {
            var root = Anchor("Forge", p, mapRoot);
            root.rotation = Quaternion.Euler(0, yaw, 0);
            Color stone = new Color(0.42f, 0.4f, 0.42f), stoneDk = new Color(0.26f, 0.25f, 0.28f), iron = new Color(0.3f, 0.31f, 0.36f), dark = new Color(0.14f, 0.14f, 0.17f);
            Color lava = new Color(1f, 0.45f, 0.08f), wood = new Color(0.38f, 0.25f, 0.15f);
            // platforma pavata cu bordura de pietre
            if (nature) ShrineArt.Platform(root, Vector3.zero, 4.3f, new Color(0.7f, 0.66f, 0.62f), NatureKit.For(curMap), 0.28f);
            else
            {
                Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.12f, 0), new Vector3(9.4f, 0.12f, 9.4f), stoneDk, root, false);
                Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.27f, 0), new Vector3(8.4f, 0.04f, 8.4f), stone, root);
            }
            // acoperis de lemn cu tigla rosie peste cuptor (stalpi + doua ape)
            Color roof = new Color(0.66f, 0.27f, 0.2f), roofDk = new Color(0.45f, 0.18f, 0.14f);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = 0; sz < 2; sz++)
                {
                    Vector3 pp = new Vector3(sx * 3.5f, 0, sz == 0 ? -4.1f : 0.6f);
                    Art.Prim(PrimitiveType.Cube, pp + Vector3.up * 2.1f, new Vector3(0.32f, 4.2f, 0.32f), wood, root);
                }
            for (int sd = -1; sd <= 1; sd += 2)
            {
                var rp = Art.Prim(PrimitiveType.Cube, new Vector3(0, 4.75f, -1.75f + sd * 1.45f), new Vector3(8.2f, 0.2f, 3.3f), sd < 0 ? roof : roofDk, root, false);
                rp.transform.localRotation = Quaternion.Euler(sd * 24f, 0, 0);
                for (int r = 0; r < 4; r++)   // randuri de tigla
                {
                    var tl = Art.Prim(PrimitiveType.Cube, new Vector3(0, 4.83f - r * 0.32f, -1.75f + sd * (0.35f + r * 0.72f)), new Vector3(8.3f, 0.08f, 0.16f), roofDk, root, false);
                    tl.transform.localRotation = Quaternion.Euler(sd * 24f, 0, 0);
                }
            }
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 5.42f, -1.75f), new Vector3(8.4f, 0.22f, 0.3f), wood, root, false);   // coama
            // cuptorul (in spate): corp, gura cu lava, cos
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 1.6f, -2.9f), new Vector3(3.4f, 2.7f, 2f), stoneDk, root);
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 3.05f, -2.9f), new Vector3(3.8f, 0.35f, 2.3f), stone, root, false);
            var mouth = Art.Prim(PrimitiveType.Cube, new Vector3(0, 1.15f, -1.88f), new Vector3(1.6f, 1f, 0.08f), lava, root, false, false);
            Art.SetEmission(mouth.GetComponent<Renderer>().sharedMaterial, lava * 2.4f);
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 5.0f, -3.2f), new Vector3(1f, 3.8f, 1f), stoneDk, root, false);
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 6.95f, -3.2f), new Vector3(1.3f, 0.22f, 1.3f), stone, root, false);
            AddFire(root, new Vector3(0, 1.1f, -1.7f), lava, 22f);
            AddFire(root, new Vector3(0, 7.2f, -3.2f), new Color(0.5f, 0.45f, 0.45f), 5f);   // fum din cos
            AddFire(root, new Vector3(-0.2f, 1.75f, 0.3f), new Color(1f, 0.85f, 0.35f), 4f);   // scantei pe nicovala
            // gramada de minereu / cristale langa nicovala
            Color[] oreC = { new Color(0.45f, 0.75f, 1f), new Color(0.75f, 0.5f, 1f), new Color(1f, 0.8f, 0.3f) };
            for (int k = 0; k < 5; k++)
            {
                var ore = Art.Prim(PrimitiveType.Cube, new Vector3(2.0f + (k % 3) * 0.38f, 0.42f + (k / 3) * 0.3f, 1.7f + (k % 2) * 0.35f), Vector3.one * (0.34f + (k % 2) * 0.1f), oreC[k % 3], root, false, false);
                ore.transform.localRotation = Quaternion.Euler(20f * k, 33f * k, 15f);
                Art.SetEmission(ore.GetComponent<Renderer>().sharedMaterial, oreC[k % 3] * 0.6f);
            }
            // nicovala in centru, pe un butuc
            Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.6f, 0.3f), new Vector3(1.1f, 0.35f, 1.1f), wood, root);
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 1.1f, 0.3f), new Vector3(0.6f, 0.4f, 0.5f), iron, root, false);
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 1.42f, 0.3f), new Vector3(1.9f, 0.32f, 0.7f), iron, root, false);
            var horn = Art.Prim(PrimitiveType.Cylinder, new Vector3(1.15f, 1.42f, 0.3f), new Vector3(0.38f, 0.25f, 0.38f), iron, root, false);
            horn.transform.localRotation = Quaternion.Euler(0, 0, 90);
            var hot = Art.Prim(PrimitiveType.Cube, new Vector3(-0.2f, 1.62f, 0.3f), new Vector3(0.9f, 0.08f, 0.18f), lava, root, false, false);
            Art.SetEmission(hot.GetComponent<Renderer>().sharedMaterial, lava * 1.8f);
            // doua brazere in fata (simetrice)
            for (int sd = -1; sd <= 1; sd += 2)
            {
                Vector3 bp = new Vector3(sd * 3.2f, 0, 2.6f);
                Art.Prim(PrimitiveType.Cylinder, bp + Vector3.up * 0.75f, new Vector3(0.35f, 0.75f, 0.35f), dark, root, false);
                Art.Prim(PrimitiveType.Cylinder, bp + Vector3.up * 1.55f, new Vector3(0.95f, 0.12f, 0.95f), iron, root, false);
                var coal = Art.Prim(PrimitiveType.Cylinder, bp + Vector3.up * 1.66f, new Vector3(0.75f, 0.04f, 0.75f), lava, root, false, false);
                Art.SetEmission(coal.GetComponent<Renderer>().sharedMaterial, lava * 2f);
                AddFire(root, bp + Vector3.up * 1.8f, new Color(1f, 0.55f, 0.12f), 12f);
            }
            // raft cu arme (lateral, in spate)
            for (int sd = -1; sd <= 1; sd += 2)
            {
                Vector3 rp = new Vector3(sd * 3.1f, 0, -1.2f);
                Art.Prim(PrimitiveType.Cube, rp + new Vector3(0, 0.9f, 0), new Vector3(0.15f, 1.5f, 1.6f), wood, root, false);
                for (int w = 0; w < 3; w++)
                {
                    var blade = Art.Prim(PrimitiveType.Cube, rp + new Vector3(-sd * 0.18f, 1.05f, -0.55f + w * 0.55f), new Vector3(0.06f, 1.3f, 0.14f), new Color(0.75f, 0.78f, 0.85f), root, false);
                    blade.transform.localRotation = Quaternion.Euler(0, 0, sd * 8f);
                }
            }
            // jgheab de lava in fata platformei
            var trough = Art.Prim(PrimitiveType.Cube, new Vector3(0, 0.3f, 3.9f), new Vector3(4.2f, 0.12f, 0.45f), lava, root, false, false);
            Art.SetEmission(trough.GetComponent<Renderer>().sharedMaterial, lava * 1.6f);
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 0.25f, 3.9f), new Vector3(4.6f, 0.2f, 0.75f), stoneDk, root, false);
            // (fara lumina punctiforma: lava / jarul emisiv + bloom dau aceeasi impresie, mult mai ieftin pe telefon)
            Block(p, 6.5f);

            var it = root.gameObject.AddComponent<Interactable>();
            it.radius = 5.5f;
            it.kind = InteractKind.Forge;
            it.color = new Color(1f, 0.65f, 0.25f);
            WorldLabel.Create(root, p + Vector3.up * 7.8f, "FORGE", new Color(1f, 0.65f, 0.25f), 30f);
        }

        void AddFire(Transform root, Vector3 local, Color c, float rate)
        {
            var go = new GameObject("FireFx");
            go.transform.SetParent(root, false);
            go.transform.localPosition = local;
            var f = go.AddComponent<FireFx>();
            f.col = c; f.rate = rate;
        }

        /// <summary>Altarul de REBIRTH (doar in hub): langa arena boss-ului, cu drumul lui separat si o alee cu felinare.</summary>
        void BuildRebirthShrine(Vector3 o, Transform start, Transform arena)
        {
            Vector3 c = o + new Vector3(-30f, 0, 30f);
            Color stone = new Color(0.6f, 0.58f, 0.66f), stoneDk = new Color(0.4f, 0.38f, 0.46f), pink = new Color(1f, 0.45f, 0.9f);
            // platforma pavata cu bordura, cerc de rune roz care se roteste, 6 menhire cu cristale roz
            if (nature)
            {
                var th0 = NatureKit.For(0);
                ShrineArt.Platform(arena, c, 6.2f, new Color(0.86f, 0.82f, 0.92f), th0);
                ShrineArt.RuneCircle(arena, c + Vector3.up * 0.24f, 4.4f, pink);
                ShrineArt.Menhirs(arena, c, 6, 6.3f, 4.2f, th0, pink, 0f);
                ShrineArt.Glow(arena, c + Vector3.up * 3f, pink, 12f, 1.6f);
            }
            else
            {
            var b0 = Art.Prim(PrimitiveType.Cylinder, c + new Vector3(0, 0.1f, 0), new Vector3(13f, 0.1f, 13f), stoneDk, arena, false);
            b0.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var b1 = Art.Prim(PrimitiveType.Cylinder, c + new Vector3(0, 0.22f, 0), new Vector3(11.6f, 0.03f, 11.6f), stone, arena, false);
            b1.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            for (int k = 0; k < 6; k++)
            {
                float a = (k * 60f + 30f) * Mathf.Deg2Rad;
                Vector3 cp = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 5.4f;
                Art.Prim(PrimitiveType.Cylinder, cp + Vector3.up * 2.2f, new Vector3(0.7f, 2.1f, 0.7f), stone, arena);
                var gem = Art.Prim(PrimitiveType.Cube, cp + Vector3.up * 4.6f, Vector3.one * 0.5f, pink, arena, false, false);
                gem.transform.rotation = Quaternion.Euler(45, 45, 0);
                Art.SetEmission(gem.GetComponent<Renderer>().sharedMaterial, pink * 0.9f);
            }
            }
            // statuia priveste spre aleea care vine de la drumul principal
            Vector3 face = new Vector3(5.6f, 0, -3.8f).normalized;
            BuildStatue(c + Vector3.up * 0.24f, start, Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg);
            Block(c, 8f);
            // drumul separat: din drumul principal (la z ~ 12) spre altar, cu felinare pe ambele parti (alee)
            Vector3 from = o + new Vector3(Mathf.Sin(12f * 0.12f) * 3f + 1.8f, 0, 12.5f), to = c + new Vector3(5.6f, 0, -3.8f);
            BuildAlley(from, to, Color.Lerp(GameData.Maps[0].ground, new Color(0.78f, 0.74f, 0.82f), 0.65f), pink, arena);
        }

        // ---------------------------------------------------------------
        //  Pavele, alei, platforme (fara iarba peste ele)
        // ---------------------------------------------------------------
        static Color CobCol(int i)
        {
            return i == 3 ? new Color(0.62f, 0.52f, 0.5f) : i == 2 ? new Color(0.92f, 0.95f, 1f) : i == 1 ? new Color(1f, 0.93f, 0.8f) : Color.white;
        }

        /// <summary>Disc de piatra pavata (putin mai deschis decat solul din jur) cu bordura din pietre mici.</summary>
        void StoneDisc(Vector3 p, float r, Transform parent)
        {
            var cob = Art.PaintedTex("Cobble");
            Color cc = CobCol(curMap);
            var d = Art.Prim(PrimitiveType.Cylinder, p + Vector3.up * 0.04f, new Vector3(r * 2f, 0.02f, r * 2f), Color.white, parent, false, false);
            Art.SetColor(d.GetComponent<Renderer>().sharedMaterial, Color.Lerp(cc, Color.white, 0.25f));
            if (cob != null) Art.ApplyTexture(d.GetComponent<Renderer>().sharedMaterial, cob, r / 2.2f);
            d.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var th = NatureKit.For(curMap);
            int n = Mathf.Max(10, Mathf.RoundToInt(r * 2f * Mathf.PI / 1.15f));
            for (int k = 0; k < n; k++)
            {
                float a = k / (float)n * Mathf.PI * 2f;
                Vector3 sp = p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r + 0.15f);
                var st = NatureKit.Place(NatureKit.PathStones[rng.Next(NatureKit.PathStones.Length)], sp, R(1.05f, 1.3f), -a * Mathf.Rad2Deg + R(-12f, 12f), parent, th, true);
                if (st == null) continue;
                st.localScale = new Vector3(1f, 0.9f, 1f);
                foreach (var rr in st.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>Cactus construit din bucati: trunchi verde inchis + coaste mai deschise, brate si o floare roz in varf.</summary>
        void BuildCactus(Vector3 p, float h, Transform parent)
        {
            var root = Anchor("Cactus", p, parent);
            root.rotation = Quaternion.Euler(0, R(0, 360), 0);
            Color body = new Color(0.2f, 0.52f, 0.26f), rib = new Color(0.36f, 0.7f, 0.36f), dark = new Color(0.14f, 0.4f, 0.2f);
            float w = Mathf.Clamp(h * 0.24f, 0.55f, 0.9f);
            bool barrel = h < 2.4f;
            if (barrel)
            {
                // cactus rotund (butoi)
                float bh = h * 0.55f;
                Art.Prim(PrimitiveType.Sphere, new Vector3(0, bh * 0.5f, 0), new Vector3(w * 2f, bh, w * 2f), body, root, true);
                for (int k = 0; k < 8; k++)
                {
                    float a = k / 8f * Mathf.PI * 2f;
                    var rb = Art.Prim(PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * w * 0.82f, bh * 0.5f, Mathf.Sin(a) * w * 0.82f), new Vector3(w * 0.42f, bh * 0.96f, w * 0.42f), k % 2 == 0 ? rib : dark, root, false);
                    rb.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                }
                Flower(root, new Vector3(0, bh, 0), w * 0.55f);
                return;
            }
            // trunchi inalt (saguaro)
            Column(root, Vector3.zero, h, w, body, rib, dark);
            int arms = rng.Next(1, 3);
            for (int k = 0; k < arms; k++)
            {
                float side = k == 0 ? 1f : -1f;
                float ay = h * R(0.35f, 0.55f), aw = w * 0.62f, out1 = w * 1.6f, ah = h * R(0.28f, 0.4f);
                var hor = Art.Prim(PrimitiveType.Capsule, new Vector3(side * out1 * 0.5f, ay, 0), new Vector3(aw * 2f, out1 * 0.5f + aw, aw * 2f), body, root, false);
                hor.transform.localRotation = Quaternion.Euler(0, 0, 90);
                Column(root, new Vector3(side * out1, ay, 0), ah, aw, body, rib, dark);
                Flower(root, new Vector3(side * out1, ay + ah, 0), aw * 0.6f);
            }
            Flower(root, new Vector3(0, h, 0), w * 0.6f);
        }

        void Column(Transform root, Vector3 b, float h, float w, Color body, Color rib, Color dark)
        {
            Art.Prim(PrimitiveType.Capsule, b + new Vector3(0, h * 0.5f, 0), new Vector3(w * 2f, h * 0.5f, w * 2f), body, root, b == Vector3.zero);
            for (int k = 0; k < 6; k++)
            {
                float a = k / 6f * Mathf.PI * 2f;
                Art.Prim(PrimitiveType.Capsule, b + new Vector3(Mathf.Cos(a) * w * 0.86f, h * 0.5f, Mathf.Sin(a) * w * 0.86f), new Vector3(w * 0.36f, h * 0.48f, w * 0.36f), k % 2 == 0 ? rib : dark, root, false);
            }
        }

        void Flower(Transform root, Vector3 at, float s)
        {
            Color petal = rng.NextDouble() < 0.5 ? new Color(1f, 0.45f, 0.7f) : new Color(1f, 0.85f, 0.35f);
            for (int k = 0; k < 5; k++)
            {
                float a = k / 5f * Mathf.PI * 2f;
                Art.Prim(PrimitiveType.Sphere, at + new Vector3(Mathf.Cos(a) * s * 0.45f, s * 0.15f, Mathf.Sin(a) * s * 0.45f), new Vector3(s * 0.6f, s * 0.3f, s * 0.6f), petal, root, false);
            }
            Art.Prim(PrimitiveType.Sphere, at + new Vector3(0, s * 0.25f, 0), Vector3.one * s * 0.4f, new Color(1f, 0.9f, 0.4f), root, false);
        }

        readonly List<Vector4> paveHoles = new List<Vector4>();

        /// <summary>O pavela (disc plat) + gaura in iarba sub ea, ca iarba sa nu creasca peste.</summary>
        void Paver(Vector3 p, float sx, float sz, Color c, Transform parent)
        {
            if (nature)
            {
                var ps = NatureKit.Place(Pick(NatureKit.PathStones), p + Vector3.up * 0.01f, Mathf.Max(sx, sz) * 1.05f, R(0, 360), parent, NatureKit.For(curMap), true);
                if (ps != null)
                {
                    foreach (var r in ps.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    ps.localScale = new Vector3(1f, 0.6f, 1f);   // plate, nu te impiedici in ele
                    paveHoles.Add(new Vector4(p.x, 0, p.z, Mathf.Max(sx, sz) * 0.5f + 0.9f));
                    return;
                }
            }
            var st = Art.Prim(PrimitiveType.Cylinder, p + Vector3.up * 0.03f, new Vector3(sx, 0.02f, sz), c, parent, false);
            st.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            paveHoles.Add(new Vector4(p.x, 0, p.z, Mathf.Max(sx, sz) * 0.5f + 0.9f));
        }

        /// <summary>Alee: pavele intre doua puncte + felinare pereche de o parte si de alta.</summary>
        void BuildAlley(Vector3 from, Vector3 to, Color paveCol, Color lampCol, Transform parent)
        {
            Vector3 dir = to - from; dir.y = 0;
            float len = dir.magnitude;
            if (len < 1f) return;
            dir /= len;
            Vector3 side = new Vector3(dir.z, 0, -dir.x);
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 2.5f));
            for (int k = 0; k <= n; k++)
            {
                Vector3 sp = from + dir * (k * len / n);
                Paver(sp, 1.9f, 1.9f, paveCol, parent);
                Block(sp, 2.4f);
                // felinare doar unde e nevoie: o pereche la capatul aleii (si una la inceput daca aleea e lunga)
                if (k == n)
                    for (int sd = -1; sd <= 1; sd += 2)
                    {
                        Vector3 lp = sp + side * (sd * 2.1f);
                        Art.Prim(PrimitiveType.Cylinder, lp + Vector3.up * 1.1f, new Vector3(0.18f, 1.1f, 0.18f), new Color(0.35f, 0.3f, 0.36f), parent, false);
                        var lamp = Art.Prim(PrimitiveType.Sphere, lp + Vector3.up * 2.35f, Vector3.one * 0.45f, lampCol, parent, false, false);
                        Art.SetEmission(lamp.GetComponent<Renderer>().sharedMaterial, lampCol * 1.1f);
                        paveHoles.Add(new Vector4(lp.x, 0, lp.z, 0.7f));
                    }
            }
        }

        /// <summary>Platforma rotunda (cu bordura) pentru ou / alte locuri speciale.</summary>
        void BuildSpotPad(Vector3 p, float r, MapDef m, Transform parent)
        {
            if (nature && Art.PaintedTex("Cobble") != null)
            {
                StoneDisc(p, r + 0.4f, parent);
                paveHoles.Add(new Vector4(p.x, 0, p.z, r + 1.2f));
                Block(p, r + 1.5f);
                return;
            }
            Color pave = Color.Lerp(m.ground, Color.white, 0.55f), rim = Color.Lerp(m.ground, new Color(0.45f, 0.45f, 0.5f), 0.65f);
            var a = Art.Prim(PrimitiveType.Cylinder, p + Vector3.up * 0.015f, new Vector3(r * 2f + 1.2f, 0.02f, r * 2f + 1.2f), rim, parent, false);
            a.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var b = Art.Prim(PrimitiveType.Cylinder, p + Vector3.up * 0.025f, new Vector3(r * 2f, 0.02f, r * 2f), pave, parent, false);
            b.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            paveHoles.Add(new Vector4(p.x, 0, p.z, r + 1.2f));
            Block(p, r + 1.5f);
        }

        void BuildEgg(Vector3 p, Transform mapRoot, int map)
        {
            var root = Anchor("EggAltar", p, mapRoot);
            var eb = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.4f, 0), new Vector3(2.8f, 0.4f, 2.8f), new Color(0.9f, 0.85f, 1f), root, true, false);
            var ebTex = nature ? Art.PaintedTex("Cobble") : null;
            if (ebTex != null) { Art.SetColor(eb.GetComponent<Renderer>().sharedMaterial, CobCol(map) * 0.95f); Art.ApplyTexture(eb.GetComponent<Renderer>().sharedMaterial, ebTex, 0.9f); }
            var group = new GameObject("EggGroup").transform;
            group.SetParent(root, false);
            group.localPosition = new Vector3(0, 2.3f, 0);
            var egg = Art.Prim(PrimitiveType.Sphere, Vector3.zero, new Vector3(1.5f, 2f, 1.5f), Color.white, group, false, false);
            Art.SetEmission(egg.GetComponent<Renderer>().sharedMaterial, new Color(0.15f, 0.15f, 0.2f));
            float[] dys = { -0.4f, 0.1f, 0.5f };
            for (int k = 0; k < 9; k++)
            {
                float dy = dys[k % 3];
                float hr = 0.75f * Mathf.Sqrt(1f - dy * dy) - 0.05f;
                float ang = k * 0.7f;
                Color sc = Color.HSVToRGB(k / 9f, 0.7f, 1f);
                Art.Prim(PrimitiveType.Sphere, new Vector3(Mathf.Cos(ang) * hr, dy, Mathf.Sin(ang) * hr), Vector3.one * 0.32f, sc, group, false);
            }
            var sp = group.gameObject.AddComponent<Spinner>();
            sp.rot = new Vector3(0, 40, 0); sp.bobAmp = 0.2f; sp.bobSpeed = 2f;
            // pets decorative in jurul altarului
            string[] deco = { GameData.MapPets[map][0], GameData.MapPets[map][2], GameData.MapPets[map][4] };
            for (int k = 0; k < 3; k++)
            {
                float a = k / 3f * Mathf.PI * 2f + 0.5f;
                ModelLib.Place(GameData.PETS + deco[k], p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.6f + Vector3.up * 0.8f, 0.8f, -a * Mathf.Rad2Deg + 90f, root);
            }

            var it = root.gameObject.AddComponent<Interactable>();
            it.radius = 4.5f;
            it.kind = InteractKind.EggAltar;
            it.target = map;   // fiecare harta are oul ei, cu alti pets
            it.color = new Color(1f, 0.6f, 0.95f);
            WorldLabel.Create(root, p + Vector3.up * 4.4f, GameData.Maps[map].name.ToUpper() + " EGG", new Color(1f, 0.6f, 0.95f), 20f);
        }

        void BuildStatue(Vector3 p, Transform mapRoot, float yaw = 0f)
        {
            var root = Anchor("RebirthStatue", p, mapRoot);
            root.rotation = Quaternion.Euler(0, yaw, 0);
            // soclu in trepte (piatra pavata) + statuia + cristal mare roz cu un inel de cristale mici care orbiteaza
            Color pk = new Color(1f, 0.45f, 0.9f);
            var cobT = nature ? Art.PaintedTex("Cobble") : null;
            var ped0 = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.3f, 0), new Vector3(3.4f, 0.3f, 3.4f), new Color(0.62f, 0.6f, 0.7f), root, true, false);
            var ped1 = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.8f, 0), new Vector3(2.5f, 0.2f, 2.5f), new Color(0.78f, 0.76f, 0.86f), root, true, false);
            if (cobT != null) { Art.ApplyTexture(ped0.GetComponent<Renderer>().sharedMaterial, cobT, 1.2f); Art.ApplyTexture(ped1.GetComponent<Renderer>().sharedMaterial, cobT, 0.9f); }
            var trim = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.61f, 0), new Vector3(3.5f, 0.03f, 3.5f), pk, root, false, false);
            Art.SetEmission(trim.GetComponent<Renderer>().sharedMaterial, pk * 0.8f);
            var st = ModelLib.Spawn(GameData.ARENA + "statue", Anchor("S", p + Vector3.up * 1f, root), 3.2f, 180f);
            if (st == null) Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 2.5f, 0), new Vector3(0.9f, 1.5f, 0.9f), new Color(1f, 0.8f, 0.2f), root);
            ShrineArt.Crystal(root, new Vector3(0, 5.4f, 0), 1.1f, pk, 50f);
            ShrineArt.Orbit(root, new Vector3(0, 5.3f, 0), 1.5f, 8, Color.Lerp(pk, Color.white, 0.3f), 45f);
            AddFire(root, new Vector3(0, 1f, 0), pk, 3f);

            var it = root.gameObject.AddComponent<Interactable>();
            it.radius = 4.5f;
            it.kind = InteractKind.Rebirth;
            it.color = new Color(1f, 0.45f, 0.95f);
            WorldLabel.Create(root, p + Vector3.up * 6.4f, "REBIRTH", new Color(1f, 0.45f, 0.95f));
        }

        /// <summary>ITEM INDEX: un pupitru de piatra cu o carte mare deschisa, 4 pietre (culorile raritatilor) care se rotesc deasupra.</summary>
        void BuildIndex(Vector3 p, Transform mapRoot, float yaw)
        {
            var root = Anchor("ItemIndex", p, mapRoot);
            root.rotation = Quaternion.Euler(0, yaw, 0);
            Color stone = new Color(0.62f, 0.6f, 0.66f), dark = new Color(0.42f, 0.4f, 0.46f), gold = new Color(1f, 0.8f, 0.32f);
            Color cover = new Color(0.45f, 0.12f, 0.16f), page = new Color(0.98f, 0.94f, 0.84f);
            // pupitrul
            Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.15f, 0), new Vector3(2.4f, 0.15f, 2.4f), dark, root);
            Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.75f, 0), new Vector3(0.8f, 0.6f, 0.8f), stone, root);
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 1.45f, 0), new Vector3(1.9f, 0.18f, 1.3f), dark, root, false);
            // cartea: coperta + doua pagini in "V" + semn de carte
            var book = new GameObject("Book").transform;
            book.SetParent(root, false);
            book.localPosition = new Vector3(0, 1.6f, 0);
            book.localRotation = Quaternion.Euler(-18f, 0, 0);
            Art.Prim(PrimitiveType.Cube, new Vector3(0, 0, 0), new Vector3(2.1f, 0.08f, 1.45f), cover, book, false);
            for (int s2 = -1; s2 <= 1; s2 += 2)
            {
                var pg = Art.Prim(PrimitiveType.Cube, new Vector3(s2 * 0.5f, 0.1f, 0), new Vector3(0.98f, 0.1f, 1.3f), page, book, false);
                pg.transform.localRotation = Quaternion.Euler(0, 0, -s2 * 8f);
                for (int l = 0; l < 4; l++)   // randuri de "text"
                    Art.Prim(PrimitiveType.Cube, new Vector3(s2 * 0.5f, 0.16f + Mathf.Abs(s2 * 0.5f) * 0.12f, 0.4f - l * 0.24f), new Vector3(0.7f, 0.01f, 0.05f), new Color(0.55f, 0.5f, 0.45f), book, false);
            }
            Art.Prim(PrimitiveType.Cube, new Vector3(0.1f, -0.05f, -0.8f), new Vector3(0.12f, 0.02f, 0.4f), gold, book, false);
            // 4 pietre (Common, Rare, Legendary, Mythic) care se rotesc deasupra cartii
            var orbit = new GameObject("Orbit").transform;
            orbit.SetParent(root, false);
            orbit.localPosition = new Vector3(0, 3f, 0);
            var sp = orbit.gameObject.AddComponent<Spinner>(); sp.rot = new Vector3(0, 45f, 0); sp.bobAmp = 0.15f; sp.bobSpeed = 1.6f;
            int[] rr = ItemDB.UsedRarities;
            for (int k = 0; k < rr.Length; k++)
            {
                float a = k / (float)rr.Length * Mathf.PI * 2f;
                Color c = ItemDB.Col(rr[k]);
                var g = Art.Prim(PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 1.1f, 0, Mathf.Sin(a) * 1.1f), Vector3.one * 0.32f, c, orbit, false, false);
                g.transform.localRotation = Quaternion.Euler(45f, 0, 45f);
                Art.SetEmission(g.GetComponent<Renderer>().sharedMaterial, c * 0.6f);
                if (rr[k] >= 5) g.AddComponent<MythicHue>();
            }
            var col = root.gameObject.AddComponent<CapsuleCollider>();
            col.radius = 1.1f; col.height = 3f; col.center = new Vector3(0, 1.5f, 0);
            var it = root.gameObject.AddComponent<Interactable>();
            it.radius = 4.2f;
            it.kind = InteractKind.ItemIndex;
            it.color = gold;
            WorldLabel.Create(root, p + Vector3.up * 4.6f, "ITEM INDEX", gold);
            Block(p, 4.2f);
        }

        void BuildShop(Vector3 p, Transform mapRoot)
        {
            var root = Anchor("Shop", p, mapRoot);
            var s = ModelLib.Spawn(GameData.TOWN + "stall-green", root, 4f, 200f);
            if (s == null)
            {
                Art.Prim(PrimitiveType.Cube, new Vector3(0, 0.6f, 0), new Vector3(3f, 1.2f, 1.4f), new Color(0.6f, 0.4f, 0.2f), root);
                Art.Prim(PrimitiveType.Cube, new Vector3(0, 2.8f, 0), new Vector3(3.4f, 0.3f, 2f), new Color(0.3f, 0.8f, 0.4f), root, false);
            }
            ModelLib.Place(GameData.DUN + "potion", p + new Vector3(0.8f, 1.2f, 0.3f), 0.6f, 0f, root);
            ModelLib.Place(GameData.DUN + "chest", p + new Vector3(-1.8f, 0, 1.4f), 1f, 30f, root);
            var it = root.gameObject.AddComponent<Interactable>();
            it.radius = 4.5f;
            it.kind = InteractKind.Shop;
            it.color = new Color(0.45f, 1f, 0.55f);
            WorldLabel.Create(root, p + Vector3.up * 4.4f, "SHOP", new Color(0.45f, 1f, 0.55f));
        }

        void BuildGift(Vector3 p, Transform mapRoot)
        {
            var root = Anchor("Gift", p, mapRoot);
            var g = ModelLib.Spawn(GameData.HOL + "present-a-cube", Anchor("G", p, root), 1.8f);
            if (g == null)
            {
                Art.Prim(PrimitiveType.Cube, new Vector3(0, 0.8f, 0), Vector3.one * 1.6f, new Color(1f, 0.3f, 0.4f), root);
                Art.Prim(PrimitiveType.Cube, new Vector3(0, 0.8f, 0), new Vector3(1.65f, 1.65f, 0.3f), new Color(1f, 0.9f, 0.3f), root, false);
            }
            var it = root.gameObject.AddComponent<Interactable>();
            it.radius = 4f;
            it.kind = InteractKind.Gift;
            it.color = new Color(1f, 0.85f, 0.3f);
            WorldLabel.Create(root, p + Vector3.up * 3f, "GIFT", new Color(1f, 0.85f, 0.3f));
        }

        void BuildHeroPad(Vector3 p, Transform mapRoot)
        {
            var root = Anchor("HeroPad", p, mapRoot);
            var pad = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.08f, 0), new Vector3(3f, 0.08f, 3f), new Color(0.5f, 0.8f, 1f), root, false, false);
            Art.SetEmission(pad.GetComponent<Renderer>().sharedMaterial, new Color(0.1f, 0.25f, 0.4f));
            var it = root.gameObject.AddComponent<Interactable>();
            it.radius = 2.8f;
            it.kind = InteractKind.HeroPad;
            it.color = new Color(0.5f, 0.85f, 1f);
            WorldLabel.Create(root, p + Vector3.up * 1.2f, "CHANGE HERO", new Color(0.5f, 0.85f, 1f), 18f);
        }

        void BuildTradePortal(Vector3 p, Transform mapRoot, float yaw = 0f)
        {
            var root = Anchor("TradePortal", p, mapRoot);
            root.rotation = Quaternion.Euler(0, yaw, 0);
            Color c = new Color(1f, 0.8f, 0.3f);
            PortalArt.Build(root, c, new Color(0.78f, 0.62f, 0.48f));
            var it = root.gameObject.AddComponent<Interactable>();
            it.radius = 4.5f;
            it.kind = InteractKind.TradePortal;
            it.color = c;
            WorldLabel.Create(root, p + Vector3.up * PortalArt.LabelY, "TRADE PLAZA", c, 26f);
        }

        void BuildPortal(Vector3 p, int target, Transform mapRoot, string prefix, float yaw = 0f)
        {
            var tm = GameData.Maps[target];
            var root = Anchor("Portal_" + tm.name, p, mapRoot);
            root.rotation = Quaternion.Euler(0, yaw, 0);
            Color c = tm.enemies[0].col;
            PortalArt.Build(root, c, new Color(0.6f, 0.6f, 0.66f));

            var it = root.gameObject.AddComponent<Interactable>();
            it.radius = 4.5f;
            bool back = prefix.Length > 0;
            it.kind = InteractKind.MapPortal;
            it.target = target;
            it.back = back;
            it.color = Color.Lerp(c, Color.white, 0.4f);
            WorldLabel.Create(root, p + Vector3.up * PortalArt.LabelY, back ? prefix + tm.name : tm.name + "  (Lv " + tm.req + ")", Color.Lerp(c, Color.white, 0.4f), 22f);
        }
    }


}
