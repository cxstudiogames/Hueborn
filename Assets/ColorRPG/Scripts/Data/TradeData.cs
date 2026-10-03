// =====================================================================
//  TradeData.cs - date pentru Trade Plaza: servere cu 18 jucatori,
//  10 booth-uri (tarabe) pe server, listari de iteme intre jucatori.
//  NPC-urile nu mai vand iteme: itemele pica doar de pe harti.
// =====================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorRPG
{
    /// <summary>Un item pus la vanzare pe booth-ul jucatorului.</summary>
    [Serializable]
    public class BoothListing
    {
        public int kind;          // 0 = item, 1 = pet, 2 = material (vezi TradeDB.Mat*)
        public Item item;
        public PetData pet;
        public int mat, amount;   // material: tip + cantitate
        public long price;        // in TOKENS
        public float listedAt;    // playTime la momentul listarii

        public string Name { get { return TradeDB.ListingName(kind, item, pet, mat, amount); } }
        public Color Col { get { return TradeDB.ListingCol(kind, item, pet, mat); } }
        public long Fair { get { return TradeDB.ListingValue(kind, item, pet, mat, amount); } }
    }

    /// <summary>(Salvari vechi) oferta dintr-un magazin NPC - nu mai e folosita.</summary>
    [Serializable]
    public class NpcOffer
    {
        public int booth;
        public Item item;
        public long coins;
        public int ore;
        public bool sold;
    }

    /// <summary>Un item de pe booth-ul altui jucator (simulat).</summary>
    public class SimListing
    {
        public int kind;
        public Item item;
        public PetData pet;
        public int mat, amount;
        public long price;        // in TOKENS
        public bool sold;
        public int netIndex;      // multiplayer: pozitia listarii la vanzator
        public string sig;        // multiplayer: semnatura (sa stim ca e aceeasi listare)
        public string Name { get { return TradeDB.ListingName(kind, item, pet, mat, amount); } }
        public Color Col { get { return TradeDB.ListingCol(kind, item, pet, mat); } }
    }

    /// <summary>Booth-ul altui jucator de pe server.</summary>
    public class SimBooth
    {
        public string owner;
        public string hero;
        public Color col;
        public ulong netOwner;    // multiplayer: jucatorul real care are booth-ul (0 = simulat)
        public readonly List<SimListing> stock = new List<SimListing>();
    }

    /// <summary>Un server din Trade Plaza: 18 jucatori, 10 booth-uri.</summary>
    public class PlazaServer
    {
        public int id;
        public int players;                                                     // cu tine inclus
        public readonly SimBooth[] booths = new SimBooth[TradeDB.BoothCount];   // null = liber (sau al tau)
        public int Occupied { get { int n = 0; foreach (var b in booths) if (b != null) n++; return n; } }
    }

    public static class TradeDB
    {
        public const int MaxListings = 18;          // iteme, pets si materiale pe booth
        public const int BoothCount = 10;
        public const int MaxPlayers = 18;
        public const int ServerCount = 40;
        public const float RestockSeconds = 900f;              // booth-urile celorlalti se schimba la 15 minute
        public const float OfflineCapSeconds = 24f * 3600f;

        public static readonly Color[] BoothCols =
        {
            new Color(1f, 0.45f, 0.35f), new Color(0.45f, 0.9f, 0.5f), new Color(0.45f, 0.65f, 1f), new Color(0.85f, 0.5f, 1f),
            new Color(1f, 0.8f, 0.25f), new Color(0.45f, 0.95f, 1f), new Color(1f, 0.55f, 0.8f), new Color(0.6f, 1f, 0.35f),
            new Color(1f, 0.65f, 0.3f), new Color(0.7f, 0.7f, 1f),
        };

        /// <summary>Valoarea "de piata" a unui item in coins (vechi).</summary>
        public static long FairValue(Item it)
        {
            return Math.Max(5L, (long)Mathf.Round(ItemDB.SellValue(it) * 2.4f));
        }

        // ---------------- TOKENS: toate schimburile din piata se fac in tokens ----------------
        public const int MatStones = 0, MatCloak = 1, MatPotXp = 2, MatPotDmg = 3, MatPotCoin = 4;
        public static readonly string[] MatNames = { "Forge Stone", "Guardian Cloak", "XP Potion (x2)", "Damage Potion (x1.5)", "Coin Potion (x1.5)" };
        public static readonly Color[] MatCols = { new Color(0.75f, 0.8f, 0.95f), new Color(0.75f, 0.45f, 1f), new Color(0.4f, 0.9f, 1f), new Color(1f, 0.4f, 0.35f), new Color(1f, 0.85f, 0.25f) };
        /// <summary>Pretul corect (tokens) pentru 1 bucata din material.</summary>
        public static readonly float[] MatUnitTokens = { 0.4f, 14f, 3f, 3f, 3f };
        public static readonly int[] MatIcon = { 16, 17, 36, 37, 38 };

        public static long TokenValue(Item it)
        {
            if (it == null) return 1;
            float[] r = { 1f, 2f, 4f, 9f, 20f, 45f };
            float v = r[Mathf.Clamp(it.rarity, 0, 5)] * (1f + it.tier * 0.9f) * (1f + it.plus * 0.12f);
            return Math.Max(1L, (long)Mathf.Round(v));
        }
        public static long PetValue(PetData p)
        {
            if (p == null) return 1;
            float[] r = { 2f, 4f, 9f, 20f, 45f, 100f };
            return Math.Max(1L, (long)Mathf.Round(r[Mathf.Clamp(p.rarity, 0, 5)] * (1f + p.map * 1.1f)));
        }
        public static long MatValue(int mat, int amount)
        {
            return Math.Max(1L, (long)Mathf.Round(MatUnitTokens[Mathf.Clamp(mat, 0, MatUnitTokens.Length - 1)] * amount));
        }
        public static long ListingValue(int kind, Item it, PetData p, int mat, int amount)
        {
            return kind == 1 ? PetValue(p) : kind == 2 ? MatValue(mat, amount) : TokenValue(it);
        }
        public static string ListingName(int kind, Item it, PetData p, int mat, int amount)
        {
            if (kind == 1) return p == null ? "?" : ItemDB.RarityNames[p.rarity] + " " + GameData.PetName(p.model) + " (pet)";
            if (kind == 2) return amount + "x " + MatNames[Mathf.Clamp(mat, 0, MatNames.Length - 1)];
            return it == null ? "?" : ItemDB.FullName(it);
        }
        public static Color ListingCol(int kind, Item it, PetData p, int mat)
        {
            if (kind == 1) return p == null ? Color.white : ItemDB.Col(p.rarity);
            if (kind == 2) return MatCols[Mathf.Clamp(mat, 0, MatCols.Length - 1)];
            return it == null ? Color.white : ItemDB.Col(it.rarity);
        }

        /// <summary>Probabilitatea pe secunda ca un item de pe booth sa se vanda.</summary>
        public static float SaleRate(BoothListing l)
        {
            float ratio = l.price / (float)Math.Max(1L, l.Fair);
            // la fair price ~ o vanzare la 2 minute; mai ieftin = mai repede, mai scump = mai rar
            float f = Mathf.Clamp((1.9f - ratio) / 0.9f, 0.02f, 2.2f);
            return f * f / 120f;
        }

        public static string RateLabel(BoothListing l)
        {
            float ratio = l.price / (float)Math.Max(1L, l.Fair);
            if (ratio < 0.8f) return "<color=#66ff77>BARGAIN - sells fast</color>";
            if (ratio < 1.15f) return "<color=#ccff66>fair price</color>";
            if (ratio < 1.5f) return "<color=#ffcc55>a bit pricey - sells slowly</color>";
            return "<color=#ff6666>too expensive - almost nobody buys</color>";
        }

        public static long Now()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public static string[] ShopperNames =
        {
            "Mihai", "Ana", "Luca", "Ioana", "Vlad", "Maria", "Radu", "Elena", "Tudor", "Sara", "Dragos", "Irina",
            "xX_Slayer_Xx", "NoobMaster", "LootGoblin", "PetLover99", "ProForger", "CritQueen", "DragonBoi", "Miruna",
            "Andrei_77", "ShadowFox", "Bianca", "Cosmin", "GoldHunter", "Teo", "Alex_RO", "PixelKnight"
        };
    }
}
