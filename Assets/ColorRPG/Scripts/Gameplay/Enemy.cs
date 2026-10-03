// =====================================================================
//  Enemy.cs - inamici si boss-uri (AI simplu, flash, knockback, drop-uri)
// =====================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> All = new List<Enemy>();

        [System.NonSerialized] public EnemyDef def;
        public string nm;
        public int hp, maxHp, dmg, xp, coins, mapIdx, lvl;
        public bool boss;
        public float speed = 3.5f, height = 2f;
        public Vector3 home;
        public Color baseCol;

        public bool Dying { get; private set; }
        public float Radius { get { return boss ? 1.6f : 0.6f; } }
        public float HitFlash { get { return flash; } }
        // efecte de la skill-urile eroilor
        float slowT, stunT, fxT;
        public void Slow(float t) { slowT = Mathf.Max(slowT, t); }
        public void Stun(float t) { stunT = Mathf.Max(stunT, boss ? t * 0.4f : t); }
        float Spd { get { return speed * (slowT > 0f ? 0.35f : 1f); } }
        public float LastHitTime { get; private set; }

        Transform vis;
        Vector3 baseScale;
        SimpleAnimator anim;
        readonly List<Material> mats = new List<Material>();
        float atk = 0.8f, flash, seed, windup, squash, wanderT, slamT = 6f;
        Vector3 knock, wanderGoal;
        bool slamming, awake = true, roared;
        public static readonly string[] MapSnd = { "Meadow", "Desert", "Frozen", "Lava" };
        string Snd { get { return MapSnd[Mathf.Clamp(mapIdx, 0, 3)]; } }
        float cullT, spawnT;
        float provokedT;     // a fost lovit -> te urmareste o vreme chiar daca esti mai departe
        bool chasing, returning;
        public int kind;     // tipul de monstru pe harta (0..2) - fiecare are zona lui
        float hintT;
        GameObject warn;

        void OnEnable() { All.Add(this); seed = Random.value * 10f; wanderGoal = transform.position; }
        void OnDisable() { All.Remove(this); Unregister(); }

        // ---------------------------------------------------------------
        //  Multiplayer: fiecare monstru are un id comun (harta, zona, loc)
        // ---------------------------------------------------------------
        public int netId, slot;
        [System.NonSerialized] public float netSeen;
        static readonly Dictionary<int, Enemy> byId = new Dictionary<int, Enemy>();
        Vector3 netPos; float netYaw; bool netWind, netInit;
        public static Enemy ById(int id) { Enemy e; return id != 0 && byId.TryGetValue(id, out e) && e != null && !e.Dying ? e : null; }
        public static int MakeId(int map, bool boss, int kind, int slot) { return map * 1000 + (boss ? 999 : Mathf.Clamp(kind, 0, 9) * 100 + Mathf.Clamp(slot, 0, 98)) + 1; }
        void Unregister() { Enemy x; if (netId != 0 && byId.TryGetValue(netId, out x) && x == this) byId.Remove(netId); }
        /// <summary>Flag-uri pentru clienti: 1 = pregateste lovitura, 2 = urmareste pe cineva.</summary>
        public byte NetFlags { get { return (byte)((windup > 0f || slamming ? 1 : 0) | (chasing ? 2 : 0)); } }

        // ---------------------------------------------------------------
        //  Constructie
        // ---------------------------------------------------------------
        public static Enemy Spawn(int mi, EnemyDef d, bool boss, Vector3 pos, int id = 0)
        {
            if (id != 0 && ById(id) != null) return null;   // exista deja (multiplayer)
            var m = GameData.Maps[mi];
            var go = new GameObject(d.name);
            go.transform.SetParent(SceneGroups.Get("Enemies"), false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            var e = go.AddComponent<Enemy>();
            e.def = d;
            e.boss = boss;
            e.nm = d.name;
            e.mapIdx = mi;
            e.home = pos;
            e.baseCol = d.col;
            e.lvl = m.req + (boss ? 4 : Random.Range(0, 3));

            float hpM = d.hpMul * (boss ? 12f : 1f);
            e.hp = e.maxHp = Mathf.RoundToInt(m.hp * hpM * Random.Range(0.9f, 1.1f));
            e.dmg = Mathf.RoundToInt(m.dmg * d.dmgMul * (boss ? 2f : 1f));
            e.xp = Mathf.RoundToInt(m.xp * d.xpMul * (boss ? 10f : 1f));
            e.coins = Mathf.RoundToInt(m.coins * d.coinMul * (boss ? 12f : 1f));
            e.speed = d.speed * (boss ? 0.9f : 1f);
            e.netId = id;
            if (id != 0) byId[id] = e;
            e.Build();
            if (Time.timeSinceLevelLoad > 3f)
            {
                Fx.Poof(pos + Vector3.up * 0.5f, d.col, boss ? 2.5f : 1f);
                if (boss) Fx.Pillar(pos, d.col, 14f, 1.2f);
                if (boss) Sfx.PlayNear("BossSpawn", pos, 45f, 0.9f);
            }
            return e;
        }

        void Build()
        {
            float sc = boss ? 2.4f : 1f;
            vis = new GameObject("Vis").transform;
            vis.SetParent(transform, false);

            Transform model = null;
            if (!string.IsNullOrEmpty(def.model)) model = ModelLib.Spawn(def.model, vis, def.height, def.yaw);
            if (model != null)
            {
                anim = SimpleAnimator.Setup(model, def.model);
                if (anim != null) anim.Play("idle", 0f);
                mats.AddRange(ModelLib.OwnMaterials(model));
                height = def.height;
            }
            else BuildPrimitive();

            vis.localScale = Vector3.one * sc;
            baseScale = vis.localScale;
            height = (height + 0.5f) * sc;

            if (boss)
            {
                // coroana aurie deasupra capului
                var crown = new GameObject("Crown").transform;
                crown.SetParent(vis, false);
                crown.localPosition = new Vector3(0, (height / sc) - 0.35f, 0);
                Color gold = new Color(1f, 0.82f, 0.2f);
                Art.Prim(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.7f, 0.12f, 0.7f), gold, crown, false);
                for (int i = 0; i < 5; i++)
                {
                    float a = i / 5f * Mathf.PI * 2f;
                    var sp = Art.Prim(PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 0.3f, 0.2f, Mathf.Sin(a) * 0.3f), new Vector3(0.14f, 0.3f, 0.14f), gold, crown, false);
                    sp.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 45f);
                }
                Art.Prim(PrimitiveType.Sphere, new Vector3(0, 0.2f, 0.33f), Vector3.one * 0.14f, new Color(1f, 0.2f, 0.4f), crown, false);
                var sp2 = crown.gameObject.AddComponent<Spinner>();
                sp2.rot = new Vector3(0, 40, 0);
            }

            // umbra blob
            var blob = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.03f, 0), new Vector3(1.4f, 0.01f, 1.4f) * sc, Color.black, transform, false, false);
            var br = blob.GetComponent<Renderer>();
            br.sharedMaterial = Art.ShadowBlob();
            br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
        }

        void BuildPrimitive()
        {
            Color col = baseCol;
            Color dark = Art.Dk(col, 0.6f);
            GameObject body;
            float eyeY, eyeZ, h;
            if (def.shape == Shape.Slime)
            {
                body = Art.Prim(PrimitiveType.Sphere, new Vector3(0, 0.65f, 0), new Vector3(1.7f, 1.3f, 1.7f), col, vis, false, false);
                eyeY = 0.85f; eyeZ = 0.75f; h = 1.3f;
            }
            else if (def.shape == Shape.Cube)
            {
                body = Art.Prim(PrimitiveType.Cube, new Vector3(0, 0.6f, 0), new Vector3(1.8f, 1.1f, 1.6f), col, vis, false, false);
                Art.Prim(PrimitiveType.Cube, new Vector3(-1.15f, 0.5f, 0.95f), new Vector3(0.5f, 0.4f, 1.0f), dark, vis, false);
                Art.Prim(PrimitiveType.Cube, new Vector3(1.15f, 0.5f, 0.95f), new Vector3(0.5f, 0.4f, 1.0f), dark, vis, false);
                Art.Prim(PrimitiveType.Cube, new Vector3(0, 1.0f, -0.9f), new Vector3(0.35f, 0.35f, 1.2f), dark, vis, false);
                eyeY = 0.9f; eyeZ = 0.82f; h = 1.2f;
            }
            else
            {
                body = Art.Prim(PrimitiveType.Capsule, new Vector3(0, 1.6f, 0), Vector3.one * 1.6f, col, vis, false, false);
                Art.Prim(PrimitiveType.Sphere, new Vector3(-1.1f, 1.8f, 0.1f), Vector3.one * 0.9f, dark, vis, false);
                Art.Prim(PrimitiveType.Sphere, new Vector3(1.1f, 1.8f, 0.1f), Vector3.one * 0.9f, dark, vis, false);
                eyeY = 2.3f; eyeZ = 0.78f; h = 3.2f;
            }
            for (int s = -1; s <= 1; s += 2)
            {
                Art.Prim(PrimitiveType.Sphere, new Vector3(s * 0.32f, eyeY, eyeZ), Vector3.one * 0.36f, Color.white, vis, false);
                Art.Prim(PrimitiveType.Sphere, new Vector3(s * 0.32f, eyeY, eyeZ + 0.14f), Vector3.one * 0.18f, Color.black, vis, false);
            }
            var bm = body.GetComponent<Renderer>().sharedMaterial;
            if (bm.HasProperty("_Smoothness")) bm.SetFloat("_Smoothness", 0.7f);
            if (bm.HasProperty("_Glossiness")) bm.SetFloat("_Glossiness", 0.7f);
            mats.Add(bm);
            height = h;
        }

        // ---------------------------------------------------------------
        //  AI
        // ---------------------------------------------------------------
        void Update()
        {
            if (Dying) return;
            float dt = Time.deltaTime;
            var g = Game.I;
            var p = g != null ? g.player : null;
            if (p == null) return;

            // inamicii de pe alte harti / foarte departe dorm (economie de CPU)
            cullT -= dt;
            if (cullT <= 0f)
            {
                cullT = 0.4f + Random.value * 0.2f;
                bool near = (transform.position - p.transform.position).sqrMagnitude < 95f * 95f || (Mp.HostOn(mapIdx) && Mp.AnyPeerNear(transform.position, 95f));
                if (near != awake) { awake = near; vis.gameObject.SetActive(near); }
            }
            if (!awake) return;

            // squash & stretch + respiratie
            squash = Mathf.MoveTowards(squash, 0f, dt * 4f);
            float breathe = 1f + 0.05f * Mathf.Sin(Time.time * 5f + seed);
            float sq = squash * Mathf.Sin(squash * 20f);
            spawnT += dt;
            float sk = spawnT >= 0.45f ? 1f : EaseOutBack(spawnT / 0.45f);
            vis.localScale = new Vector3(baseScale.x * (breathe + sq * 0.25f), baseScale.y * (breathe - sq * 0.3f), baseScale.z * (breathe + sq * 0.25f)) * sk;

            if (flash > 0f)
            {
                flash -= dt;
                Color em = flash > 0f ? Color.white * (flash / 0.12f) * 0.9f : Color.black;
                foreach (var m in mats) Art.SetEmission(m, em);
            }

            // multiplayer: monstrul e miscat de gazda camerei
            if (Mp.ClientOn(mapIdx) && netId != 0) { NetFollow(dt); return; }

            // knockback
            if (knock.sqrMagnitude > 0.001f)
            {
                transform.position += knock * dt;
                knock = Vector3.Lerp(knock, Vector3.zero, 1f - Mathf.Exp(-9f * dt));
            }

            // incetinit (gheata) / ametit (stun)
            fxT -= dt;
            if (slowT > 0f) { slowT -= dt; if (fxT <= 0f) { fxT = 0.25f; Fx.Sparkle(transform.position + Vector3.up * height * 0.6f, new Color(0.6f, 0.9f, 1f), 1, 0.4f); } }
            if (stunT > 0f)
            {
                stunT -= dt;
                if (fxT <= 0f) { fxT = 0.2f; Fx.Sparkle(transform.position + Vector3.up * (height + 0.3f), new Color(1f, 0.95f, 0.4f), 1, 0.4f); }
                windup = 0f;
                return;
            }

            // tinta: tu, sau (daca esti gazda) cel mai apropiat jucator din camera
            Vector3 tpos = p.transform.position; bool tdead = p.dead, tlocal = true; ulong tid = 0;
            if (Mp.HostOn(mapIdx))
                Mp.NearestTarget(transform.position, g.mapIndex == mapIdx && !g.InPlaza ? p : null, out tpos, out tdead, out tid, out tlocal);
            Vector3 to = tpos - transform.position; to.y = 0;
            float d = to.magnitude;
            // raza de actiune: fiecare tip de monstru are raza lui; boss-ul lupta DOAR in arena lui
            float aggro = def != null ? def.Aggro : 9f;
            float reach = Radius + 1.5f;
            Vector3 fromHome = transform.position - home; fromHome.y = 0;
            bool leashed;
            if (provokedT > 0f) provokedT -= dt;
            if (boss)
            {
                bool inArena = GameData.InArena(mapIdx, tpos) && (!tlocal || (g.mapIndex == mapIdx && !g.InPlaza));
                chasing = inArena && !tdead;
                Vector3 fa = transform.position - GameData.Maps[mapIdx].Arena; fa.y = 0;
                leashed = !chasing && fromHome.magnitude > 1.5f;
                if (fa.magnitude > GameData.ArenaRadius - 2f) { Vector3 c = GameData.Maps[mapIdx].Arena + fa.normalized * (GameData.ArenaRadius - 2f); c.y = 0; transform.position = c; }
            }
            else
            {
                if (returning && fromHome.magnitude < 2.5f) returning = false;
                if (!chasing && !tdead && ((d < aggro && !returning) || provokedT > 0f)) chasing = true;
                if (chasing && (tdead || (d > aggro * 1.8f && provokedT <= 0f) || fromHome.magnitude > aggro + 12f))
                {
                    chasing = false;
                    if (fromHome.magnitude > aggro + 12f) { returning = true; provokedT = 0f; }   // s-a departat prea mult de zona lui -> se intoarce
                }
                leashed = !chasing && (returning || fromHome.magnitude > 7f);
            }
            bool moving = false;

            if (chasing)
            {
                if (boss && !roared)
                {
                    // raget la inceputul luptei (fiecare boss are vocea lui)
                    roared = true;
                    Sfx.Play("BossRoar" + Mathf.Clamp(mapIdx, 0, 3), 0.95f, 1f, 0.04f);
                    Fx.Shake(0.35f);
                }
                if (boss) BossSkills(tpos, d, dt);
                if (slamming) { /* sta pe loc in timpul slam-ului */ }
                else if (windup > 0f)
                {
                    windup -= dt;
                    if (windup <= 0f)
                    {
                        if (d <= reach + 0.8f)
                        {
                            int amount = Mathf.Max(1, Mathf.RoundToInt(dmg * Random.Range(0.8f, 1.2f)));
                            if (tlocal) p.TakeDamage(amount); else Mp.SendDamage(tid, amount);
                        }
                        atk = boss ? 1.4f : 1.3f;
                    }
                }
                else if (d > reach)
                {
                    transform.position += to / d * Spd * dt;
                    moving = true;
                }
                else
                {
                    atk -= dt;
                    if (atk <= 0f)
                    {
                        windup = 0.35f;      // mica "pregatire" vizibila inainte de lovitura
                        squash = 0.6f;
                        if (anim != null && def.humanoid) anim.Play("attack-melee-right", 0.05f, false, true);
                    }
                }
                if (d > 0.1f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 1f - Mathf.Exp(-10f * dt));
            }
            else
            {
                // plimbare in jurul casei
                wanderT -= dt;
                if (wanderT <= 0f || leashed)
                {
                    wanderT = Random.Range(2.5f, 5f);
                    wanderGoal = home + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
                    if (leashed) wanderGoal = home;
                }
                Vector3 w = wanderGoal - transform.position; w.y = 0;
                if (w.magnitude > 0.6f)
                {
                    transform.position += w.normalized * Spd * (leashed ? 1.2f : 0.45f) * dt;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(w), 1f - Mathf.Exp(-5f * dt));
                    moving = true;
                }
                if (leashed && hp < maxHp) hp = Mathf.Min(maxHp, hp + Mathf.CeilToInt(maxHp * 0.3f * dt));
            }

            // separare intre inamici (sa nu se suprapuna)
            foreach (var o in All)
            {
                if (o == this || o == null) continue;
                Vector3 dd = transform.position - o.transform.position; dd.y = 0;
                float min = Radius + o.Radius + 0.3f;
                float m2 = dd.sqrMagnitude;
                if (m2 < min * min && m2 > 0.0001f)
                    transform.position += dd.normalized * (min - Mathf.Sqrt(m2)) * 0.5f;
            }
            var pos = transform.position; pos.y = 0f;
            Obstacle.PushOut(ref pos, Radius * 0.8f);   // nu intra in copaci / stanci / cactusi
            transform.position = pos;

            // animatii
            if (anim != null && windup <= 0f && (!def.humanoid || anim.Done || anim.Current == "idle" || anim.Current == "walk" || anim.Current == "run" || anim.Current == "sprint"))
            {
                string mv = moving ? (d < aggro && anim.Has("run") ? "run" : (anim.Has("walk") ? "walk" : "idle")) : "idle";
                if (moving && d < aggro && def.humanoid && anim.Has("sprint") && speed > 3.8f) mv = "sprint";
                anim.Play(mv, 0.15f);
            }
            else if (anim == null && moving)
            {
                // topaiala pentru primitive
                float hop = Mathf.Abs(Mathf.Sin(Time.time * 8f + seed)) * 0.25f;
                vis.localPosition = new Vector3(0, hop, 0);
            }
        }

        // ---------------------------------------------------------------
        //  Boss: atac "slam" in zona, cu avertizare pe sol
        // ---------------------------------------------------------------
        void BossSkills(Vector3 tpos, float d, float dt)
        {
            if (slamming) return;
            slamT -= dt;
            if (slamT <= 0f && d < 12f && GameData.InArena(mapIdx, tpos))
            {
                slamT = Random.Range(6f, 9f);
                if (Mp.HostOn(mapIdx)) Mp.SendSlam(netId);
                StartCoroutine(Slam(Game.I.player, true));
            }
        }

        /// <summary>Client: gazda a pornit slam-ul (cercul rosu) - te loveste doar gazda, aici e doar efectul.</summary>
        public void NetSlam() { if (!Dying && !slamming) StartCoroutine(Slam(Game.I.player, false)); }

        IEnumerator Slam(Player p, bool deal)
        {
            slamming = true;
            const float R = 6.5f;
            Vector3 c = transform.position;
            warn = Art.Prim(PrimitiveType.Cylinder, c + Vector3.up * 0.05f, new Vector3(0.1f, 0.01f, 0.1f), Color.red, null, false, false);
            var wr = warn.GetComponent<Renderer>();
            wr.sharedMaterial = Art.GlowMaterial(new Color(1f, 0.15f, 0.1f, 0.45f));
            wr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Game.I.Say(nm + " is charging an attack! Get out of the red circle!", 1.5f);
            float t = 0f;
            const float T = 1.2f;
            while (t < T && !Dying)
            {
                t += Time.deltaTime;
                float k = t / T;
                warn.transform.localScale = new Vector3(R * 2f * k, 0.01f, R * 2f * k);
                squash = 0.3f * k;
                vis.localPosition = new Vector3(0, Mathf.Sin(k * Mathf.PI) * 1.5f, 0);
                yield return null;
            }
            vis.localPosition = Vector3.zero;
            if (warn != null) Destroy(warn);
            if (!Dying)
            {
                squash = 1f;
                Fx.Ring(c, baseCol, R * 0.9f, 50);
                Fx.Poof(c, new Color(0.8f, 0.7f, 0.6f), 2f);
                Fx.Shake(0.5f);
                Sfx.Play("BossSlam", 1f, 1f, 0.05f);
                Vector3 dd = p.transform.position - c; dd.y = 0;
                bool here = Game.I.mapIndex == mapIdx && !Game.I.InPlaza;
                if (deal && here && !p.dead && dd.magnitude < R) p.TakeDamage(Mathf.RoundToInt(dmg * 1.8f));
                if (deal && Mp.HostOn(mapIdx)) Mp.DamagePeersInRadius(c, R, Mathf.RoundToInt(dmg * 1.8f));
            }
            slamming = false;
        }

        // ---------------------------------------------------------------
        //  Lovit / moarte
        // ---------------------------------------------------------------
        public void Hit(int d, bool crit, Vector3 from, bool fromPet = false)
        {
            if (Dying) return;
            // boss-ul poate fi lovit doar din interiorul arenei
            if (boss && !GameData.InArena(mapIdx, Game.I.player.transform.position))
            {
                if (Time.time > hintT) { hintT = Time.time + 2f; Game.I.hud.Pop(transform.position + Vector3.up * (height + 0.5f), "ENTER THE ARENA!", new Color(1f, 0.5f, 0.4f), 1.1f); }
                return;
            }
            bool netClient = Mp.ClientOn(mapIdx) && netId != 0;
            provokedT = 6f;
            hp -= d;
            if (netClient) hp = Mathf.Max(0, hp);   // viata reala o tine gazda
            flash = 0.12f;
            squash = crit ? 1f : 0.6f;
            LastHitTime = Time.time;
            Vector3 k = transform.position - from; k.y = 0;
            if (k.sqrMagnitude > 0.001f) knock = k.normalized * (boss ? 1.5f : (crit ? 9f : 5f)) * (fromPet ? 0.3f : 1f);

            var g = Game.I;
            Color popCol = fromPet ? new Color(0.8f, 0.95f, 1f) : crit ? new Color(1f, 0.6f, 0.1f) : Color.white;
            g.hud.Pop(transform.position + Vector3.up * height, (crit ? "CRIT " : "") + Fmt.Num(d), popCol, crit ? 1.5f : (fromPet ? 0.75f : 1f));
            Fx.Hit(transform.position + Vector3.up * height * 0.45f, baseCol, crit);
            if (!fromPet)
            {
                // lovitura: taietura (sabie, sabie curbata, sulita) sau izbitura (topor, ciocan, spadon)
                //  + textura hartii (slime umed, nisip, gheata, lava) + "shing" la crit
                var wk = ItemDB.KindOf(g.player.Equipped(Slot.Weapon));
                bool blunt = wk == ItemDB.WeaponKind.Hammer || wk == ItemDB.WeaponKind.Axe || wk == ItemDB.WeaponKind.Greatsword;
                Sfx.Play(blunt ? "HitBlunt" : "HitBlade", crit ? 0.8f : 0.6f, blunt ? 0.95f : 1.05f, 0.1f, 0.04f);
                Sfx.Play("HitTex" + Snd, 0.4f, 1f, 0.12f, 0.06f);
                if (crit) Sfx.Play("CritHit", 0.85f, 1f, 0.06f, 0.05f);
                if (crit) { Fx.HitStop(0.045f); Fx.Shake(0.22f); }
            }
            if (netClient) { Mp.SendHit(netId, d, crit); return; }
            if (hp <= 0)
            {
                if (Mp.HostOn(mapIdx)) Mp.SendKill(netId, Net.MpLink.LocalId);
                DieBy(true, null);
            }
        }

        /// <summary>Gazda: alt jucator a lovit monstrul. Cine da ultima lovitura ia kill-ul.</summary>
        public void NetHit(int d, bool crit, ulong from)
        {
            if (Dying) return;
            provokedT = 6f;
            hp -= d;
            flash = 0.12f;
            squash = crit ? 1f : 0.6f;
            LastHitTime = Time.time;
            Game.I.hud.Pop(transform.position + Vector3.up * height, Fmt.Num(d), new Color(0.75f, 0.82f, 0.95f), 0.75f);
            Fx.Hit(transform.position + Vector3.up * height * 0.45f, baseCol, crit);
            if (hp <= 0)
            {
                Mp.SendKill(netId, from);
                DieBy(false, Mp.NameOf(from));
            }
        }

        /// <summary>Client: gazda spune ca monstrul a murit (mine = tu ai dat ultima lovitura).</summary>
        public void NetKill(bool mine, string by)
        {
            if (Dying) return;
            hp = 0;
            DieBy(mine, by);
        }

        /// <summary>Client: monstrul nu mai exista la gazda -> dispare fara recompensa.</summary>
        public void NetVanish()
        {
            if (Dying) return;
            Dying = true;
            All.Remove(this); Unregister();
            if (warn != null) Destroy(warn);
            Fx.Poof(transform.position + Vector3.up * 0.6f, baseCol, boss ? 2f : 1f);
            Destroy(gameObject);
        }

        /// <summary>Client: pozitia / viata de la gazda.</summary>
        public void NetSnap(Vector3 pos, float yaw, int hpNow, int hpMax, byte fl)
        {
            netSeen = Time.time;
            if (!netInit || (pos - transform.position).sqrMagnitude > 12f * 12f) { netInit = true; transform.position = pos; }
            netPos = pos; netYaw = yaw;
            maxHp = Mathf.Max(1, hpMax);
            hp = Time.time - LastHitTime < 0.4f ? Mathf.Min(hp, hpNow) : hpNow;
            bool wind = (fl & 1) != 0;
            if (wind && !netWind)
            {
                squash = 0.6f;
                if (anim != null && def.humanoid) anim.Play("attack-melee-right", 0.05f, false, true);
            }
            netWind = wind;
            chasing = (fl & 2) != 0;
        }

        void NetFollow(float dt)
        {
            if (!netInit) return;
            Vector3 delta = netPos - transform.position; delta.y = 0;
            transform.position = Vector3.Lerp(transform.position, netPos, 1f - Mathf.Exp(-10f * dt));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, netYaw, 0), 1f - Mathf.Exp(-10f * dt));
            bool moving = delta.magnitude > 0.15f;
            if (anim != null && !netWind && (!def.humanoid || anim.Done || anim.Current == "idle" || anim.Current == "walk" || anim.Current == "run" || anim.Current == "sprint"))
                anim.Play(moving ? (chasing && anim.Has("run") ? "run" : (anim.Has("walk") ? "walk" : "idle")) : "idle", 0.15f);
            else if (anim == null && moving)
                vis.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(Time.time * 8f + seed)) * 0.25f, 0);
        }

        /// <summary>ADMIN: omoara monstrul (si boss-ul din afara arenei).</summary>
        public void AdminKill() { if (Dying) return; hp = 0; if (Mp.HostOn(mapIdx)) Mp.SendKill(netId, Net.MpLink.LocalId); DieBy(true, null); }

        /// <summary>Moartea monstrului. mine = recompensele (XP, monede, loot) sunt ale tale; by = cine l-a omorat (altfel).</summary>
        void DieBy(bool mine, string by)
        {
            Dying = true;
            All.Remove(this); Unregister();
            if (warn != null) Destroy(warn);
            var g = Game.I;
            var m = GameData.Maps[mapIdx];
            var p = g.player;
            if (!mine)
            {
                // altcineva a dat ultima lovitura: fara recompense pentru tine
                if (boss && g.mapIndex == mapIdx && !g.InPlaza) g.Say(nm + " was defeated by " + (by ?? "another player") + "!", 3.5f);
                Sfx.Play("Die" + Snd, boss ? 0.9f : 0.45f, boss ? 0.7f : 1f, 0.08f, 0.05f);
                if (!Mp.ClientOn(mapIdx)) g.StartCoroutine(g.RespawnRoutine(mapIdx, boss, kind, slot));
                StartCoroutine(DeathAnim());
                return;
            }

            p.GainXp(Mathf.RoundToInt(xp * p.XpBoost));
            p.QuestKill(this);
            p.MaterialDrop(this);
            p.D.kills++;
            g.hud.Pop(transform.position + Vector3.up * (height + 0.8f), "+" + Fmt.Num(xp) + " XP", new Color(0.4f, 0.9f, 1f), 0.9f);

            Vector3 c = transform.position;
            int drops = boss ? 10 : Random.Range(1, 3);
            int total = Mathf.RoundToInt(coins * Random.Range(0.7f, 1.3f) * p.CoinMul);
            for (int i = 0; i < drops; i++)
                Loot.SpawnCoin(Mathf.Max(1, total / drops), c);

            if (boss)
            {
                for (int i = 0; i < 6; i++) Loot.SpawnOre(2 + m.tier, c, m.oreCol);
                int n = Random.Range(1, 3);
                for (int i = 0; i < n; i++) Loot.SpawnItem(ItemDB.RandomDrop(m.tier, 2f + 0.2f * p.D.rebirth, 1, 0.6f), c);
                p.AddShards(1 + m.tier, "boss " + nm);
                g.hud.BigText("BOSS DEFEATED!", new Color(1f, 0.85f, 0.2f));
                g.Say(nm + " has been defeated!", 4f);
                Sfx.Play("JingleBoss", 0.8f, 1f, 0f);
                p.D.bossKills++;
                Fx.Shake(0.6f);
                Fx.HitStop(0.12f);
            }
            else
            {
                if (Random.value < 0.5f) Loot.SpawnOre(Random.Range(1, 3), c, m.oreCol);
                float itemChance = 0.05f + 0.006f * p.D.rebirth;   // itemele pica rar (nu le ai "la discretie")
                if (Random.value < itemChance) Loot.SpawnItem(ItemDB.RandomDrop(m.tier, 0.1f * p.D.rebirth, 0, 0.2f), c);
            }

            Sfx.Play("Die" + Snd, boss ? 0.9f : 0.55f, boss ? 0.7f : 1f, 0.08f, 0.05f);
            if (boss) Sfx.Play("BossDie", 1f, 1f, 0f);
            if (!Mp.ClientOn(mapIdx)) g.StartCoroutine(g.RespawnRoutine(mapIdx, boss, kind, slot));   // client: reapare de la gazda
            StartCoroutine(DeathAnim());
        }

        IEnumerator DeathAnim()
        {
            float sc = boss ? 2.4f : 1f;
            if (anim != null && anim.Has("die"))
            {
                anim.Speed = 1.3f;
                anim.Play("die", 0.05f, false, true);
                foreach (var m in mats) Art.SetEmission(m, Color.white * 0.5f);
                yield return new WaitForSeconds(0.7f);
            }
            else
            {
                // squash rapid
                float t = 0f;
                while (t < 0.15f)
                {
                    t += Time.deltaTime;
                    float k = t / 0.15f;
                    vis.localScale = new Vector3(baseScale.x * (1f + k * 0.5f), baseScale.y * (1f - k * 0.7f), baseScale.z * (1f + k * 0.5f));
                    yield return null;
                }
            }
            Fx.Poof(transform.position + Vector3.up * 0.6f * sc, baseCol, sc);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (warn != null) Destroy(warn);
        }
    }
}
