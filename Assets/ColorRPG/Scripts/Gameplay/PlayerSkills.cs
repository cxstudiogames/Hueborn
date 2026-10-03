// =====================================================================
//  PlayerSkills.cs - cele 3 skill-uri ale eroului (1 / 2 / 3 sau bara de
//  pe HUD). Fiecare erou are combinatia lui (SkillDB.HeroSkills), fiecare
//  skill are animatia si efectele lui. Damage-ul = Damage-ul jucatorului x mult.
// =====================================================================
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public partial class Player
    {
        readonly float[] skillCd = new float[3];
        float shieldT, cryT, furyT;
        // animatii speciale
        float spinT, spinHit2 = -1f; Quaternion spinBase;
        float dashT; Vector3 dashDir; readonly HashSet<Enemy> dashHit = new HashSet<Enemy>();
        bool slamPending; float slamT, slamMult, slamRadius, slamStun; Vector3 leapVel; Color slamCol;

        public SkillKind[] Skills { get { return SkillDB.ForHero(D != null ? D.hero : 0); } }
        public float SkillCooldownLeft(int i) { return skillCd[i]; }
        public void ResetSkillCooldowns() { for (int i = 0; i < 3; i++) skillCd[i] = 0f; }
        public float SkillCooldownFrac(int i) { var d = SkillDB.Get(Skills[i]); return d.cooldown > 0f ? Mathf.Clamp01(skillCd[i] / d.cooldown) : 0f; }
        public bool ShieldOn { get { return shieldT > 0f; } }
        public bool CryOn { get { return cryT > 0f; } }
        public bool FuryOn { get { return furyT > 0f; } }
        /// <summary>Bonusul Battle Cry (nu intra in POWER).</summary>
        public float SkillDmgBoost { get { return cryT > 0f ? 1.5f : 1f; } }
        /// <summary>Animatie de skill in desfasurare - miscarea normala e blocata.</summary>
        public bool SkillBusy { get { return dashT > 0f || slamPending; } }

        /// <summary>Opreste animatiile de skill in desfasurare (la moarte / teleport).</summary>
        public void ResetSkillState()
        {
            dashT = 0f; slamPending = false; leapVel = Vector3.zero;
            if (spinT > 0f && model != null) model.localRotation = spinBase;
            spinT = 0f; spinHit2 = -1f;
        }

        void TickSkills(float dt, bool uiOpen)
        {
            for (int i = 0; i < 3; i++) if (skillCd[i] > 0f) skillCd[i] -= dt;
            if (shieldT > 0f) shieldT -= dt;
            if (cryT > 0f) cryT -= dt;
            if (furyT > 0f) furyT -= dt;

            if (!uiOpen) for (int i = 0; i < 3; i++) if (Inp.SkillDown(i)) UseSkill(i);

            // Whirlwind: modelul se invarte
            if (spinT > 0f && model != null)
            {
                spinT -= dt;
                model.localRotation = spinBase * Quaternion.Euler(0, (0.6f - spinT) * 1500f, 0);
                if (Random.value < 0.7f) Fx.Ambient(transform.position + Quaternion.Euler(0, Random.Range(0, 360f), 0) * Vector3.forward * Range * 0.8f + Vector3.up, Vector3.up, new Color(0.85f, 0.9f, 1f), 0.35f, 0.25f, true);
                if (spinHit2 > 0f && spinT <= spinHit2) { spinHit2 = -1f; SpinHit(); }
                if (spinT <= 0f) model.localRotation = spinBase;
            }

            // Dash: alunecare rapida inainte, loveste tot ce atinge
            if (dashT > 0f)
            {
                dashT -= dt;
                cc.Move(dashDir * 40f * dt);
                Fx.Ambient(transform.position + Vector3.up * 1f + Random.insideUnitSphere * 0.4f, -dashDir * 2f, new Color(0.8f, 0.85f, 1f), 0.6f, 0.3f, true);
                if (trail != null) trail.emitting = true;
                foreach (var e in Enemy.All.ToArray())
                {
                    if (e == null || e.Dying || dashHit.Contains(e)) continue;
                    if (FlatDist(e.transform.position) - e.Radius <= 2.4f) { dashHit.Add(e); SkillHit(e, 2.2f, Color.white); }
                }
            }

            // Ground Slam / Leap Slam: in aer -> impact la aterizare
            if (slamPending)
            {
                slamT += dt;
                if (leapVel.sqrMagnitude > 0f) cc.Move(leapVel * dt);
                if ((!airborne && slamT > 0.15f) || slamT > 1.6f)
                {
                    slamPending = false; leapVel = Vector3.zero;
                    Vector3 p = transform.position;
                    Area(p, slamRadius, slamMult, 0f, slamStun, slamCol);
                    Fx.Poof(p + Vector3.up * 0.2f, new Color(0.6f, 0.5f, 0.4f), 2.2f);
                    Fx.Ring(p + Vector3.up * 0.1f, slamCol, slamRadius, 56);
                    Fx.Ring(p + Vector3.up * 0.1f, Color.white, slamRadius * 0.55f, 30);
                    Fx.Fountain(p, new Color(0.65f, 0.55f, 0.4f), 26);
                    SkillFx.Wave(this, p, slamCol, slamRadius, 0f, 0f, 0f, 0.35f);
                    Fx.Shake(0.5f); Fx.HitStop(0.05f);
                    Sfx.Play("BossSlam", 0.7f, 1.1f, 0.1f);
                }
            }
        }

        // ---------------------------------------------------------------
        public void UseSkill(int i)
        {
            if (dead || i < 0 || i > 2) return;
            var g = Game.I;
            if (g != null && g.InPlaza) { g.Say("Skills can't be used in the Trade Plaza.", 1.5f); Sfx.Play("Error", 0.4f); return; }
            if (skillCd[i] > 0f || SkillBusy) { Sfx.Play("Error", 0.25f); return; }
            var def = SkillDB.Get(Skills[i]);
            skillCd[i] = def.cooldown;
            Cast(def);
        }

        Vector3 Fwd()
        {
            Vector3 f = transform.forward; f.y = 0;
            return f.sqrMagnitude > 0.01f ? f.normalized : Vector3.forward;
        }

        /// <summary>Se intoarce spre cel mai apropiat inamic (daca exista) si intoarce directia.</summary>
        Vector3 FaceTarget(float maxDist, out Enemy target)
        {
            target = NearestEnemy(Mathf.Min(maxDist, 13f));   // skill-urile tintesc doar monstrii din apropiere
            if (target != null)
            {
                Vector3 f = target.transform.position - transform.position; f.y = 0;
                if (f.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(f);
            }
            return Fwd();
        }

        void PlayAnim(string a, float speed = 1.4f)
        {
            if (anim == null) return;
            if (!anim.Has(a)) a = anim.Has("attack-melee-right") ? "attack-melee-right" : "idle";
            anim.Speed = speed;
            anim.Play(a, 0.05f, false, true);
            lastAttackAnim = Time.time + 0.15f;
        }

        Vector3 Chest { get { return transform.position + Vector3.up * 1.3f; } }

        void Cast(SkillDef d)
        {
            Enemy tgt;
            Vector3 p = transform.position;
            Color c = d.col;
            switch (d.kind)
            {
                case SkillKind.Whirlwind:
                    FaceTarget(Range + 3f, out tgt);
                    if (model != null && spinT <= 0f) spinBase = model.localRotation;
                    spinT = 0.6f; spinHit2 = 0.3f;
                    PlayAnim("attack-melee-right", 2.2f);
                    if (trail != null) trail.emitting = true;
                    SpinHit();
                    Sfx.Play("SwingHeavy", 0.7f, 0.8f, 0.05f);
                    break;

                case SkillKind.ShieldWall:
                    shieldT = 6f;
                    PlayAnim("interact-right", 1.2f);
                    SkillFx.Aura(transform, c, 3f, 6f);
                    Fx.Ring(p + Vector3.up * 0.1f, c, 2.5f, 36);
                    Fx.Sparkle(Chest, Color.white, 14, 1.2f);
                    Sfx.Play("Equip", 0.8f, 0.7f, 0f);
                    Say("SHIELD WALL", c);
                    break;

                case SkillKind.GroundSlam:
                    StartSlam(7.5f, 6f, 2.6f, 1.6f, Vector3.zero, c);
                    PlayAnim("jump", 1.4f);
                    break;

                case SkillKind.LeapSlam:
                {
                    Vector3 f = FaceTarget(16f, out tgt);
                    float dist = tgt != null ? Mathf.Clamp(FlatDist(tgt.transform.position) - tgt.Radius - 0.5f, 2f, 12f) : 8f;
                    StartSlam(10.5f, 4.8f, 3.2f, 0.8f, f * dist / 0.8f, c);
                    PlayAnim("jump", 1.3f);
                    break;
                }

                case SkillKind.ArrowRain:
                {
                    Vector3 f = FaceTarget(20f, out tgt);
                    Vector3 at = tgt != null ? tgt.transform.position : p + f * 7f;
                    PlayAnim("interact-right", 1.6f);
                    SkillFx.Zone(this, at, c, "arrows", 2f, 4.2f, 0.5f, 0.25f, 0f);
                    for (int k = 0; k < 6; k++) Fx.Ambient(Chest, Vector3.up * 20f + Random.insideUnitSphere * 3f, new Color(1f, 0.9f, 0.6f), 0.25f, 0.4f, true);
                    Sfx.Play("SwingThrust", 0.7f, 1.3f, 0.1f);
                    break;
                }

                case SkillKind.DashStrike:
                {
                    dashDir = FaceTarget(14f, out tgt);
                    dashT = 0.24f; dashHit.Clear();
                    PlayAnim("attack-melee-right", 2f);
                    Fx.Poof(p + Vector3.up * 0.3f, new Color(0.85f, 0.85f, 0.9f), 0.9f);
                    Sfx.Play("SwingLight", 0.8f, 0.8f, 0.05f);
                    break;
                }

                case SkillKind.HealingLight:
                {
                    float h = MaxHp * 0.35f;
                    hp = Mathf.Min(MaxHp, hp + h);
                    PlayAnim("emote-yes", 1.2f);
                    Fx.Pillar(p, c, 6f, 1f);
                    Fx.Fountain(p, c, 30);
                    Fx.Sparkle(Chest, Color.white, 16, 1.2f);
                    Game.I.hud.Pop(p + Vector3.up * 2.6f, "+" + Mathf.RoundToInt(h), c, 1.1f);
                    Sfx.Play("Potion", 0.8f, 1.3f, 0f);
                    break;
                }

                case SkillKind.Fireball:
                {
                    Vector3 f = FaceTarget(22f, out tgt);
                    PlayAnim("attack-melee-right", 1.8f);
                    Vector3 dir = tgt != null ? (tgt.transform.position + Vector3.up - Chest) : f;
                    dir.y = Mathf.Clamp(dir.y, -2f, 2f);
                    SkillFx.Projectile(this, Chest + f * 0.8f, dir, 24f, c, 0.8f, 2.4f, true, 3.2f, 0f);
                    Fx.Poof(Chest + f, c, 0.6f);
                    Sfx.Play("SwingHeavy", 0.6f, 1.4f, 0.1f);
                    break;
                }

                case SkillKind.Meteor:
                {
                    Vector3 f = FaceTarget(22f, out tgt);
                    Vector3 at = tgt != null ? tgt.transform.position : p + f * 8f;
                    at.y = p.y;
                    PlayAnim("emote-yes", 1.4f);
                    Fx.Pillar(p, c, 4f, 0.6f);
                    SkillFx.Meteor(this, at, c, 4.6f, 5.5f);
                    Sfx.Play("BossRoar1", 0.25f, 1.8f, 0.1f);
                    break;
                }

                case SkillKind.FlameRing:
                    PlayAnim("attack-melee-right", 1.6f);
                    SkillFx.Wave(this, p, c, 7f, 2.2f, 0f, 0f, 0.5f);
                    Fx.Ring(p + Vector3.up * 0.2f, new Color(1f, 0.85f, 0.3f), 2f, 30);
                    for (int k = 0; k < 24; k++)
                    {
                        float a = k / 24f * Mathf.PI * 2f;
                        Fx.Ambient(p + Vector3.up * 0.5f, new Vector3(Mathf.Cos(a), 0.3f, Mathf.Sin(a)) * 14f, Color.Lerp(c, Color.yellow, Random.value * 0.5f), 0.9f, 0.5f, true);
                    }
                    Fx.Shake(0.25f);
                    Sfx.Play("BossSlam", 0.4f, 1.6f, 0.1f);
                    break;

                case SkillKind.IceShard:
                {
                    Vector3 f = FaceTarget(20f, out tgt);
                    PlayAnim("attack-melee-right", 1.8f);
                    for (int k = -1; k <= 1; k++)
                        SkillFx.Projectile(this, Chest + f * 0.8f, Quaternion.Euler(0, k * 14f, 0) * f, 28f, c, 0.45f, 1.5f, false, 0f, 3f);
                    Fx.Sparkle(Chest + f, c, 10, 0.5f);
                    Sfx.Play("Crit", 0.45f, 1.6f, 0.1f);
                    break;
                }

                case SkillKind.FrostNova:
                    PlayAnim("emote-yes", 1.6f);
                    SkillFx.Wave(this, p, c, 7f, 1.8f, 4f, 0f, 0.4f);
                    for (int k = 0; k < 14; k++)
                    {
                        float a = k / 14f * Mathf.PI * 2f;
                        Vector3 sp = p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * Random.Range(2.5f, 6f);
                        Fx.Fountain(sp, Color.Lerp(c, Color.white, 0.5f), 4);
                    }
                    Fx.Ring(p + Vector3.up * 0.1f, Color.white, 7f, 60);
                    Sfx.Play("BossSlam", 0.35f, 1.7f, 0.05f);
                    break;

                case SkillKind.Blizzard:
                {
                    Vector3 f = FaceTarget(18f, out tgt);
                    Vector3 at = tgt != null ? tgt.transform.position : p + f * 6f;
                    PlayAnim("emote-yes", 1.3f);
                    SkillFx.Zone(this, at, c, "snow", 3f, 5f, 0.42f, 0.3f, 2f);
                    Sfx.Play("Ambient", 0.4f, 1.4f, 0f);
                    break;
                }

                case SkillKind.ChainLightning:
                {
                    FaceTarget(18f, out tgt);
                    PlayAnim("attack-melee-right", 1.8f);
                    var pts = new List<Vector3> { Chest };
                    var hit = new HashSet<Enemy>();
                    float m = 2.2f;
                    Enemy cur = tgt;
                    while (cur != null && hit.Count < 5)
                    {
                        hit.Add(cur);
                        pts.Add(cur.transform.position + Vector3.up * 1f);
                        SkillHit(cur, m, c);
                        cur.Stun(0.3f);
                        Fx.Sparkle(cur.transform.position + Vector3.up, c, 10, 0.6f);
                        m *= 0.85f;
                        Enemy next = null; float nd = 9f;
                        foreach (var e in Enemy.All)
                        {
                            if (e == null || e.Dying || hit.Contains(e)) continue;
                            float dd = Vector3.Distance(e.transform.position, cur.transform.position);
                            if (dd < nd) { nd = dd; next = e; }
                        }
                        cur = next;
                    }
                    if (pts.Count == 1) pts.Add(Chest + Fwd() * 8f);
                    SkillFx.Bolt(pts, c, 0.35f, 0.35f);
                    Sfx.Play("Crit", 0.7f, 1.4f, 0.1f);
                    Fx.Shake(0.15f);
                    break;
                }

                case SkillKind.ThunderClap:
                {
                    PlayAnim("attack-melee-right", 1.3f);
                    SkillFx.Wave(this, p, c, 6.5f, 1.9f, 0f, 1.6f, 0.35f);
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 top = p + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * 3f;
                        SkillFx.Bolt(new List<Vector3> { top + Vector3.up * 14f, top }, c, 0.3f, 0.3f);
                    }
                    Fx.Pillar(p, c, 5f, 0.4f);
                    Fx.Shake(0.4f);
                    Sfx.Play("BossSlam", 0.6f, 1.3f, 0.1f);
                    Sfx.Play("Crit", 0.5f, 0.8f, 0.1f);
                    break;
                }

                case SkillKind.Blink:
                {
                    Vector3 f = FaceTarget(12f, out tgt);
                    Fx.Poof(Chest, c, 1.2f);
                    Fx.Sparkle(Chest, c, 16, 1f);
                    cc.Move(f * 9f);
                    Vector3 q = transform.position;
                    Fx.Poof(q + Vector3.up * 1.2f, c, 1.4f);
                    Fx.Ring(q + Vector3.up * 0.1f, c, 3.5f, 36);
                    Area(q, 3.5f, 2f, 0f, 0f, c);
                    PlayAnim("attack-melee-right", 1.8f);
                    Sfx.Play("Portal", 0.6f, 1.6f, 0.1f);
                    break;
                }

                case SkillKind.PoisonCloud:
                {
                    Vector3 f = FaceTarget(14f, out tgt);
                    Vector3 at = tgt != null ? tgt.transform.position : p + f * 4f;
                    PlayAnim("interact-right", 1.5f);
                    SkillFx.Zone(this, at, c, "poison", 4f, 4.5f, 0.35f, 0.4f, 1f);
                    Fx.Poof(at + Vector3.up * 0.5f, c, 1.8f);
                    Sfx.Play("Potion", 0.6f, 0.6f, 0.1f);
                    break;
                }

                case SkillKind.BattleCry:
                    cryT = 8f;
                    PlayAnim("emote-yes", 1.1f);
                    SkillFx.Aura(transform, c, 3f, 8f);
                    Fx.Ring(p + Vector3.up * 0.1f, c, 5f, 50);
                    Fx.Ring(p + Vector3.up * 0.1f, new Color(1f, 0.8f, 0.3f), 3f, 30);
                    Fx.Shake(0.3f);
                    Sfx.Play("BossRoar0", 0.35f, 1.5f, 0.05f);
                    Say("BATTLE CRY  +50% DMG", c);
                    break;

                case SkillKind.ShadowFury:
                    furyT = 5f;
                    PlayAnim("emote-yes", 1.4f);
                    SkillFx.Aura(transform, c, 3f, 5f);
                    Fx.Poof(Chest, new Color(0.25f, 0.1f, 0.35f), 1.6f);
                    Fx.Sparkle(Chest, c, 20, 1.2f);
                    Sfx.Play("Portal", 0.5f, 0.7f, 0.05f);
                    Say("SHADOW FURY  100% CRIT", c);
                    break;

                case SkillKind.LifeDrain:
                {
                    FaceTarget(16f, out tgt);
                    PlayAnim("interact-right", 1.4f);
                    if (tgt == null) { Fx.Sparkle(Chest + Fwd() * 2f, c, 10, 0.8f); break; }
                    int dmg = SkillHit(tgt, 3f, c);
                    float heal = Mathf.Min(MaxHp * 0.25f, dmg * 0.5f);
                    hp = Mathf.Min(MaxHp, hp + heal);
                    SkillFx.Bolt(new List<Vector3> { Chest, tgt.transform.position + Vector3.up }, c, 0.25f, 0.5f);
                    for (int k = 0; k < 6; k++) SkillFx.Orb(tgt.transform.position + Vector3.up, transform, c);
                    Game.I.hud.Pop(p + Vector3.up * 2.6f, "+" + Mathf.RoundToInt(heal), new Color(0.45f, 1f, 0.45f), 1f);
                    Sfx.Play("Hurt", 0.4f, 0.6f, 0.1f);
                    break;
                }

                case SkillKind.HolyNova:
                {
                    PlayAnim("emote-yes", 1.5f);
                    SkillFx.Wave(this, p, c, 6.5f, 1.9f, 0f, 0f, 0.4f);
                    float h = MaxHp * 0.15f;
                    hp = Mathf.Min(MaxHp, hp + h);
                    Fx.Pillar(p, c, 7f, 0.7f);
                    Fx.Sparkle(Chest, Color.white, 20, 1.5f);
                    Game.I.hud.Pop(p + Vector3.up * 2.6f, "+" + Mathf.RoundToInt(h), new Color(0.45f, 1f, 0.45f), 1f);
                    Sfx.Play("JingleLevel", 0.35f, 1.4f, 0f);
                    break;
                }
            }
        }

        void Say(string s, Color c)
        {
            if (Game.I != null && Game.I.hud != null) Game.I.hud.Pop(transform.position + Vector3.up * 3.2f, s, c, 1.2f);
        }

        void StartSlam(float jumpV, float radius, float mult, float stun, Vector3 leap, Color c)
        {
            vy = jumpV; airborne = true; airT = 0f; jumpStretch = 1f;
            slamPending = true; slamT = 0f; slamRadius = radius; slamMult = mult; slamStun = stun; leapVel = leap; slamCol = c;
            Fx.Poof(transform.position + Vector3.up * 0.1f, new Color(0.85f, 0.85f, 0.8f), 0.7f);
            Sfx.Play("SwingHeavy", 0.5f, 0.7f, 0.1f);
        }

        void SpinHit()
        {
            Area(transform.position, Range + 1.6f, 1.5f, 0f, 0f, new Color(0.85f, 0.9f, 1f));
            Fx.Ring(transform.position + Vector3.up * 1f, new Color(0.85f, 0.9f, 1f), Range + 1.2f, 40);
            Sfx.Play("SwingHeavy", 0.6f, 1.1f, 0.15f);
        }

        void Area(Vector3 c, float r, float mult, float slow, float stun, Color col)
        {
            for (int i = Enemy.All.Count - 1; i >= 0; i--)
            {
                if (i >= Enemy.All.Count) continue;
                var e = Enemy.All[i];
                if (e == null || e.Dying) continue;
                Vector3 d = e.transform.position - c; d.y = 0;
                if (d.magnitude - e.Radius <= r)
                {
                    SkillHit(e, mult, col);
                    if (slow > 0f) e.Slow(slow);
                    if (stun > 0f) e.Stun(stun);
                }
            }
        }

        /// <summary>Loveste un inamic cu Damage x mult (crit normal, sau mereu in Shadow Fury). Intoarce damage-ul dat.</summary>
        public int SkillHit(Enemy e, float mult, Color col)
        {
            if (e == null || e.Dying || mult <= 0f) return 0;
            bool crit = furyT > 0f || Random.value < CritChance;
            int dmg = Mathf.Max(1, Mathf.RoundToInt(Damage * mult * Random.Range(0.9f, 1.1f) * (crit ? 2.2f : 1f)));
            e.Hit(dmg, crit, transform.position);
            Fx.Hit(e.transform.position + Vector3.up * 1f, col, crit);
            return dmg;
        }
    }
}
