// =====================================================================
//  Player.cs - miscare, atac, animatii si aspectul eroului
//  (progresul, inventarul, forja, pets si rebirth sunt in PlayerProgress.cs)
// =====================================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    [RequireComponent(typeof(CharacterController))]
    public partial class Player : MonoBehaviour
    {
        public const float BaseRange = 3.6f;   // lovitura de aproape (nu mai lovesti de departe)
        /// <summary>Raza de atac: lancea si sabia mare ajung mai departe.</summary>
        public float Range { get { return BaseRange + ItemDB.KindReach(Equipped(Slot.Weapon)); } }
        public const float HeroHeight = 2.1f;

        public float hp = 100f;
        public bool dead;
        // pentru multiplayer (ceilalti vad cum te misti / lovesti)
        public byte SwingCount { get; private set; }
        public bool SprintNow { get; private set; }
        public bool MovingNow { get; private set; }

        CharacterController cc;
        Transform model;            // holder-ul modelului (se roteste)
        /// <summary>Modelul eroului (cu armele/armura vizibile) - folosit de avatarul din INVENTORY.</summary>
        public Transform ModelRoot { get { return model; } }
        public string GearKey { get { return gearKey + "|" + (D != null ? D.hero : 0); } }
        SimpleAnimator anim;
        Transform swordPivot;       // sabia (atasata de brat sau de corp)
        TrailRenderer trail;
        readonly List<Material> swordMats = new List<Material>();
        bool proceduralSword;       // true = fara model, sabia se leagana procedural
        Transform swordTilt, weaponMesh, tipT;
        // echipamentul vizibil (coif + armura) - atasat de oasele "head" si "torso"
        Transform headBone, torsoBone, helmetRoot, armorRoot;
        Vector3 headLocal, torsoLocal, headSize, torsoSize;
        Quaternion headRot, torsoRot;
        string gearKey = "";
        const float SwordLen = HeroHeight * 0.38f;   // proportional cu eroul

        float vy, atkT, swingT, hurtT = -10f, stepT, lastAttackAnim;
        // saritura (SPACE): anticipare -> zbor (jump / fall) -> aterizare cu squash si praf
        float jumpPrep = -1f, airT, landSquash, jumpStretch;
        bool airborne;
        bool attackLeft;
        Vector3 moveDir;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.5f; cc.center = new Vector3(0, 1, 0); cc.stepOffset = 0.4f;
            cc.slopeLimit = 50f;

            LoadProgress();
            BuildVisual();
            RefreshSword();
            RebuildPets();
            hp = MaxHp;
        }

        // ---------------------------------------------------------------
        //  Aspect
        // ---------------------------------------------------------------
        public void BuildVisual()
        {
            // construim cu rotatie neutra: calculul pozitiei mainii (din poza T) depinde de ea
            var savedRot = transform.rotation;
            transform.rotation = Quaternion.identity;
            BuildVisualInner();
            transform.rotation = savedRot;
        }

        /// <summary>Schimba eroul (dir = -1 / +1) si reface aspectul.</summary>
        public void ChangeHero(int dir)
        {
            int n = GameData.Heroes.Length;
            D.hero = ((D.hero + dir) % n + n) % n;
            BuildVisual();
            RefreshSword();
            Fx.Poof(transform.position + Vector3.up, new Color(0.9f, 0.8f, 1f), 1f);
            Sfx.Play("Portal", 0.5f);
            SaveNow();
        }

        void BuildVisualInner()
        {
            if (model != null) Kill(model);
            swordPivot = null; anim = null; trail = null;

            string path = GameData.CHAR + GameData.Heroes[Mathf.Clamp(D.hero, 0, GameData.Heroes.Length - 1)];
            model = ModelLib.Spawn(path, transform, HeroHeight);
            if (model != null)
            {
                anim = SimpleAnimator.Setup(model, path);
                if (anim != null) anim.Play("idle");
                proceduralSword = false;
            }
            else
            {
                model = new GameObject("Body").transform;
                model.SetParent(transform, false);
                Art.Prim(PrimitiveType.Capsule, new Vector3(0, 1, 0), Vector3.one, new Color(0.2f, 0.55f, 1f), model, false);
                Art.Prim(PrimitiveType.Sphere, new Vector3(0, 1.8f, 0), new Vector3(0.95f, 0.5f, 0.95f), new Color(1f, 0.25f, 0.3f), model, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    Art.Prim(PrimitiveType.Sphere, new Vector3(s * 0.2f, 1.45f, 0.4f), Vector3.one * 0.24f, Color.white, model, false);
                    Art.Prim(PrimitiveType.Sphere, new Vector3(s * 0.2f, 1.45f, 0.51f), Vector3.one * 0.12f, Color.black, model, false);
                }
                proceduralSword = true;
            }
            BuildSword();
            FindGearAnchors();
            gearKey = "";

            // umbra "blob" sub erou (arata bine si fara umbre reale)
            var blob = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.03f, 0), new Vector3(1.3f, 0.01f, 1.3f), new Color(0, 0, 0, 1), model, false, false);
            var bm = blob.GetComponent<Renderer>();
            bm.sharedMaterial = Art.ShadowBlob();
            bm.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void BuildSword()
        {
            swordMats.Clear();
            Transform arm = proceduralSword ? null : ModelLib.FindDeep(model, "arm-right");
            swordPivot = new GameObject("SwordPivot").transform;
            if (arm != null)
            {
                // Modelele Kenney au poza de bind "T": bratul e intins pe axa X.
                // Mana = capatul bratului = marginea modelului pe partea bratului.
                Vector3 root = model.position;
                float side = Mathf.Sign(arm.position.x - root.x);
                if (side == 0f) side = 1f;
                Bounds b;
                float handX = arm.position.x + side * HeroHeight * 0.22f;
                if (ModelLib.GetBounds(model.gameObject, out b))
                    handX = (side > 0 ? b.max.x : b.min.x) - side * HeroHeight * 0.05f;
                swordPivot.SetParent(arm, true);
                swordPivot.position = new Vector3(handX, arm.position.y - HeroHeight * 0.02f, arm.position.z + HeroHeight * 0.03f);
                // lama spre inainte, usor ridicata; cand bratul coboara (rotatie pe Z) lama ramane inainte
                swordPivot.rotation = Quaternion.Euler(-25f, 0, 0);
            }
            else
            {
                swordPivot.SetParent(model, false);
                swordPivot.localPosition = new Vector3(0.7f, 1.1f, 0.3f);
                proceduralSword = true;
            }

            // arma (construita din cod de GearVisuals) - lama pe +Y => o rotim spre +Z
            swordTilt = new GameObject("Tilt").transform;
            swordTilt.SetParent(swordPivot, false);
            swordTilt.localRotation = Quaternion.Euler(90f, 0, 0);
            weaponMesh = null;

            // dara luminoasa la varful sabiei (activa doar in timpul loviturii)
            var tip = new GameObject("Tip").transform;
            tip.SetParent(swordPivot, false);
            tip.localPosition = new Vector3(0, 0, SwordLen * 0.8f);
            tipT = tip;
            trail = tip.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.14f;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = 0.3f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
            trail.sharedMaterial = Art.FxMaterial(true);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.emitting = false;
        }

        /// <summary>Gaseste oasele capului si trunchiului + dimensiunile lor (in poza de bind, rotatie neutra).</summary>
        void FindGearAnchors()
        {
            headBone = torsoBone = null;
            if (proceduralSword || model == null) return;
            headBone = ModelLib.FindDeep(model, "head");
            torsoBone = ModelLib.FindDeep(model, "torso");
            Transform headMesh = ModelLib.FindDeep(model, "head-mesh");
            Bounds hb = new Bounds(), bb = new Bounds();
            bool hasHead = headMesh != null && ModelLib.GetBounds(headMesh.gameObject, out hb);
            bool hasBody = ModelLib.GetBounds(model.gameObject, out bb);
            if (headBone == null || !hasHead) { headBone = null; }
            else
            {
                headLocal = headBone.InverseTransformPoint(hb.center);
                headRot = Quaternion.Inverse(headBone.rotation);
                headSize = hb.size;
            }
            if (torsoBone != null && hasHead && hasBody)
            {
                // trunchiul: intre sold (osul torso) si gat (baza capului)
                float top = hb.min.y, bottom = Mathf.Min(torsoBone.position.y, top - HeroHeight * 0.12f);
                float h = Mathf.Max(0.1f, top - bottom);
                Vector3 c = new Vector3(torsoBone.position.x, bottom + h * 0.5f, torsoBone.position.z);
                torsoLocal = torsoBone.InverseTransformPoint(c);
                torsoRot = Quaternion.Inverse(torsoBone.rotation);
                torsoSize = new Vector3(hb.size.x * 0.72f, h, Mathf.Min(bb.size.z, hb.size.z) * 0.7f);
            }
            else torsoBone = null;
        }

        static Transform GearRoot(string n, Transform bone, Vector3 local, Quaternion rot)
        {
            var t = new GameObject(n).transform;
            t.SetParent(bone, false);
            t.localPosition = local;
            t.localRotation = rot;
            float s = bone.lossyScale.x;
            t.localScale = Vector3.one / Mathf.Max(0.0001f, s);
            return t;
        }

        /// <summary>Scoate imediat piesa veche de pe model (Destroy se face abia la sfarsitul frame-ului,
        /// iar avatarul din INVENTORY copia modelul in acelasi frame -> ramanea itemul vechi pe el).</summary>
        static void Kill(Transform t)
        {
            t.gameObject.SetActive(false);
            t.SetParent(null, false);
            Destroy(t.gameObject);
        }

        /// <summary>Reface arma, coiful si armura vizibile (dupa echipare, forja, schimbare de erou).</summary>
        public void RefreshSword()
        {
            var w = Equipped(Slot.Weapon);
            var hItem = Equipped(Slot.Helmet);
            var aItem = Equipped(Slot.Armor);
            string key = (w == null ? "-" : w.id + "+" + w.plus) + "|" + (hItem == null ? "-" : hItem.id + "+" + hItem.plus) + "|" + (aItem == null ? "-" : aItem.id + "+" + aItem.plus);
            int r = w == null ? 0 : w.rarity;
            Color c = ItemDB.Col(r);

            if (key != gearKey)
            {
                gearKey = key;
                // arma
                if (swordTilt != null)
                {
                    if (weaponMesh != null) Kill(weaponMesh);
                    weaponMesh = null;
                    if (w != null)   // fara arma echipata: mana goala
                    {
                        weaponMesh = GearVisuals.BuildWeapon(w, swordTilt, SwordLen);
                        weaponMesh.localPosition = new Vector3(0, -SwordLen * 0.15f, 0);   // manerul in pumn
                    }
                    if (tipT != null)
                    {
                        var k = ItemDB.KindOf(w);
                        float reach = k == ItemDB.WeaponKind.Spear ? 1.3f : k == ItemDB.WeaponKind.Greatsword ? 1.05f : 0.85f;
                        tipT.localPosition = new Vector3(0, 0, SwordLen * reach);
                    }
                }
                else
                {
                    foreach (var m in swordMats)
                    {
                        if (proceduralSword) Art.SetColor(m, c);
                        Art.SetEmission(m, r >= 2 ? c * (0.25f + 0.12f * r) : Color.black);
                    }
                }
                // coif
                if (helmetRoot != null) Kill(helmetRoot);
                helmetRoot = null;
                if (hItem != null && headBone != null)
                {
                    helmetRoot = GearRoot("Helmet", headBone, headLocal, headRot);
                    GearVisuals.BuildHelmet(hItem, helmetRoot, headSize);
                }
                // armura
                if (armorRoot != null) Kill(armorRoot);
                armorRoot = null;
                if (aItem != null && torsoBone != null)
                {
                    armorRoot = GearRoot("Armor", torsoBone, torsoLocal, torsoRot);
                    GearVisuals.BuildArmor(aItem, armorRoot, torsoSize);
                }
            }
            if (swordPivot != null)
            {
                float s = 1f + Mathf.Min(0.2f, (w == null ? 0 : w.plus) * 0.008f) + r * 0.025f;
                swordPivot.localScale = Vector3.one * s;
            }
            if (trail != null)
            {
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.Lerp(c, Color.white, 0.4f), 0), new GradientColorKey(c, 1) },
                          new[] { new GradientAlphaKey(0.85f, 0), new GradientAlphaKey(0f, 1) });
                trail.colorGradient = g;
            }
        }

        // ---------------------------------------------------------------
        //  Update
        // ---------------------------------------------------------------
        void Update()
        {
            D.playTime += Time.deltaTime;
            TickBuffs(Time.deltaTime);
            if (dead) return;
            float dt = Time.deltaTime;
            var g = Game.I;
            bool uiOpen = g != null && g.hud != null && g.hud.Blocking;

            // miscare
            Vector2 mv = uiOpen ? Vector2.zero : Inp.Move();
            // miscare relativa la camera (third person)
            float yaw = g != null ? g.CamYaw : 0f;
            moveDir = Quaternion.Euler(0, yaw, 0) * new Vector3(mv.x, 0, mv.y);
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();
            bool sprint = Inp.Sprint() && moveDir.sqrMagnitude > 0.01f;
            SprintNow = sprint; MovingNow = moveDir.sqrMagnitude > 0.01f;
            float speed = (sprint ? 12.5f : 9f) * (1f + 0.02f * Mathf.Min(10, D.rebirth));
            bool grounded = cc.isGrounded;
            bool jumpPressed = Inp.JumpDown();   // citit mereu (pe telefon nu ramane "memorat" pentru mai tarziu)
            if (!uiOpen && grounded && jumpPrep < 0f && !airborne && jumpPressed)
            {
                jumpPrep = 0.08f;                         // mica ghemuire inainte de salt
                if (anim != null && anim.Has("jump")) { anim.Speed = 1.3f; anim.Play("jump", 0.06f, false, true); }
                lastAttackAnim = Time.time;
            }
            if (jumpPrep >= 0f)
            {
                jumpPrep -= dt;
                if (jumpPrep < 0f)
                {
                    vy = 10.5f;
                    airborne = true; airT = 0f;
                    jumpStretch = 1f;
                    Sfx.Play("SwingLight", 0.25f, 0.7f, 0.1f);
                    Fx.Poof(transform.position + Vector3.up * 0.1f, new Color(0.85f, 0.85f, 0.8f), 0.5f);
                    grounded = false;
                }
            }
            if (airborne)
            {
                airT += dt;
                vy -= 26f * dt;
                if (vy < 0f && anim != null && anim.Has("fall") && anim.Current == "jump" && anim.Done) anim.Play("fall", 0.15f);
                if (cc.isGrounded && airT > 0.12f && vy <= 0f)
                {
                    airborne = false;
                    landSquash = 1f;
                    Fx.Poof(transform.position + Vector3.up * 0.1f, new Color(0.85f, 0.82f, 0.75f), 0.8f);
                    Sfx.Play(g != null && g.InPlaza ? "StepStone" : GameData.Maps[g != null ? g.mapIndex : 0].step, 0.5f, 0.85f, 0.1f);
                    lastAttackAnim = Time.time - 0.2f;
                    vy = -2f;
                }
            }
            else vy = grounded ? -2f : vy - 25f * dt;
            cc.Move((moveDir * speed * (airborne ? 0.9f : 1f) + Vector3.up * vy) * dt);

            // squash & stretch la saritura / aterizare (animatie vie, nu saritura statica)
            if (model != null)
            {
                jumpStretch = Mathf.MoveTowards(jumpStretch, 0f, dt * 3.5f);
                landSquash = Mathf.MoveTowards(landSquash, 0f, dt * 5f);
                float prep = jumpPrep >= 0f ? 1f - jumpPrep / 0.08f : 0f;
                float sy = 1f + 0.16f * jumpStretch - 0.18f * Mathf.Sin(landSquash * Mathf.PI) - 0.12f * prep;
                float sxz = 1f - 0.08f * jumpStretch + 0.14f * Mathf.Sin(landSquash * Mathf.PI) + 0.08f * prep;
                model.localScale = new Vector3(sxz, sy, sxz);
            }
            if (moveDir.sqrMagnitude > 0.01f && swingT <= 0f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 1f - Mathf.Exp(-16f * dt));

            // pasi
            if (moveDir.sqrMagnitude > 0.01f && cc.isGrounded)
            {
                stepT -= dt * (sprint ? 1.45f : 1f);
                if (stepT <= 0f)
                {
                    stepT = 0.32f;
                    Sfx.Play(g.InPlaza ? "StepStone" : GameData.Maps[g.mapIndex].step, 0.25f, 1f, 0.15f);
                }
            }

            // atac
            atkT -= dt;
            swingT -= dt;
            bool pointerFree = g == null || g.hud == null || !g.hud.PointerOverUI();
            // un click = o lovitura. Click-urile rapide ataca mai repede (pana la ClickMinGap);
            // un click dat prea devreme e tinut minte ("buffer") si loveste imediat ce se poate.
            // un click = EXACT o lovitura (nu se mai pune in "coada", ca sa nu para ca loveste de 2 ori).
            // Cu AUTO SWING pornit, click-urile tale au prioritate: auto-ul asteapta 1s dupa ultimul click.
            if (!uiOpen && Inp.Attack(pointerFree))
            {
                lastClickT = Time.time;
                if (Time.time - lastHitT >= ClickMinGap) DoAttack();
            }
            bool auto = D.autoAttack && D.passAuto && !uiOpen && Time.time - lastClickT > 1f && NearestEnemy(Range + 1.5f) != null;
            if (auto && Time.time - lastHitT >= AttackCooldown) DoAttack();

            if (trail != null) trail.emitting = swingT > 0f && weaponMesh != null;
            if (proceduralSword && swordPivot != null)
            {
                if (swingT > 0f)
                {
                    float k = 1f - Mathf.Clamp01(swingT / 0.25f);
                    swordPivot.localRotation = Quaternion.Euler(Mathf.Lerp(-70f, 70f, k), 0, 0);
                }
                else swordPivot.localRotation = Quaternion.Euler(-25f, 0, 0);
            }

            TickSkills(dt, uiOpen);

            // animatii
            if (anim != null && Time.time - lastAttackAnim > 0.32f && !airborne && jumpPrep < 0f)
            {
                if (moveDir.sqrMagnitude > 0.01f) anim.Play(sprint && anim.Has("sprint") ? "sprint" : "walk", 0.12f);
                else anim.Play("idle", 0.2f);
                anim.Speed = moveDir.sqrMagnitude > 0.01f ? (sprint ? 1.25f : 1.35f) : 1f;
            }

            // regenerare in afara luptei
            if (Time.time - hurtT > 5f && hp < MaxHp)
                hp = Mathf.Min(MaxHp, hp + MaxHp * 0.04f * dt);
            if (hp > MaxHp) hp = MaxHp;

            if (!uiOpen && Inp.PotionDown()) Potion();
            // potiunile de boost (XP / DMG / COINS) se beau doar din INVENTORY > MATERIALS
        }

        float FlatDist(Vector3 p)
        {
            Vector3 d = p - transform.position; d.y = 0;
            return d.magnitude;
        }

        public Enemy NearestEnemy(float maxDist)
        {
            Enemy nearest = null; float nd = maxDist;
            foreach (var e in Enemy.All)
            {
                if (e == null || e.Dying) continue;
                float d = FlatDist(e.transform.position) - e.Radius;
                if (d < nd) { nd = d; nearest = e; }
            }
            return nearest;
        }

        float lastClickT = -10f, lastHitT = -10f;
        /// <summary>Intervalul minim intre doua lovituri date prin click (spam = atac rapid). Auto Swing loveste la AttackCooldown.</summary>
        float ClickMinGap { get { return AttackCooldown * 0.45f; } }

        void DoAttack()
        {
            atkT = AttackCooldown;
            lastHitT = Time.time;
            swingT = 0.25f;
            SwingCount++;

            var nearest = NearestEnemy(Range + 2f);
            if (nearest != null)
            {
                Vector3 f = nearest.transform.position - transform.position; f.y = 0;
                if (f.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(f);
            }

            if (anim != null)
            {
                // sabia e in mana dreapta -> folosim doar animatia de lovire cu dreapta
                attackLeft = !attackLeft;
                string a = anim.Has("attack-melee-right") ? "attack-melee-right" : "attack-melee-left";
                anim.Speed = 1.6f;
                anim.Play(a, 0.05f, false, true);
                lastAttackAnim = Time.time;
            }
            // fiecare tip de arma are "whoosh"-ul lui
            var wk = ItemDB.KindOf(Equipped(Slot.Weapon));
            string sw = wk == ItemDB.WeaponKind.Spear ? "SwingThrust" : (wk == ItemDB.WeaponKind.Hammer || wk == ItemDB.WeaponKind.Axe || wk == ItemDB.WeaponKind.Greatsword) ? "SwingHeavy" : "SwingLight";
            Sfx.Play(sw, 0.5f, 1f, 0.1f);

            bool any = false;
            for (int i = Enemy.All.Count - 1; i >= 0; i--)
            {
                if (i >= Enemy.All.Count) continue;
                var e = Enemy.All[i];
                if (e == null || e.Dying) continue;
                if (FlatDist(e.transform.position) - e.Radius <= Range)
                {
                    bool crit = FuryOn || Random.value < CritChance;
                    int dmg = Mathf.Max(1, Mathf.RoundToInt(Damage * Random.Range(0.9f, 1.1f) * (crit ? 2.2f : 1f)));
                    e.Hit(dmg, crit, transform.position);
                    any = true;
                }
            }
            if (any) Fx.Shake(0.12f);
        }

        public void TakeDamage(int d)
        {
            if (dead || AdminTools.God) return;
            int real = Mathf.Max(1, Mathf.RoundToInt(d * (1f - Defense) * (ShieldOn ? 0.35f : 1f)));
            hp -= real; hurtT = Time.time;
            Game.I.hud.Pop(transform.position + Vector3.up * 2.6f, "-" + real, new Color(1f, 0.3f, 0.3f), 1f);
            Game.I.hud.FlashDamage();
            Sfx.Play("Hurt", 0.5f);
            Fx.Shake(0.18f);
            if (hp <= 0f) StartCoroutine(Die());
        }

        IEnumerator Die()
        {
            dead = true;
            hp = 0f;
            D.deaths++;
            ResetSkillState();
            long lost = D.coins / 10;
            D.coins -= lost;
            if (anim != null) { anim.Speed = 1f; anim.Play("die", 0.1f, false, true); }
            Sfx.Play("JingleDeath", 0.7f, 1f, 0f);
            Game.I.Say("You died! You lost " + Fmt.Num(lost) + " coins.", 3f);
            Game.I.hud.DeathFade(2f);
            yield return new WaitForSeconds(2f);
            hp = MaxHp;
            Teleport(GameData.Maps[Game.I.mapIndex].Spawn);
            dead = false;
            if (anim != null) anim.Play("idle", 0.05f, true, true);
            SaveNow();
        }

        public void Teleport(Vector3 p)
        {
            ResetSkillState();
            cc.enabled = false;
            transform.position = p;
            cc.enabled = true;
            vy = 0f;
            transform.rotation = Quaternion.identity;               // privim spre interiorul hartii (+Z)
            if (Game.I != null) Game.I.ResetCamera(0f);
            foreach (var pet in petObjs) if (pet != null) pet.transform.position = p + Random.insideUnitSphere.normalized * 2f;
        }

        /// <summary>Ridicat loot: o aplecare rapida (doar prima parte a animatiei "pick-up", accelerata),
        /// apoi revine imediat in pozitia de lupta (nu mai ramane cu mainile incrucisate).</summary>
        public void PickUpAnim()
        {
            if (anim == null || dead || airborne) return;
            string n = anim.Has("pick-up") ? "pick-up" : anim.Has("interact-right") ? "interact-right" : null;
            if (n == null) return;
            const float sp = 2.1f;
            float len = Mathf.Max(0.2f, anim.Length(n));
            if (anim.Play(n, 0.06f, false, true))
            {
                anim.Speed = sp;
                lastAttackAnim = Time.time - 0.32f + len * 0.5f / sp;   // dupa jumatate din clip (jos) -> inapoi la idle / mers
            }
        }

        public void PlayEmote(string n)
        {
            if (anim == null || dead) return;
            if (anim.Play(n, 0.1f, false, true)) lastAttackAnim = Time.time + 0.5f;
        }
    }
}
