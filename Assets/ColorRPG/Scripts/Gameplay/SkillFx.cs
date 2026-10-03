// =====================================================================
//  SkillFx.cs - efectele "vii" ale skill-urilor: proiectile (fireball,
//  ice shard), meteor, zone care lovesc in timp (arrow rain, blizzard,
//  poison), unde care se extind (nova, flame ring, thunder clap),
//  fulgere (chain lightning, life drain) si aure (shield, battle cry).
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public class SkillFx : MonoBehaviour
    {
        public enum Kind { Projectile, Meteor, Zone, Wave, Bolt, Aura, Orb }

        public Kind kind;
        public Player owner;
        public Color col = Color.white;
        public float mult = 1f;          // multiplicator de damage (x Damage-ul jucatorului)
        public float radius = 3f;
        public float life = 1f;
        public float slow, stun;         // efecte aplicate inamicilor loviti
        public Vector3 vel, target;
        public float tickEvery = 0.3f;
        public bool explode;             // proiectil: explodeaza in zona (altfel loveste o singura tinta)
        public string zoneStyle = "";    // "arrows", "snow", "poison"
        public Transform follow;         // aura / orb: urmareste acest obiect

        float t, tickT;
        Transform visual;
        Material mat;
        LineRenderer line;
        Vector3[] boltPts;
        readonly HashSet<Enemy> hitOnce = new HashSet<Enemy>();
        Vector3 start;

        // ---------------------------------------------------------------
        //  Fabrici
        // ---------------------------------------------------------------
        static SkillFx New(string n, Kind k, Player p, Color c, Vector3 pos)
        {
            var go = new GameObject("Skill_" + n);
            go.transform.position = pos;
            var f = go.AddComponent<SkillFx>();
            f.kind = k; f.owner = p; f.col = c; f.start = pos;
            return f;
        }

        Transform Ball(float size, Color c)
        {
            var go = Art.Prim(PrimitiveType.Sphere, Vector3.zero, Vector3.one * size, c, transform, false, false);
            var r = go.GetComponent<Renderer>();
            mat = Art.GlowMaterial(Art.A(Color.Lerp(c, Color.white, 0.35f), 0.9f));
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        public static SkillFx Projectile(Player p, Vector3 from, Vector3 dir, float speed, Color c, float size, float mult, bool explode, float radius, float slow)
        {
            var f = New("Projectile", Kind.Projectile, p, c, from);
            f.vel = dir.normalized * speed; f.mult = mult; f.explode = explode; f.radius = radius; f.slow = slow; f.life = 1.3f;
            f.visual = f.Ball(size, c);
            if (!explode) f.visual.localScale = new Vector3(size * 0.6f, size * 0.6f, size * 1.8f);   // cristal alungit
            f.transform.rotation = Quaternion.LookRotation(dir);
            return f;
        }

        public static SkillFx Meteor(Player p, Vector3 at, Color c, float mult, float radius)
        {
            var f = New("Meteor", Kind.Meteor, p, c, at + new Vector3(-5f, 22f, -3f));
            f.target = at; f.mult = mult; f.radius = radius; f.life = 0.95f; f.stun = 0.8f;
            f.visual = f.Ball(2.2f, c);
            Fx.Ring(at + Vector3.up * 0.1f, Art.A(c, 0.8f), radius, 46);   // unde va cadea
            return f;
        }

        public static SkillFx Zone(Player p, Vector3 at, Color c, string style, float dur, float radius, float mult, float tick, float slow)
        {
            var f = New("Zone", Kind.Zone, p, c, at);
            f.zoneStyle = style; f.life = dur; f.radius = radius; f.mult = mult; f.tickEvery = tick; f.slow = slow;
            Fx.Ring(at + Vector3.up * 0.1f, c, radius, 40);
            return f;
        }

        public static SkillFx Wave(Player p, Vector3 at, Color c, float radius, float mult, float slow, float stun, float dur = 0.45f)
        {
            var f = New("Wave", Kind.Wave, p, c, at);
            f.radius = radius; f.mult = mult; f.slow = slow; f.stun = stun; f.life = dur;
            // inelul vizibil care creste
            var go = Art.Prim(PrimitiveType.Cylinder, Vector3.up * 0.15f, new Vector3(1f, 0.04f, 1f), c, f.transform, false, false);
            f.mat = Art.GlowMaterial(Art.A(c, 0.55f));
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = f.mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.visual = go.transform;
            return f;
        }

        public static SkillFx Bolt(List<Vector3> pts, Color c, float width, float dur)
        {
            var f = New("Bolt", Kind.Bolt, null, c, pts[0]);
            f.life = dur;
            f.line = f.gameObject.AddComponent<LineRenderer>();
            f.line.useWorldSpace = true;
            f.line.widthMultiplier = width;
            f.line.material = Art.GlowMaterial(Color.Lerp(c, Color.white, 0.4f));
            f.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.line.numCapVertices = 2;
            f.boltPts = pts.ToArray();
            f.Jag();
            return f;
        }

        public static SkillFx Aura(Transform follow, Color c, float size, float dur)
        {
            var f = New("Aura", Kind.Aura, null, c, follow.position);
            f.follow = follow; f.life = dur;
            f.visual = f.Ball(size, c);
            Art.SetColor(f.mat, Art.A(c, 0.22f));
            return f;
        }

        /// <summary>Globulet care zboara de la o pozitie spre jucator (life drain).</summary>
        public static SkillFx Orb(Vector3 from, Transform to, Color c)
        {
            var f = New("Orb", Kind.Orb, null, c, from);
            f.follow = to; f.life = 1.2f;
            f.vel = new Vector3(Random.Range(-4f, 4f), Random.Range(3f, 7f), Random.Range(-4f, 4f));
            f.visual = f.Ball(0.35f, c);
            return f;
        }

        // ---------------------------------------------------------------
        //  Damage
        // ---------------------------------------------------------------
        void HitEnemy(Enemy e, float m)
        {
            if (e == null || e.Dying || owner == null) return;
            owner.SkillHit(e, m, col);
            if (slow > 0f) e.Slow(slow);
            if (stun > 0f) e.Stun(stun);
        }

        void Area(Vector3 c, float r, float m)
        {
            for (int i = Enemy.All.Count - 1; i >= 0; i--)
            {
                if (i >= Enemy.All.Count) continue;
                var e = Enemy.All[i];
                if (e == null || e.Dying) continue;
                Vector3 d = e.transform.position - c; d.y = 0;
                if (d.magnitude - e.Radius <= r) HitEnemy(e, m);
            }
        }

        void Jag()
        {
            var pts = new List<Vector3>();
            for (int i = 0; i < boltPts.Length - 1; i++)
            {
                Vector3 a = boltPts[i], b = boltPts[i + 1];
                int seg = Mathf.Max(3, Mathf.RoundToInt(Vector3.Distance(a, b) / 0.8f));
                for (int s = 0; s < seg; s++)
                {
                    Vector3 p = Vector3.Lerp(a, b, s / (float)seg);
                    if (s > 0) p += Random.insideUnitSphere * 0.45f;
                    pts.Add(p);
                }
            }
            pts.Add(boltPts[boltPts.Length - 1]);
            line.positionCount = pts.Count;
            line.SetPositions(pts.ToArray());
        }

        // ---------------------------------------------------------------
        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            float k = Mathf.Clamp01(t / life);
            switch (kind)
            {
                case Kind.Projectile:
                {
                    Vector3 prev = transform.position;
                    transform.position += vel * dt;
                    if (visual != null) visual.Rotate(0, 0, 720f * dt, Space.Self);
                    Fx.Ambient(transform.position + Random.insideUnitSphere * 0.25f, -vel * 0.08f + Vector3.up * 0.6f, Color.Lerp(col, Color.white, Random.value * 0.4f), Random.Range(0.25f, 0.5f), 0.35f, true);
                    Enemy hit = null;
                    foreach (var e in Enemy.All)
                    {
                        if (e == null || e.Dying || hitOnce.Contains(e)) continue;
                        Vector3 d = e.transform.position - transform.position; d.y = 0;
                        if (d.magnitude <= e.Radius + 0.7f) { hit = e; break; }
                    }
                    if (hit != null || t >= life)
                    {
                        if (explode)
                        {
                            Area(transform.position, radius, mult);
                            Fx.Poof(transform.position, col, 1.4f);
                            Fx.Hit(transform.position, col, true);
                            Fx.Ring(new Vector3(transform.position.x, start.y + 0.1f, transform.position.z), col, radius, 36);
                            Sfx.Play("BossSlam", 0.35f, 1.5f, 0.1f);
                            Fx.Shake(0.15f);
                            Destroy(gameObject);
                        }
                        else if (hit != null)
                        {
                            hitOnce.Add(hit);
                            HitEnemy(hit, mult);
                            Fx.Sparkle(hit.transform.position + Vector3.up, col, 10, 0.8f);
                            Destroy(gameObject);
                        }
                        else { Fx.Sparkle(transform.position, col, 6, 0.4f); Destroy(gameObject); }
                    }
                    break;
                }
                case Kind.Meteor:
                {
                    Vector3 from = start;
                    transform.position = Vector3.Lerp(from, target + Vector3.up * 0.8f, k * k);
                    if (visual != null) visual.Rotate(300f * dt, 200f * dt, 0);
                    for (int i = 0; i < 3; i++)
                        Fx.Ambient(transform.position + Random.insideUnitSphere * 0.9f, Vector3.up * 2f, Color.Lerp(col, new Color(1f, 0.9f, 0.4f), Random.value), Random.Range(0.5f, 1.1f), 0.5f, true);
                    if (t >= life)
                    {
                        Area(target, radius, mult);
                        Fx.Poof(target + Vector3.up * 0.5f, new Color(0.35f, 0.3f, 0.28f), 2.6f);
                        Fx.Hit(target + Vector3.up, col, true);
                        Fx.Fountain(target, col, 40);
                        Fx.Ring(target + Vector3.up * 0.1f, col, radius, 60);
                        Fx.Ring(target + Vector3.up * 0.1f, new Color(1f, 0.85f, 0.4f), radius * 0.6f, 40);
                        Fx.Pillar(target, col, 7f, 0.5f);
                        Fx.Shake(0.55f);
                        Fx.HitStop(0.05f);
                        Sfx.Play("BossSlam", 0.8f, 0.8f, 0.05f);
                        Destroy(gameObject);
                    }
                    break;
                }
                case Kind.Zone:
                {
                    // vizual: sageti / fulgi / nori de otrava
                    int n = zoneStyle == "poison" ? 2 : 4;
                    for (int i = 0; i < n; i++)
                    {
                        Vector2 r2 = Random.insideUnitCircle * radius;
                        Vector3 p = start + new Vector3(r2.x, 0f, r2.y);
                        if (zoneStyle == "arrows")
                            Fx.Ambient(p + Vector3.up * 9f, Vector3.down * 26f, new Color(0.95f, 0.85f, 0.6f), 0.28f, 0.33f, true);
                        else if (zoneStyle == "snow")
                            Fx.Ambient(p + Vector3.up * 7f, new Vector3(2f, -14f, 1f), Color.Lerp(col, Color.white, 0.5f), Random.Range(0.25f, 0.5f), 0.5f, true);
                        else
                            Fx.Ambient(p + Vector3.up * 0.4f, Vector3.up * Random.Range(0.3f, 1f), Art.A(col, 0.6f), Random.Range(0.9f, 1.6f), 1.1f, false);
                    }
                    tickT -= dt;
                    if (tickT <= 0f)
                    {
                        tickT = tickEvery;
                        Area(start, radius, mult);
                        if (zoneStyle == "arrows") { Fx.Poof(start + new Vector3(Random.Range(-radius, radius) * 0.6f, 0.2f, Random.Range(-radius, radius) * 0.6f), new Color(0.8f, 0.7f, 0.5f), 0.4f); Sfx.Play("SwingThrust", 0.2f, 1.6f, 0.2f); }
                        else Fx.Ring(start + Vector3.up * 0.1f, Art.A(col, 0.6f), radius, 28);
                    }
                    if (t >= life) Destroy(gameObject);
                    break;
                }
                case Kind.Wave:
                {
                    float r = radius * Mathf.Sin(k * Mathf.PI * 0.5f);
                    if (visual != null) visual.localScale = new Vector3(r * 2f, 0.04f, r * 2f);
                    if (mat != null) Art.SetColor(mat, Art.A(col, 0.6f * (1f - k)));
                    foreach (var e in Enemy.All.ToArray())
                    {
                        if (e == null || e.Dying || hitOnce.Contains(e)) continue;
                        Vector3 d = e.transform.position - start; d.y = 0;
                        if (d.magnitude - e.Radius <= r) { hitOnce.Add(e); HitEnemy(e, mult); }
                    }
                    if (Random.value < 0.8f)
                    {
                        float a = Random.Range(0f, Mathf.PI * 2f);
                        Fx.Ambient(start + new Vector3(Mathf.Cos(a) * r, 0.3f, Mathf.Sin(a) * r), Vector3.up * 2.5f, col, 0.5f, 0.4f, true);
                    }
                    if (t >= life) Destroy(gameObject);
                    break;
                }
                case Kind.Bolt:
                {
                    if (line != null)
                    {
                        if (Random.value < 0.5f) Jag();
                        line.widthMultiplier *= 1f - dt * 4f;
                        Color c = Color.Lerp(col, Color.white, 0.4f); c.a = 1f - k;
                        line.startColor = c; line.endColor = c;
                    }
                    if (t >= life) Destroy(gameObject);
                    break;
                }
                case Kind.Aura:
                {
                    if (follow == null) { Destroy(gameObject); break; }
                    transform.position = follow.position + Vector3.up * 1.1f;
                    float pulse = 1f + 0.06f * Mathf.Sin(t * 8f);
                    float fade = Mathf.Clamp01((life - t) / 0.4f) * Mathf.Clamp01(t / 0.2f);
                    if (visual != null) visual.localScale = Vector3.one * 3f * pulse * (0.6f + 0.4f * fade);
                    if (mat != null) Art.SetColor(mat, Art.A(col, 0.22f * fade));
                    if (Random.value < 0.35f) Fx.Ambient(follow.position + Random.insideUnitSphere * 1.2f + Vector3.up, Vector3.up * 1.5f, col, 0.3f, 0.6f, true);
                    if (t >= life) Destroy(gameObject);
                    break;
                }
                case Kind.Orb:
                {
                    if (follow == null) { Destroy(gameObject); break; }
                    Vector3 to = follow.position + Vector3.up * 1.2f;
                    vel = Vector3.Lerp(vel, (to - transform.position).normalized * 16f, dt * 5f);
                    transform.position += vel * dt;
                    Fx.Ambient(transform.position, Vector3.zero, col, 0.3f, 0.25f, true);
                    if ((to - transform.position).sqrMagnitude < 0.5f || t >= life) { Fx.Sparkle(to, col, 4, 0.5f); Destroy(gameObject); }
                    break;
                }
            }
        }
    }
}
