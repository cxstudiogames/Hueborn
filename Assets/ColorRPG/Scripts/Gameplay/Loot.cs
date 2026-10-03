// =====================================================================
//  Loot.cs - monede, minereu si cufere cu echipament (cu raza colorata)
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public class Loot : MonoBehaviour
    {
        public enum Kind { Coin, Ore, Item }

        public Kind kind;
        public long amount;
        public Item item;
        Color col;
        Vector3 vel;
        bool landed;
        float age, groundY = 0.45f;
        Transform vis;
        GameObject beam;
        Transform icon;          // iconita care pluteste deasupra (itemul / minereul exact)
        Transform glow;          // cercul colorat de pe jos
        float landedAt = -1f;
        /// <summary>Loot-ul neluat dispare dupa atatea secunde (ultimele 3 clipeste).</summary>
        public const float Life = 10f;
        int bounces;
        bool collected;
        // multiplayer: loot comun, il ia primul care ajunge la el
        public int netId;
        bool claimed;
        float claimT;
        static bool fromNet;
        static readonly System.Collections.Generic.Dictionary<int, Loot> byNet = new System.Collections.Generic.Dictionary<int, Loot>();
        public Color NetColor { get { return col; } }
        /// <summary>Loot-ul care nu e moneda (minereu, iteme) - se ia cu [E] (sau automat cu MAGNET PASS).</summary>
        public static readonly System.Collections.Generic.List<Loot> Pickable = new System.Collections.Generic.List<Loot>();
        public bool Landed { get { return landed; } }
        public string PickName { get { return kind == Kind.Item && item != null ? item.name : amount + " Ore"; } }
        public Color PickCol { get { return kind == Kind.Item && item != null ? ItemDB.Col(item.rarity) : col; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Pickable.Clear(); byNet.Clear(); fromNet = false; glowMats.Clear(); }

        static readonly System.Collections.Generic.Dictionary<int, Material> glowMats = new System.Collections.Generic.Dictionary<int, Material>();
        static Material GlowMat(Color c, float a)
        {
            int k = ((int)(c.r * 31) << 15) | ((int)(c.g * 31) << 10) | ((int)(c.b * 31) << 5) | (int)(a * 31);
            Material m;
            if (!glowMats.TryGetValue(k, out m) || m == null) { m = Art.MeshFx(Art.A(c, a), true, Art.SoftDot); glowMats[k] = m; }
            return m;
        }

        /// <summary>Cerc moale colorat pe jos (sub loot).</summary>
        void AddGlow(Color c, float size, float a)
        {
            var q = Art.Prim(PrimitiveType.Quad, new Vector3(0, 0.04f, 0), new Vector3(size, size, 1f), Color.white, transform, false, true);
            q.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = GlowMat(c, a);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            glow = q.transform;
            q.SetActive(false);
        }

        /// <summary>Iconita care pluteste deasupra loot-ului (mereu cu fata spre camera).</summary>
        void AddIcon(Sprite sp, float size, float height)
        {
            if (sp == null) return;
            var go = new GameObject("Icon");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, height, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = 5;
            float px = Mathf.Max(sp.rect.width, sp.rect.height) / sp.pixelsPerUnit;
            go.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, px));
            icon = go.transform;
            go.SetActive(false);
        }

        void OnDisable() { Pickable.Remove(this); Loot x; if (netId != 0 && byNet.TryGetValue(netId, out x) && x == this) byNet.Remove(netId); }

        /// <summary>Loot nou creat de tine: il vad si ceilalti din camera.</summary>
        static void Share(Loot l, Vector3 pos)
        {
            if (fromNet || !Mp.ShareLoot) return;
            l.netId = Mp.NewLootId();
            byNet[l.netId] = l;
            Mp.ShareLootSpawn(l, pos, l.vel);
        }

        /// <summary>Loot creat de alt jucator din camera.</summary>
        public static void SpawnNet(int id, Kind k, long amount, Vector3 pos, Vector3 vel, Color c, Item it)
        {
            if (byNet.ContainsKey(id)) return;
            fromNet = true;
            Loot l = null;
            try
            {
                if (k == Kind.Coin) l = SpawnCoin(amount, pos);
                else if (k == Kind.Ore) l = SpawnOre((int)amount, pos, c);
                else if (it != null) l = SpawnItem(it, pos);
            }
            finally { fromNet = false; }
            if (l == null) return;
            l.vel = vel;
            l.netId = id;
            byNet[id] = l;
        }

        /// <summary>Gazda a decis cine a luat loot-ul "id".</summary>
        public static void NetTaken(int id, bool mine)
        {
            Loot l;
            if (!byNet.TryGetValue(id, out l) || l == null) return;
            byNet.Remove(id);
            if (mine && Game.I != null && Game.I.player != null) { l.collected = false; l.Apply(Game.I.player); }
            else Destroy(l.gameObject);
        }

        /// <summary>Ia cu mana tot loot-ul (non-moneda) din jurul jucatorului.</summary>
        public static int PickUpAround(Player p, float r)
        {
            int n = 0;
            for (int i = Pickable.Count - 1; i >= 0; i--)
            {
                if (i >= Pickable.Count) continue;
                var l = Pickable[i];
                if (l == null || !l.landed) continue;
                Vector3 d = l.transform.position - p.transform.position; d.y = 0;
                if (d.magnitude <= r) { l.Collect(p); n++; }
            }
            if (n > 0) p.PickUpAnim();
            return n;
        }

        // ---------------------------------------------------------------
        //  Fabrici
        // ---------------------------------------------------------------
        static Loot Make(Kind k, Vector3 pos, Color c, float up = 1f)
        {
            var go = new GameObject(k.ToString());
            go.transform.SetParent(SceneGroups.Get("Loot"), false);
            go.transform.position = pos + Vector3.up * up;
            var l = go.AddComponent<Loot>();
            l.kind = k; l.col = c;
            if (k != Kind.Coin) Pickable.Add(l);
            l.vel = new Vector3(Random.Range(-3.5f, 3.5f), Random.Range(7f, 10f), Random.Range(-3.5f, 3.5f));
            l.vis = new GameObject("Vis").transform;
            l.vis.SetParent(go.transform, false);
            return l;
        }

        public static Loot SpawnCoin(long amount, Vector3 pos)
        {
            var l = Make(Kind.Coin, pos, new Color(1f, 0.85f, 0.1f));
            l.amount = amount;
            var m = ModelLib.Spawn(GameData.DUN + "coin", l.vis, 0.75f, 0f, true, false);
            if (m != null)
            {
                m.localPosition = new Vector3(0, -0.35f, 0);
                foreach (var mat in ModelLib.OwnMaterials(m)) Art.SetEmission(mat, new Color(0.35f, 0.25f, 0f));
            }
            else
            {
                var v = Art.Prim(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.6f, 0.06f, 0.6f), new Color(1f, 0.85f, 0.1f), l.vis, false);
                v.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            Share(l, pos);
            return l;
        }

        public static Loot SpawnOre(int amount, Vector3 pos, Color c)
        {
            var l = Make(Kind.Ore, pos, c);
            l.amount = amount;
            // grupuri de cristale (3 bucati) in culoarea minereului + iconita deasupra + cerc pe jos
            var mat = GearVisuals.Mat(c, 0.1f, 0.85f, c * 0.45f);   // partajat (cache dupa culoare)
            float[,] cr = { { 0f, 0f, 0f, 1f }, { 0.2f, -0.08f, 35f, 0.7f }, { -0.18f, -0.1f, -30f, 0.6f } };
            for (int i = 0; i < 3; i++)
            {
                var v = Art.Prim(PrimitiveType.Cube, new Vector3(cr[i, 0], cr[i, 1] * 0.5f, i == 2 ? 0.08f : -0.04f), Vector3.one, c, l.vis, false, true);
                v.transform.localRotation = Quaternion.Euler(0, 45f + i * 20f, cr[i, 2]) * Quaternion.Euler(0, 45f, 0);
                v.transform.localScale = new Vector3(0.22f, 0.62f, 0.22f) * cr[i, 3];
                v.GetComponent<Renderer>().sharedMaterial = mat;
            }
            l.AddGlow(c, 1.6f, 0.35f);
            l.AddIcon(ItemIcons.Get(5), 0.55f, 1.15f);
            Share(l, pos);
            return l;
        }

        public static Loot SpawnItem(Item it, Vector3 pos)
        {
            var l = Make(Kind.Item, pos, ItemDB.Col(it.rarity), 1.5f);
            l.item = it;
            l.vel *= 0.6f;
            l.groundY = 0f;
            BuildChest(it, l.vis);
            l.AddGlow(ItemDB.Col(it.rarity), 2.2f + it.rarity * 0.15f, 0.3f + it.rarity * 0.04f);
            l.AddIcon(ItemIcons.For(it), 0.85f, 1.75f);
            // raza de lumina colorata (cu atat mai inalta cu cat e mai rar)
            if (it.rarity < 2) { l.beam = null; goto noBeam; }
            l.beam = Art.Prim(PrimitiveType.Cylinder, Vector3.zero, Vector3.one, Color.white, l.transform, false, false);
            var br = l.beam.GetComponent<Renderer>();
            br.sharedMaterial = Art.GlowMaterial(Art.A(ItemDB.Col(it.rarity), 0.28f + it.rarity * 0.05f));
            br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float h = 3f + it.rarity * 2.5f;
            l.beam.transform.localScale = new Vector3(0.22f + it.rarity * 0.04f, h * 0.5f, 0.22f + it.rarity * 0.04f);
            l.beam.transform.localPosition = new Vector3(0, h * 0.5f, 0);
            l.beam.SetActive(false);
            noBeam:

            Sfx.Play(it.rarity >= 3 ? "ItemRare" : "ItemDrop", it.rarity >= 3 ? 0.8f : 0.5f, 1f, 0.05f);
            if (it.rarity >= 4 && Game.I != null)
                Game.I.Say(ItemDB.RarityNames[it.rarity].ToUpper() + " DROP! " + it.name, 4f);
            Share(l, pos);
            return l;
        }

        /// <summary>Cufarul nou: lemn cald, capac rotunjit, benzi de metal in culoarea raritatii
        /// (fier - Common, albastru - Rare, aur - Legendary, curcubeu - Mythic) si lacat cu piatra.</summary>
        static void BuildChest(Item it, Transform root)
        {
            int r = it.rarity;
            Color rc = ItemDB.Col(r);
            Color wood = r >= 4 ? new Color(0.36f, 0.2f, 0.13f) : new Color(0.55f, 0.34f, 0.18f);
            Color metal = r >= 5 ? new Color(0.95f, 0.35f, 0.55f) : r >= 4 ? new Color(1f, 0.78f, 0.3f) : r >= 2 ? new Color(0.45f, 0.65f, 1f) : new Color(0.62f, 0.64f, 0.68f);
            float s = 0.95f + r * 0.05f;
            var c = new GameObject("Chest").transform;
            c.SetParent(root, false);
            c.localScale = Vector3.one * s;
            var woodM = Art.SharedLit(wood);
            var metalM = GearVisuals.Mat(metal, 0.85f, 0.7f, r >= 4 ? metal * 0.18f : Color.black);
            System.Func<PrimitiveType, Vector3, Vector3, Material, Vector3, GameObject> P = (t, pos, sc, m, eu) =>
            {
                var g = Art.Prim(t, pos, sc, Color.white, c, false, true);
                g.transform.localRotation = Quaternion.Euler(eu);
                g.GetComponent<Renderer>().sharedMaterial = m;
                return g;
            };
            // corpul + capacul rotunjit (cilindru culcat, jumatatea de jos intra in corp)
            P(PrimitiveType.Cube, new Vector3(0, 0.27f, 0), new Vector3(0.9f, 0.54f, 0.6f), woodM, Vector3.zero);
            P(PrimitiveType.Cylinder, new Vector3(0, 0.54f, 0), new Vector3(0.6f, 0.45f, 0.36f), woodM, new Vector3(0, 0, 90f));
            // benzi de metal (corp + capac) si rama de jos
            for (int k = -1; k <= 1; k += 2)
            {
                P(PrimitiveType.Cube, new Vector3(k * 0.3f, 0.27f, 0), new Vector3(0.09f, 0.56f, 0.62f), metalM, Vector3.zero);
                P(PrimitiveType.Cylinder, new Vector3(k * 0.3f, 0.54f, 0), new Vector3(0.62f, 0.045f, 0.38f), metalM, new Vector3(0, 0, 90f));
            }
            P(PrimitiveType.Cube, new Vector3(0, 0.03f, 0), new Vector3(0.94f, 0.06f, 0.64f), metalM, Vector3.zero);
            // lacatul + piatra in culoarea raritatii
            P(PrimitiveType.Cube, new Vector3(0, 0.5f, 0.31f), new Vector3(0.18f, 0.2f, 0.05f), metalM, Vector3.zero);
            var gem = P(PrimitiveType.Cube, new Vector3(0, 0.5f, 0.345f), new Vector3(0.08f, 0.08f, 0.04f), GearVisuals.Mat(rc, 0.2f, 0.9f, rc * 0.5f), new Vector3(0, 0, 45f));
            if (r >= 5) gem.AddComponent<MythicHue>();
            if (r >= 4)
                for (int k = -1; k <= 1; k += 2)   // colturi aurite
                    P(PrimitiveType.Cube, new Vector3(k * 0.43f, 0.27f, 0.29f), new Vector3(0.06f, 0.56f, 0.06f), metalM, Vector3.zero);
            foreach (var rr in c.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        // ---------------------------------------------------------------
        //  Update
        // ---------------------------------------------------------------
        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            if (claimed)
            {
                // asteptam raspunsul gazdei; daca s-a pierdut conexiunea, il luam noi
                claimT += dt;
                if (claimT > 4f) { if (!Mp.ShareLoot) { claimed = false; Apply(Game.I.player); } else Destroy(gameObject); }
                return;
            }

            if (!landed)
            {
                vel.y -= 25f * dt;
                transform.position += vel * dt;
                vis.Rotate(300f * dt, 200f * dt, 0);
                if (transform.position.y <= groundY && vel.y < 0)
                {
                    var p = transform.position; p.y = groundY; transform.position = p;
                    if (bounces < 1 && kind != Kind.Item)
                    {
                        bounces++;
                        vel = new Vector3(vel.x * 0.4f, -vel.y * 0.35f, vel.z * 0.4f);
                    }
                    else
                    {
                        landed = true;
                        landedAt = age;
                        vis.localRotation = Quaternion.identity;
                        if (beam != null) beam.SetActive(true);
                        if (glow != null) glow.gameObject.SetActive(true);
                        if (icon != null) icon.gameObject.SetActive(true);
                        if (kind == Kind.Item)
                        {
                            Fx.Sparkle(transform.position + Vector3.up * 0.5f, col, 10, 0.6f);
                            Fx.Shake(0.05f);
                            if (item.rarity >= 4) { Fx.Pillar(transform.position, col, 20f, 1.6f); Fx.Ring(transform.position, col, 3f); }
                        }
                    }
                }
                return;
            }

            // iconita: pluteste si priveste spre camera
            if (icon != null)
            {
                var camT = Game.I != null && Game.I.cam != null ? Game.I.cam.transform : null;
                if (camT != null) icon.rotation = camT.rotation;
                var ip = icon.localPosition; ip.y = (kind == Kind.Item ? 1.75f : 1.15f) + Mathf.Sin(age * 2.2f) * 0.08f; icon.localPosition = ip;
            }
            // dispare daca nu-l iei (ultimele 3 secunde clipeste)
            float left = Life - (age - landedAt);
            if (left <= 0f) { Destroy(gameObject); return; }
            if (left < 3f)
            {
                bool on = Mathf.Repeat(age * (left < 1.2f ? 9f : 5f), 1f) > 0.35f;
                vis.gameObject.SetActive(on);
                if (icon != null) icon.gameObject.SetActive(on);
            }
            // plutire / rotatie
            if (kind == Kind.Item)
            {
                vis.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(age * 2.5f)) * 0.18f, 0);
                vis.Rotate(0, 40f * dt, 0);
                if (Random.value < dt * (1f + item.rarity)) Fx.Sparkle(transform.position + Vector3.up * Random.Range(0.3f, 2.5f), col, 1, 0.5f);
            }
            else
            {
                vis.Rotate(0, 220f * dt, 0);
                vis.localPosition = new Vector3(0, Mathf.Sin(age * 4f) * 0.1f, 0);
            }

            var g = Game.I;
            var pl = g != null ? g.player : null;
            if (pl == null || pl.dead) return;
            Vector3 to = pl.transform.position + Vector3.up * 0.8f - transform.position;
            float d = to.magnitude;
            // monedele se strang singure; restul doar cu MAGNET PASS (altfel [E] langa ele)
            bool auto = kind == Kind.Coin || pl.D.passMagnet;
            if (auto)
            {
                float magnet = kind == Kind.Coin ? 9f : 12f;
                if (age > 0.5f && d < magnet)
                    transform.position += to.normalized * (12f + (magnet - d) * 4f) * dt;
                if (d < 1.3f) Collect(pl);
            }
        }

        void Collect(Player p)
        {
            if (collected || claimed) return;
            if (netId != 0 && Mp.ShareLoot)
            {
                // il cerem gazdei: daca altcineva a fost mai rapid, nu-l primim
                claimed = true; claimT = 0f;
                vis.gameObject.SetActive(false);
                if (beam != null) beam.SetActive(false);
                Pickable.Remove(this);
                Mp.ClaimLoot(netId);
                return;
            }
            Apply(p);
        }

        void Apply(Player p)
        {
            if (collected || p == null) return;
            collected = true;
            var hud = Game.I.hud;
            switch (kind)
            {
                case Kind.Coin:
                    p.AddCoins(amount);
                    hud.Pop(transform.position + Vector3.up, "+" + Fmt.Num(amount), new Color(1f, 0.9f, 0.2f), 0.8f);
                    hud.PulseCoins();
                    Sfx.Play("Coin", 0.35f, 1.15f, 0.15f, 0.04f);
                    Fx.Sparkle(transform.position, new Color(1f, 0.9f, 0.3f), 3, 0.3f);
                    break;
                case Kind.Ore:
                    p.D.ore += (int)amount;
                    hud.Pop(transform.position + Vector3.up, "+" + amount + " ore", col, 0.8f);
                    Sfx.Play("Ore", 0.4f, 1.2f, 0.1f);
                    Fx.Sparkle(transform.position, col, 4, 0.3f);
                    break;
                case Kind.Item:
                    if (p.AddItem(item))   // inventar plin = vandut automat (AddItem anunta singur), fara toast de "item nou"
                    {
                        hud.ItemToast(item);
                        hud.Pop(transform.position + Vector3.up * 1.5f, item.name, ItemDB.Col(item.rarity), 1.1f);
                    }
                    Sfx.Play(item.rarity >= 3 ? "ItemRare" : "ItemDrop", 0.6f);
                    Fx.Sparkle(transform.position + Vector3.up, ItemDB.Col(item.rarity), 14, 0.8f);
                    p.SaveNow();
                    break;
            }
            Destroy(gameObject);
        }
    }


}
