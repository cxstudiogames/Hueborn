// =====================================================================
//  Pet.cs - pets care te urmeaza si ataca singuri inamicii din apropiere
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public class Pet : MonoBehaviour
    {
        Player owner;
        int index, rarity;
        SimpleAnimator anim;
        Transform vis;
        Enemy target;
        float atkT, hopT, seed, stride, moveW;
        Vector3 vel;
        bool moving;

        public void Init(Player p, int idx, int r, string model)
        {
            owner = p; index = idx; rarity = r;
            seed = Random.value * 10f;
            float size = 0.9f + r * 0.08f;
            string path = GameData.PETS + model;
            vis = ModelLib.Spawn(path, transform, size);
            if (vis != null)
            {
                anim = SimpleAnimator.Setup(vis, path);
                if (anim != null) anim.Play("idle");
            }
            else
            {
                vis = new GameObject("Vis").transform;
                vis.SetParent(transform, false);
                Color c = ItemDB.Col(r);
                Art.Prim(PrimitiveType.Sphere, new Vector3(0, 0.5f, 0), new Vector3(0.9f, 0.8f, 0.9f), c, vis, false);
                for (int s = -1; s <= 1; s += 2)
                {
                    Art.Prim(PrimitiveType.Sphere, new Vector3(s * 0.2f, 0.62f, 0.35f), Vector3.one * 0.22f, Color.white, vis, false);
                    Art.Prim(PrimitiveType.Sphere, new Vector3(s * 0.2f, 0.62f, 0.45f), Vector3.one * 0.11f, Color.black, vis, false);
                }
            }
            // (fara disc/aura sub pet - arata ca un cerc alb nenatural; raritatea se vede in fereastra PETS)
        }

        void Update()
        {
            if (owner == null) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            Vector3 op = owner.transform.position;

            // alege tinta: cel mai apropiat inamic de stapan (raza mica - pet-ul nu pleaca departe)
            bool ownerInArena = GameData.InArena(Game.I.mapIndex, op);
            if (target == null || target.Dying || Flat(target.transform.position - op) > 8.5f || (target.boss && !ownerInArena))
            {
                target = null;
                float best = 7f;
                foreach (var e in Enemy.All)
                {
                    if (e == null || e.Dying) continue;
                    if (e.boss && !ownerInArena) continue;   // boss-ul se ataca doar din arena
                    float d = Flat(e.transform.position - op);
                    if (d < best) { best = d; target = e; }
                }
            }

            Vector3 goal;
            float speed;
            if (target != null && !owner.dead)
            {
                Vector3 to = transform.position - target.transform.position; to.y = 0;
                if (to.sqrMagnitude < 0.01f) to = Vector3.forward;
                goal = target.transform.position + to.normalized * (target.Radius + 0.9f);
                speed = 14f;
                atkT -= dt;
                if (atkT <= 0f && Flat(goal - transform.position) < 1.2f)
                {
                    atkT = 1.1f + index * 0.07f;
                    hopT = 0.35f;
                    int dmg = Mathf.Max(1, Mathf.RoundToInt(owner.Damage * 0.12f * Player.PetMul[rarity]));
                    target.Hit(dmg, false, transform.position, true);
                    Fx.Sparkle(target.transform.position + Vector3.up, ItemDB.Col(rarity), 4, 0.4f);
                }
            }
            else
            {
                float a = Time.time * 0.6f + index * (Mathf.PI * 2f / Mathf.Max(1, owner.MaxPets));
                goal = op + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 3.3f;   // putin mai departe de erou (nu se mai inghesuie)
                speed = 10f;
            }

            float dist = Flat(goal - transform.position);
            Vector3 p = transform.position;
            if (Flat(p - op) > 18f) p = goal;
            else p = Vector3.MoveTowards(p, goal, speed * dt * Mathf.Clamp01(dist / 1.5f + 0.2f));
            Vector3 mv = p - transform.position; mv.y = 0;

            // viteza netezita + histerezis: pet-ul nu mai "tremura" intre mers si stat pe loc
            if (dt > 0f) vel = Vector3.Lerp(vel, mv / dt, 1f - Mathf.Exp(-8f * dt));
            float spd = vel.magnitude;
            if (!moving && spd > 0.9f) moving = true;
            else if (moving && spd < 0.35f) moving = false;
            moveW = Mathf.MoveTowards(moveW, moving ? 1f : 0f, dt * 3f);

            // salt la atac + mers legat de distanta parcursa (nu de timp) => fara topaieli rapide
            hopT -= dt;
            float hop = hopT > 0f ? Mathf.Sin((1f - hopT / 0.35f) * Mathf.PI) * 0.7f : 0f;
            stride += spd * dt * 2.4f;
            float bob = anim == null ? Mathf.Abs(Mathf.Sin(stride)) * 0.16f * moveW : 0f;
            p.y = 0.02f + hop + bob;
            transform.position = p;

            Vector3 look = target != null ? target.transform.position - transform.position : vel;
            look.y = 0;
            if (look.sqrMagnitude > 0.04f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 1f - Mathf.Exp(-6f * dt));

            if (anim != null)
                anim.Play(moving ? (spd > 7f && anim.Has("run") ? "run" : "walk") : "idle", 0.25f);
        }

        static float Flat(Vector3 v) { v.y = 0; return v.magnitude; }
    }
}
