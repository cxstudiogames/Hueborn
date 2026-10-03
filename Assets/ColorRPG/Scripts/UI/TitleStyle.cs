// =====================================================================
//  TitleStyle.cs - cu cat un titlu e mai greu de obtinut, cu atat arata
//  mai "legendar":
//    0 COMMON     - fontul jocului
//    1 RARE       - contur colorat
//    2 EPIC       - font clasic (Pagella), - NUME -
//    3 LEGENDARY  - font caligrafic (TeX Gyre Chorus), « Nume », contur auriu
//    4 MYTHIC     - caligrafic + culoare care straluceste (animata)
//  Acelasi stil e folosit si pentru litera de AWAKENING de langa nume.
// =====================================================================
using UnityEngine;
using UnityEngine.UI;

namespace ColorRPG
{
    public static class TitleStyle
    {
        public static readonly string[] TierNames = { "COMMON", "RARE", "EPIC", "LEGENDARY", "MYTHIC" };
        static Font serif, fancy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { serif = null; fancy = null; }

        public static Font Serif { get { if (serif == null) serif = Resources.Load<Font>("UI/PagellaBold"); return serif != null ? serif : UIKit.Font; } }
        public static Font Fancy { get { if (fancy == null) fancy = Resources.Load<Font>("UI/TitleFont"); return fancy != null ? fancy : Serif; } }

        static string TitleCase(string s)
        {
            var parts = s.ToLowerInvariant().Split(' ');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Length > 0 && char.IsLetter(parts[i][0])) parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            return string.Join(" ", parts);
        }

        /// <summary>Textul afisat pentru un titlu pe treapta "tier".</summary>
        public static string Decorate(string name, int tier)
        {
            if (string.IsNullOrEmpty(name)) return "";
            switch (tier)
            {
                case 2: return "- " + name + " -";
                case 3: return "« " + TitleCase(name) + " »";
                case 4: return "«« " + TitleCase(name) + " »»";
                default: return name;
            }
        }

        /// <summary>Aplica stilul pe un Text (font, marime, contur, stralucire).</summary>
        public static void Apply(Text t, string name, Color col, int tier, int baseSize)
        {
            if (t == null) return;
            tier = Mathf.Clamp(tier, 0, 4);
            var fx = t.GetComponent<TitleFx>();
            if (fx == null) fx = t.gameObject.AddComponent<TitleFx>();
            fx.Set(t, col, tier);
            t.font = tier >= 3 ? Fancy : tier == 2 ? Serif : UIKit.Font;
            float mul = tier >= 3 ? 1.3f : tier == 2 ? 1.08f : 1f;
            t.fontSize = Mathf.RoundToInt(baseSize * UIKit.FontScale * mul);
            t.text = Decorate(name, tier);
            t.color = col;
            var o = t.GetComponent<Outline>();
            if (o != null)
            {
                Color dark = new Color(col.r * 0.25f, col.g * 0.25f, col.b * 0.25f, 0.95f);
                o.effectColor = tier == 0 ? new Color(0.04f, 0.04f, 0.06f, 0.95f) : tier >= 3 ? new Color(0.35f, 0.22f, 0.02f, 0.95f) : dark;
                o.effectDistance = tier >= 3 ? new Vector2(2.4f, -2.4f) : new Vector2(2f, -2f);
            }
        }
    }

    /// <summary>Stralucirea titlurilor MYTHIC (culoarea "curge" spre alb/auriu si inapoi).</summary>
    public class TitleFx : MonoBehaviour
    {
        Text t; Color baseCol; int tier;

        public void Set(Text text, Color c, int tr) { t = text; baseCol = c; tier = tr; enabled = tr >= 4; }

        void Update()
        {
            if (t == null || tier < 4) return;
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.6f);
            Color glow = Color.Lerp(new Color(1f, 0.95f, 0.6f), Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.3f));
            t.color = Color.Lerp(baseCol, glow, k * 0.65f);
        }
    }
}
