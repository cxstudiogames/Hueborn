// =====================================================================
//  ChatFilter.cs - protectia chat-ului:
//   * anti-spam: minim 2s intre mesaje, max 4 mesaje in 15s, fara acelasi mesaj repetat,
//     fara litere/simboluri repetate la infinit si fara mesaje scrise doar cu MAJUSCULE lungi
//   * fara link-uri (http, www, .com, discord...)
//   * injuraturi / insulte in mai multe limbi -> ***  (prinde si "l33t" si litere despartite)
// =====================================================================
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ColorRPG
{
    public static class ChatFilter
    {
        public const int MaxLength = 120;
        const float MinGap = 2f, Window = 15f;
        const int MaxInWindow = 4;

        static readonly List<float> sent = new List<float>();
        static string lastMsg = "";
        static float lastMsgT = -100f, muteUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { sent.Clear(); lastMsg = ""; lastMsgT = -100f; muteUntil = 0f; }

        static readonly Regex LinkRx = new Regex(
            @"(https?\s*:\s*/\s*/|www\s*\.|discord\s*\.?\s*(gg|com|app)|\b[a-z0-9-]+\s*(\.|\(dot\)|\[dot\])\s*(com|net|org|ro|io|gg|xyz|ru|de|co|me|tk|ly|link|info|biz|site|online|shop|app|dev)\b)",
            RegexOptions.IgnoreCase);

        // injuraturi / insulte (en, ro, es, fr, de, it, pt, pl, ru translit, tr, nl, hu). Se compara dupa normalizare.
        static readonly string[] Bad =
        {
            // engleza
            "fuck", "fuk", "fck", "shit", "bitch", "cunt", "dick", "pussy", "asshole", "bastard", "whore", "slut", "retard", "nigger", "nigga", "faggot", "fag",
            "motherfucker", "wanker", "twat", "cock", "douche", "kys", "killyourself", "stfu", "dumbass", "jackass", "prick", "bollocks",
            // romana
            "pula", "pizda", "muie", "fut", "futu", "fute", "futai", "cacat", "curva", "curve", "coaie", "coaiele", "sugi", "sugipula", "handicapat", "bulangiu",
            "poponar", "labagiu", "laba", "dracu", "mortii", "mamatii", "mortiimatii", "tactu", "prost", "idiot", "imbecil", "jegos", "tigan", "cretin", "zdreanta",
            // spaniola / portugheza
            "puta", "puto", "mierda", "cabron", "pendejo", "joder", "coño", "cono", "maricon", "gilipollas", "caralho", "porra", "merda", "foda", "viado", "buceta",
            // franceza
            "merde", "putain", "salope", "connard", "connasse", "encule", "batard", "nique", "pute",
            // germana / olandeza
            "scheisse", "scheiße", "arschloch", "hurensohn", "fotze", "wichser", "schlampe", "kut", "kanker", "klootzak", "lul",
            // italiana
            "cazzo", "stronzo", "puttana", "vaffanculo", "minchia", "troia", "coglione",
            // poloneza / rusa (translit) / turca / maghiara
            "kurwa", "chuj", "jebac", "pierdol", "suka", "blyat", "blyad", "pizdec", "nahui", "orospu", "siktir", "amk", "kurva", "geci", "baszd",
        };
        // cuvinte scurte care sunt verificate doar ca cuvant intreg (altfel ar prinde cuvinte nevinovate: "class", "cocktail"...)
        static readonly HashSet<string> WholeOnly = new HashSet<string> { "fag", "cock", "lul", "kut", "fut", "futu", "fute", "laba", "prost", "puta", "puto", "pute", "amk", "suka", "kys", "dick", "cono", "porra", "merda", "fuk", "fck", "sugi", "geci", "curve", "tactu", "idiot", "cretin", "tigan", "pula", "dracu", "nique", "troia", "curva", "lula", "muie" };

        static string Norm(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char ch in s.ToLowerInvariant())
            {
                char c = ch;
                switch (c)
                {
                    case '0': c = 'o'; break;
                    case '1': case '!': case '|': c = 'i'; break;
                    case '3': c = 'e'; break;
                    case '4': case '@': c = 'a'; break;
                    case '5': case '$': c = 's'; break;
                    case '7': c = 't'; break;
                    case 'ă': case 'â': case 'á': case 'à': case 'ä': c = 'a'; break;
                    case 'î': case 'í': case 'ì': c = 'i'; break;
                    case 'ș': case 'ş': c = 's'; break;
                    case 'ț': case 'ţ': c = 't'; break;
                    case 'é': case 'è': case 'ê': c = 'e'; break;
                    case 'ó': case 'ö': case 'ő': c = 'o'; break;
                    case 'ú': case 'ü': c = 'u'; break;
                    case 'ñ': c = 'n'; break;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>Scoate literele repetate (fuuuuck -> fuck).</summary>
        static string Squeeze(string s)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++) if (i == 0 || s[i] != s[i - 1]) sb.Append(s[i]);
            return sb.ToString();
        }

        /// <summary>Contine o injuratura (si ascunsa: f.u.c.k, f u c k, fuuuck, sh1t)?</summary>
        public static bool HasProfanity(string s)
        {
            string n = Norm(s);
            var words = new List<string>();
            foreach (var w in Regex.Split(n, @"[^a-zа-я]+")) if (w.Length > 0) words.Add(Squeeze(w));
            // literele despartite ("f u c k", "p.u.l.a") se lipesc intr-un singur cuvant
            var run = new StringBuilder();
            var extra = new List<string>();
            foreach (var w in words)
            {
                if (w.Length == 1) run.Append(w);
                else { if (run.Length > 2) extra.Add(Squeeze(run.ToString())); run.Length = 0; }
            }
            if (run.Length > 2) extra.Add(Squeeze(run.ToString()));
            words.AddRange(extra);
            foreach (var bad in Bad)
            {
                string b = Squeeze(Norm(bad));
                bool whole = WholeOnly.Contains(bad);
                foreach (var w in words)
                    if (whole ? w == b : w.Contains(b)) return true;
            }
            return false;
        }

        /// <summary>Inlocuieste cuvintele urate cu *** (pentru mesajele altora).</summary>
        public static string Censor(string s)
        {
            var words = s.Split(' ');
            for (int i = 0; i < words.Length; i++)
                if (words[i].Length > 0 && HasProfanity(words[i])) words[i] = new string('*', Mathf.Clamp(words[i].Length, 3, 8));
            return string.Join(" ", words);
        }

        /// <summary>Verifica mesajul tau inainte sa fie trimis. Intoarce null daca e OK, altfel motivul.</summary>
        public static string Check(ref string msg)
        {
            float now = Time.unscaledTime;
            msg = (msg ?? "").Trim();
            msg = Regex.Replace(msg, @"\s+", " ");
            if (msg.Length == 0) return "";
            if (msg.Length > MaxLength) msg = msg.Substring(0, MaxLength);
            if (now < muteUntil) return "You are sending messages too fast - wait " + Mathf.CeilToInt(muteUntil - now) + "s.";
            sent.RemoveAll(t => now - t > Window);
            if (now - (sent.Count > 0 ? sent[sent.Count - 1] : -100f) < MinGap) return "Slow down! Wait a moment between messages.";
            if (sent.Count >= MaxInWindow) { muteUntil = now + 10f; return "Too many messages! Chat paused for 10s."; }
            string key = Squeeze(Norm(msg));
            if (key == lastMsg && now - lastMsgT < 30f) return "Don't repeat the same message.";
            if (Regex.IsMatch(msg, @"(.)\1{7,}")) return "Please don't spam characters.";
            if (LinkRx.IsMatch(msg)) return "Links are not allowed in chat.";
            if (HasProfanity(msg)) return "Please be nice - that message was blocked.";
            int letters = 0, upper = 0;
            foreach (char c in msg) if (char.IsLetter(c)) { letters++; if (char.IsUpper(c)) upper++; }
            if (letters > 12 && upper > letters * 0.8f) msg = msg.ToLowerInvariant();   // fara "TIPAT"
            sent.Add(now);
            lastMsg = key; lastMsgT = now;
            return null;
        }

        /// <summary>Mesaj primit de la alt jucator: fara link-uri (null = nu se afiseaza), injuraturile devin ***.</summary>
        public static string CleanIncoming(string msg)
        {
            msg = Regex.Replace((msg ?? "").Trim(), @"\s+", " ");
            if (msg.Length == 0 || LinkRx.IsMatch(msg)) return null;
            if (msg.Length > MaxLength) msg = msg.Substring(0, MaxLength);
            return Censor(msg);
        }

        /// <summary>Numele altui jucator: fara tag-uri de culoare, max 24 caractere.</summary>
        public static string CleanName(string n)
        {
            n = (n ?? "").Replace("<", "").Replace(">", "").Trim();
            if (n.Length == 0) n = "Player";
            if (n.Length > 24) n = n.Substring(0, 24);
            return HasProfanity(n) ? "Player" : n;
        }
    }
}
