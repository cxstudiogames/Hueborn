// =====================================================================
//  RemotePlayer.cs - un jucator REAL din aceeasi camera (multiplayer):
//  eroul lui, arma lui, numele + nivelul deasupra capului, animatii.
//  Pozitia vine prin retea (Mp.cs) de ~10 ori pe secunda si e netezita.
// =====================================================================
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public class RemotePlayer : MonoBehaviour
    {
        public ulong id;
        public string pname = "";
        public int level = 1, hero = -1;
        public long power;
        public bool hasState;

        Vector3 netPos, vel;
        float netYaw;
        byte flags, swing = 255;
        Transform model, weapon, helmetT, armorT;
        SimpleAnimator anim;
        string weaponJson = "#", helmetJson = "#", armorJson = "#", petsKey = "#";
        HeroGear.Anchors anchors;
        // detalii afisate deasupra capului (ca la tine): titlul, clasa, nivelul, puterea
        public int cls, clsTier, titleTier;
        public string title = "";
        Color titleCol = Color.white;
        readonly System.Collections.Generic.List<GameObject> pets = new System.Collections.Generic.List<GameObject>();
        Text label, bubble;
        float bubbleT, lastState;

        public bool Dead { get { return (flags & 4) != 0; } }
        public Vector3 Pos { get { return transform.position; } }
        public float LastStateTime { get { return lastState; } }

        public static RemotePlayer Create(ulong id)
        {
            var go = new GameObject("Remote Player " + id);
            go.transform.SetParent(SceneGroups.Get("Remote Players"), false);
            var r = go.AddComponent<RemotePlayer>();
            r.id = id;
            go.SetActive(false);   // apare la primul mesaj cu pozitia
            return r;
        }

        public void SetProfile(string n, int h, int lvl, long pw, string wJson)
        {
            pname = string.IsNullOrEmpty(n) ? "Player" : n;
            level = lvl; power = pw;
            if (h != hero)
            {
                hero = h;
                if (model != null) Destroy(model.gameObject);
                model = null; weapon = null; anim = null; weaponJson = "#";
                string path = GameData.CHAR + GameData.Heroes[Mathf.Clamp(h, 0, GameData.Heroes.Length - 1)];
                var saved = transform.rotation;
                transform.rotation = Quaternion.identity;
                model = ModelLib.Spawn(path, transform, Player.HeroHeight);
                anchors = model != null ? HeroGear.Find(model) : new HeroGear.Anchors();
                helmetJson = armorJson = "#";
                if (model == null)
                {
                    model = new GameObject("Body").transform;
                    model.SetParent(transform, false);
                    Art.Prim(PrimitiveType.Capsule, new Vector3(0, 1, 0), Vector3.one, Color.HSVToRGB((id * 0.17f) % 1f, 0.55f, 1f), model, false);
                }
                else
                {
                    anim = SimpleAnimator.Setup(model, path);
                    if (anim != null) anim.Play("idle");
                }
                var blob = Art.Prim(PrimitiveType.Cylinder, new Vector3(0, 0.03f, 0), new Vector3(1.3f, 0.01f, 1.3f), Color.black, model, false, false);
                var br = blob.GetComponent<Renderer>();
                br.sharedMaterial = Art.ShadowBlob();
                br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                transform.rotation = saved;
            }
            if (wJson != weaponJson) { weaponJson = wJson; BuildWeapon(); }
            UpdateLabel();
        }

        /// <summary>Restul profilului: coif, armura, clasa, titlu, pets (versiunile noi ale jocului).</summary>
        public void SetExtra(string hJson, string aJson, int c, int ct, string tName, string tColHex, int tTier, string petList)
        {
            cls = c; clsTier = ct; title = tName ?? ""; titleTier = tTier;
            Color tc; titleCol = ColorUtility.TryParseHtmlString("#" + tColHex, out tc) ? tc : Color.white;
            if (hJson != helmetJson || aJson != armorJson)
            {
                helmetJson = hJson; armorJson = aJson;
                var saved = transform.rotation;
                transform.rotation = Quaternion.identity;
                if (helmetT != null) Destroy(helmetT.gameObject);
                if (armorT != null) Destroy(armorT.gameObject);
                helmetT = HeroGear.Helmet(anchors, HeroGear.FromJson(hJson));
                armorT = HeroGear.Armor(anchors, HeroGear.FromJson(aJson));
                transform.rotation = saved;
            }
            if (petList != petsKey) { petsKey = petList; BuildPets(petList); }
            UpdateLabel();
        }

        void UpdateLabel()
        {
            if (label == null) return;
            label.text = Title();
            label.color = Color.white;
        }

        /// <summary>Pets-ii lui (model:raritate, separati prin virgula) - doar il urmeaza, fara lupta.</summary>
        void BuildPets(string list)
        {
            foreach (var p in pets) if (p != null) Destroy(p);
            pets.Clear();
            if (string.IsNullOrEmpty(list)) return;
            int i = 0;
            foreach (var part in list.Split(','))
            {
                var kv = part.Split(':');
                if (kv.Length < 2 || string.IsNullOrEmpty(kv[0])) continue;
                int r; int.TryParse(kv[1], out r);
                var go = new GameObject("RemotePet " + kv[0]);
                go.transform.SetParent(SceneGroups.Get("Remote Players"), false);
                go.transform.position = transform.position + new Vector3(Mathf.Cos(i), 0, Mathf.Sin(i)) * 2f;
                go.AddComponent<RemotePet>().Init(this, i, Mathf.Clamp(r, 0, 5), kv[0]);
                pets.Add(go);
                if (++i >= 4) break;
            }
        }

        string Title()
        {
            string t = "";
            if (!string.IsNullOrEmpty(title)) t += "<size=18><color=#" + ColorUtility.ToHtmlStringRGB(titleCol) + ">" + title.ToUpper() + "</color></size>\n";
            if (cls > 0) t += GameData.ClassTagRich(cls, clsTier, 20);
            t += "<color=#" + ColorUtility.ToHtmlStringRGB(Player.PowerTitleCol(power)) + ">" + pname + "</color>  <size=16><color=#b8c4d6>Lv " + level + "</color></size>";
            t += "\n<size=17><color=#ffb347>POWER</color> " + Fmt.Num(power) + "</size>";
            return t;
        }

        void BuildWeapon()
        {
            if (weapon != null) Destroy(weapon.gameObject);
            weapon = null;
            if (model == null || string.IsNullOrEmpty(weaponJson)) return;
            Item w = HeroGear.FromJson(weaponJson);
            if (w == null) return;
            var saved = transform.rotation;
            transform.rotation = Quaternion.identity;
            // aceeasi pozitie a mainii ca la eroul tau (modele Kenney in poza "T")
            Transform arm = ModelLib.FindDeep(model, "arm-right");
            var pivot = new GameObject("WeaponPivot").transform;
            float L = Player.HeroHeight * 0.38f;
            if (arm != null)
            {
                float side = Mathf.Sign(arm.position.x - model.position.x); if (side == 0f) side = 1f;
                float handX = arm.position.x + side * Player.HeroHeight * 0.22f;
                Bounds b;
                if (ModelLib.GetBounds(model.gameObject, out b)) handX = (side > 0 ? b.max.x : b.min.x) - side * Player.HeroHeight * 0.05f;
                pivot.SetParent(arm, true);
                pivot.position = new Vector3(handX, arm.position.y - Player.HeroHeight * 0.02f, arm.position.z + Player.HeroHeight * 0.03f);
                pivot.rotation = Quaternion.Euler(-25f, 0, 0);
            }
            else
            {
                pivot.SetParent(model, false);
                pivot.localPosition = new Vector3(0.7f, 1.1f, 0.3f);
            }
            var tilt = new GameObject("Tilt").transform;
            tilt.SetParent(pivot, false);
            tilt.localRotation = Quaternion.Euler(90f, 0, 0);
            var mesh = GearVisuals.BuildWeapon(w, tilt, L);
            if (mesh != null) mesh.localPosition = new Vector3(0, -L * 0.15f, 0);
            pivot.localScale = Vector3.one * HeroGear.WeaponScale(w);   // aceeasi marime ca pe ecranul lui
            weapon = pivot;
            transform.rotation = saved;
        }

        public void SetState(Vector3 p, float yaw, byte fl, byte sw)
        {
            lastState = Time.time;
            if (!hasState)
            {
                hasState = true;
                transform.position = p;
                transform.rotation = Quaternion.Euler(0, yaw, 0);
                gameObject.SetActive(true);
                Fx.Poof(p + Vector3.up, new Color(0.7f, 0.9f, 1f), 0.8f);
            }
            vel = (p - netPos) * 10f;   // ~10 mesaje pe secunda
            if ((p - netPos).sqrMagnitude > 15f * 15f) transform.position = p;   // teleport / respawn
            netPos = p; netYaw = yaw;
            bool wasDead = Dead;
            flags = fl;
            if (anim != null && Dead && !wasDead) { anim.Speed = 1f; anim.Play("die", 0.1f, false, true); }
            if (sw != swing)
            {
                if (swing != 255 && anim != null && !Dead)
                {
                    anim.Speed = 1.6f;
                    anim.Play(anim.Has("attack-melee-right") ? "attack-melee-right" : "attack-melee-left", 0.05f, false, true);
                    attackT = 0.32f;
                }
                swing = sw;
            }
        }

        public void Say(string text)
        {
            if (bubble == null || Game.I == null) return;
            bubble.text = text.Length > 60 ? text.Substring(0, 60) + "..." : text;
            bubbleT = 5f;
        }

        float attackT;

        void Update()
        {
            var g = Game.I;
            if (g == null || g.hud == null) return;
            if (label == null)
            {
                label = g.hud.Label(transform.position + Vector3.up * 2.9f, Title(), Color.white, 22f, 40f);
                label.verticalOverflow = VerticalWrapMode.Overflow;
                bubble = g.hud.Label(transform.position + Vector3.up * 3.3f, "", Color.white, 18f, 30f);
            }
            float dt = Time.deltaTime;
            // netezire: mergem spre ultima pozitie primita (+ putina predictie)
            Vector3 target = netPos + vel * 0.05f;
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-12f * dt));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, netYaw, 0), 1f - Mathf.Exp(-12f * dt));
            Vector3 hv = vel; hv.y = 0;
            bool moving = hv.magnitude > 0.8f;
            if (attackT > 0f) attackT -= dt;
            if (anim != null && !Dead && attackT <= 0f)
            {
                bool sprint = (flags & 2) != 0;
                if (moving) { anim.Play(sprint && anim.Has("sprint") ? "sprint" : "walk", 0.12f); anim.Speed = sprint ? 1.25f : 1.35f; }
                else { anim.Play("idle", 0.2f); anim.Speed = 1f; }
            }
            g.hud.MoveLabel(label, transform.position + Vector3.up * 2.9f);
            if (bubbleT > 0f)
            {
                bubbleT -= dt;
                g.hud.MoveLabel(bubble, transform.position + Vector3.up * 3.3f);
                if (bubbleT <= 0f) bubble.text = "";
            }
        }

        void OnDestroy()
        {
            var g = Game.I;
            if (g != null && g.hud != null) { g.hud.RemoveLabel(label); g.hud.RemoveLabel(bubble); }
            foreach (var p in pets) if (p != null) Destroy(p);
        }
    }

    /// <summary>Pet-ul unui jucator din multiplayer: il urmeaza (merge / sta), fara sa atace.</summary>
    public class RemotePet : MonoBehaviour
    {
        RemotePlayer owner;
        int index;
        SimpleAnimator anim;
        float seed;

        public void Init(RemotePlayer o, int idx, int r, string model)
        {
            owner = o; index = idx; seed = Random.value * 10f;
            string path = GameData.PETS + model;
            var vis = ModelLib.Spawn(path, transform, 0.9f + r * 0.08f);
            if (vis != null) { anim = SimpleAnimator.Setup(vis, path); if (anim != null) anim.Play("idle"); }
        }

        void Update()
        {
            if (owner == null || !owner.gameObject.activeInHierarchy) { if (owner == null) Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            Vector3 op = owner.transform.position;
            float a = index * 1.57f + 0.9f + Mathf.Sin(Time.time * 0.3f + seed) * 0.3f;
            Vector3 goal = op + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.2f;
            Vector3 d = goal - transform.position; d.y = 0;
            if (d.magnitude > 25f) { transform.position = goal; d = Vector3.zero; }
            bool moving = d.magnitude > 0.4f;
            if (moving)
            {
                transform.position += d.normalized * Mathf.Min(d.magnitude, 9f * dt * Mathf.Clamp(d.magnitude, 1f, 3f));
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 1f - Mathf.Exp(-10f * dt));
            }
            var p = transform.position; p.y = op.y; transform.position = p;
            if (anim != null) anim.Play(moving ? (anim.Has("walk") ? "walk" : "idle") : "idle", 0.2f);
        }
    }
}
