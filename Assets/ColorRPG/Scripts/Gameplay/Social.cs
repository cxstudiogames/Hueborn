// =====================================================================
//  Social.cs - jucatorii (deocamdata simulati), prieteni online/offline,
//  mesaje private (FRIENDS in chat) si "baza de date" de moderare pentru
//  ADMIN (kick, ban, mute, restrictii, cadouri, giveaway-uri).
//  Cand jocul va fi online, functiile de aici vor vorbi cu serverul -
//  restul jocului (UI-ul) ramane la fel.
// =====================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    public static class Social
    {
        static string[] directory;

        /// <summary>Toti jucatorii cunoscuti (simulati) - pentru cautare de prieteni si pentru ADMIN.</summary>
        public static string[] Directory
        {
            get
            {
                if (directory != null) return directory;
                var l = new List<string>(TradeDB.ShopperNames);
                string[] more = { "Stefan", "Catalina", "Bogdan", "Daria", "Ionut", "Larisa", "MegaLooter", "SilentBlade", "FrostByte", "LavaLord",
                                  "EggHatcher", "ForgeMaster", "Sunny_Day", "NightOwl", "CoinCollector", "TankMode", "Ninja_RO", "QueenOfPets" };
                foreach (var m in more) if (!l.Contains(m)) l.Add(m);
                directory = l.ToArray();
                return directory;
            }
        }

        public static string[] Search(string q, int max = 12)
        {
            q = (q ?? "").Trim().ToLowerInvariant();
            var res = new List<string>();
            foreach (var n in Directory)
                if (q.Length == 0 || n.ToLowerInvariant().Contains(q)) { res.Add(n); if (res.Count >= max) break; }
            return res.ToArray();
        }

        static int Hash(string s)
        {
            unchecked
            {
                int h = 23;
                foreach (char c in s.ToLowerInvariant()) h = h * 31 + c;
                return h & 0x7fffffff;
            }
        }

        /// <summary>Simulat: cine e online se schimba la fiecare 10 minute (cand jocul e online, vine de la server).</summary>
        public static bool IsOnline(string name)
        {
            if (Mp.Online) return Mp.HasPeer(name);   // multiplayer: online = in camera ta
            long bucket = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMinute / 10;
            return (Hash(name) + bucket * 7) % 5 < 2;
        }

        public static int FakeLevel(string name) { return 5 + Hash(name) % (GameData.MaxLevel - 4); }

        static readonly string[] Replies =
        {
            "hey! :D", "whats up?", "sure, see you in the plaza", "lol nice", "im farming lava rn", "gg!", "brb 5 min",
            "can you help me with the boss?", "trade later?", "thanks!!", "ok", "which map are you on?"
        };
        public static string RandomReply() { return Replies[UnityEngine.Random.Range(0, Replies.Length)]; }

        // ---------------------------------------------------------------
        //  Moderare (ADMIN) - salvata local pana cand jocul e online
        // ---------------------------------------------------------------
        [Serializable] public class Sanction { public string name, kind, reason; public long until; }   // until = UTC ticks, 0 = permanent
        [Serializable] public class LogEntry { public long time; public string text; }
        [Serializable] class Store { public List<Sanction> sanctions = new List<Sanction>(); public List<LogEntry> log = new List<LogEntry>(); }

        static Store store;
        const string Key = "crpg_admin_store";

        static Store S
        {
            get
            {
                if (store != null) return store;
                try { store = JsonUtility.FromJson<Store>(PlayerPrefs.GetString(Key, "")); } catch (Exception) { store = null; }
                if (store == null) store = new Store();
                if (store.sanctions == null) store.sanctions = new List<Sanction>();
                if (store.log == null) store.log = new List<LogEntry>();
                return store;
            }
        }

        static void Save() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(S)); PlayerPrefs.Save(); }

        public static void Log(string text)
        {
            S.log.Insert(0, new LogEntry { time = DateTime.UtcNow.Ticks, text = text });
            if (S.log.Count > 200) S.log.RemoveAt(S.log.Count - 1);
            Save();
        }

        public static List<LogEntry> AdminLog { get { return S.log; } }

        public static void Punish(string name, string kind, float hours, string reason)
        {
            S.sanctions.RemoveAll(x => x.name == name && x.kind == kind);
            long until = hours <= 0f ? 0 : DateTime.UtcNow.AddHours(hours).Ticks;
            S.sanctions.Add(new Sanction { name = name, kind = kind, reason = reason ?? "", until = until });
            Log(kind + " " + name + (hours > 0f ? " for " + DurText(hours) : " (permanent)") + (string.IsNullOrEmpty(reason) ? "" : " - " + reason));
        }

        public static void Lift(string name, string kind)
        {
            if (S.sanctions.RemoveAll(x => x.name == name && x.kind == kind) > 0) { Log("removed " + kind + " from " + name); }
        }

        public static Sanction Active(string name, string kind)
        {
            long now = DateTime.UtcNow.Ticks;
            S.sanctions.RemoveAll(x => x.until != 0 && x.until < now);
            return S.sanctions.Find(x => x.name == name && x.kind == kind);
        }

        public static List<Sanction> ActiveFor(string name)
        {
            long now = DateTime.UtcNow.Ticks;
            S.sanctions.RemoveAll(x => x.until != 0 && x.until < now);
            return S.sanctions.FindAll(x => x.name == name);
        }

        public static string DurText(float hours)
        {
            if (hours <= 0f) return "permanent";
            if (hours < 24f) return Mathf.RoundToInt(hours) + "h";
            return Mathf.RoundToInt(hours / 24f) + "d";
        }

        public static string LeftText(Sanction s)
        {
            if (s.until == 0) return "permanent";
            var left = new DateTime(s.until, DateTimeKind.Utc) - DateTime.UtcNow;
            if (left.TotalHours >= 24) return Mathf.CeilToInt((float)left.TotalDays) + "d left";
            if (left.TotalMinutes >= 60) return Mathf.CeilToInt((float)left.TotalHours) + "h left";
            return Mathf.Max(1, Mathf.CeilToInt((float)left.TotalMinutes)) + "m left";
        }

        /// <summary>Jucatorii banati / cu mute nu mai apar in chat-ul simulat.</summary>
        public static bool Silenced(string name) { return Active(name, "BAN") != null || Active(name, "MUTE") != null; }
    }
}
