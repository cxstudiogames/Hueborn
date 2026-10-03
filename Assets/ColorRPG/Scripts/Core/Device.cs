// =====================================================================
//  Device.cs - PC sau TELEFON. Pe telefon (Android/iOS) jocul arata
//  joystick-ul, butoanele JUMP / FIGHT, iconite mai mari si ascunde
//  tastele (1/2/3, E, I...). Pe PC ramane totul ca inainte.
//  In editor poti testa modul telefon din ADMIN > PLAYER (SIMULATE MOBILE).
// =====================================================================
using UnityEngine;

namespace ColorRPG
{
    public static class Device
    {
        static int forced = -1;   // -1 = automat, 0 = PC, 1 = telefon

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { forced = -1; }

        /// <summary>true pe telefon / tableta (sau cand e simulat din ADMIN).</summary>
        public static bool Mobile
        {
            get
            {
                if (forced < 0) forced = PlayerPrefs.GetInt("crpg_force_mobile", 0) == 1 ? 1 : -2;
                if (forced == 1) return true;
                return Application.isMobilePlatform;
            }
        }

        public static bool Simulated { get { return PlayerPrefs.GetInt("crpg_force_mobile", 0) == 1; } }

        /// <summary>Doar pentru test (ADMIN): porneste / opreste modul telefon pe PC. Se aplica la urmatoarea pornire a jocului.</summary>
        public static void SetSimulated(bool on)
        {
            PlayerPrefs.SetInt("crpg_force_mobile", on ? 1 : 0);
            PlayerPrefs.Save();
            forced = on ? 1 : -2;
        }

        /// <summary>Textul unei taste - gol pe telefon (acolo nu exista taste).</summary>
        public static string Key(string k) { return Mobile ? "" : k; }

        /// <summary>Scoate "[E] ", "[Q] " etc. din texte pe telefon (acolo se apasa direct pe mesaj).</summary>
        public static string StripKeys(string s)
        {
            if (!Mobile || string.IsNullOrEmpty(s)) return s;
            return System.Text.RegularExpressions.Regex.Replace(s, @"\[[A-Z0-9]{1,5}\]\s?", "").Replace("(potion: Q = ", "(potion: ");
        }

        /// <summary>"[E] " pe PC, nimic pe telefon.</summary>
        public static string KeyTag(string k) { return Mobile ? "" : "[" + k + "] "; }
    }
}
