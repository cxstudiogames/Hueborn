// =====================================================================
//  AdminAccess.cs - cine vede butonul ADMIN (si tasta F1):
//   * mereu in Unity Editor
//   * contul tau online (PlayerId-urile din AdminIds - adauga aici si pe
//     cel de pe telefon dupa ce te conectezi; il vezi in ADMIN > TOOLS)
//   * orice dispozitiv deblocat cu CODUL DE ADMIN: SETTINGS > apasa de 5 ori
//     pe "HUEBORN: LOOT LEGENDS vX" jos > scrii codul. Ramane deblocat pe acel dispozitiv
//     (si online si offline). LOCK ADMIN il scoate.
//  Codul nu e scris in clar in joc (doar amprenta lui SHA-256).
//  ATENTIE: puterile reale asupra altor jucatori (ban, cadouri) trebuie
//  verificate si pe server (Cloud Code) cand le facem online.
// =====================================================================
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ColorRPG
{
    public static class AdminAccess
    {
        const string Key = "crpg_admin_ok";
        const string Salt = "colorrpg.admin.v1|";
        // SHA-256(Salt + cod). Ca sa schimbi codul: calculeaza amprenta noului cod si inlocuieste-o aici.
        const string CodeHash = "c326a58cc05cfbe318af4a7d63cf8d4eb802ccab062f419350132574ce93ae65";

        /// <summary>Conturile online de admin (Unity Authentication PlayerId).</summary>
        static readonly string[] AdminIds =
        {
            "Stshxoe7bkzjHgH6L7Y0ZJZY53Od",   // Ionutcx (PC / editor)
        };

        /// <summary>Contul principal de admin (de la el se citeste ora ultimului RESET LEADERBOARDS).</summary>
        public static string MainId { get { return AdminIds[0]; } }
        public static bool IsAdminId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (var a in AdminIds) if (a == id) return true;
            return false;
        }

        public static bool Unlocked { get { return PlayerPrefs.GetInt(Key, 0) == 1; } }

        public static bool IsAdmin
        {
            get
            {
                if (Application.isEditor || Unlocked) return true;
                string id = ColorRPG.Net.Ugs.PlayerId;
                if (!string.IsNullOrEmpty(id)) foreach (var a in AdminIds) if (a == id) return true;
                return false;
            }
        }

        static string Hash(string s)
        {
            using (var sha = SHA256.Create())
            {
                var b = sha.ComputeHash(Encoding.UTF8.GetBytes(Salt + s));
                var sb = new StringBuilder();
                foreach (var x in b) sb.Append(x.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>Verifica codul; daca e corect, deblocheaza ADMIN pe acest dispozitiv.</summary>
        public static bool TryUnlock(string code)
        {
            code = (code ?? "").Trim().ToUpperInvariant();
            if (code.Length == 0 || Hash(code) != CodeHash) return false;
            PlayerPrefs.SetInt(Key, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static void Lock()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
