// =====================================================================
//  Iap.cs - cumparaturi reale prin Google Play Billing (Unity IAP v5).
//  Jocul doar cere "Buy(id)"; cand Google confirma plata, OnDeliver(id)
//  e chemat (si pentru platile ramase neterminate de data trecuta).
//  Produsele (aceleasi id-uri) trebuie create in Google Play Console >
//  Monetize > Products. Fara pachet / fara magazin: Ready = false.
// =====================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
#if CRPG_IAP
using Unity.Services.Core;
using UnityEngine.Purchasing;
#endif

namespace ColorRPG.Net
{
    public static class Iap
    {
        public struct Def { public string id; public bool consumable; public Def(string i, bool c) { id = i; consumable = c; } }

        /// <summary>Magazinul e conectat si produsele au venit de la Google.</summary>
        public static bool Ready { get; private set; }
        public static string LastError { get; private set; }
        /// <summary>Chemat cand o plata e confirmata: jocul da obiectul. Intoarce true daca l-a dat.</summary>
        public static Func<string, bool> OnDeliver;
        /// <summary>Chemat cand o cumparare nu reuseste (anulata / eroare).</summary>
        public static Action<string, string> OnFailed;
        /// <summary>Cumparaturi permanente gasite la Google (reinstalare / telefon nou): jocul le reda in liniste.</summary>
        public static Action<string> OnRestore;

        static bool busy;
        static readonly Dictionary<string, string> prices = new Dictionary<string, string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Ready = false; busy = false; prices.Clear(); OnDeliver = null; OnFailed = null; OnRestore = null;
#if CRPG_IAP
            store = null;
#endif
        }

        /// <summary>Pretul in moneda jucatorului (de la Google), sau fallback.</summary>
        public static string Price(string id, string fallback)
        {
            string p;
            return prices.TryGetValue(id, out p) && !string.IsNullOrEmpty(p) ? p : fallback;
        }

#if CRPG_IAP
        static StoreController store;

        public static async void Init(List<Def> products)
        {
            if (Ready || busy) return;
            busy = true;
            try
            {
                // un singur InitializeAsync pentru tot jocul (IAP si UGS pornesc aproape deodata)
                while (UnityServices.State == ServicesInitializationState.Initializing) await System.Threading.Tasks.Task.Delay(100);
                if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync();
                store = UnityIAPServices.StoreController();
                store.OnPurchasePending += OnPending;
                store.OnPurchaseFailed += f =>
                {
                    string id = FirstId(f.CartOrdered);
                    Debug.LogWarning("[ColorRPG] Purchase failed: " + id + " " + f.FailureReason + " " + f.Details);
                    if (OnFailed != null) OnFailed(id, f.FailureReason.ToString());
                };
                store.OnProductsFetched += list =>
                {
                    foreach (var p in list) if (p != null && p.definition != null && p.metadata != null) prices[p.definition.id] = p.metadata.localizedPriceString;
                    Ready = true;
                    Debug.Log("[ColorRPG] Store ready: " + list.Count + " products");
                    store.FetchPurchases();   // plati neterminate de data trecuta -> OnPurchasePending
                };
                store.OnPurchasesFetched += orders =>
                {
                    if (orders == null || orders.ConfirmedOrders == null || OnRestore == null) return;
                    foreach (var o in orders.ConfirmedOrders)
                        foreach (var it in o.CartOrdered.Items())
                            if (it != null && it.Product != null && it.Product.definition != null && it.Product.definition.type == ProductType.NonConsumable)
                                OnRestore(it.Product.definition.id);
                };
                store.OnPurchasesFetchFailed += e => Debug.LogWarning("[ColorRPG] Purchases fetch failed: " + e);
                store.OnProductsFetchFailed += e => { LastError = e != null ? e.ToString() : "fetch failed"; Debug.LogWarning("[ColorRPG] Products fetch failed: " + LastError); };
                await store.Connect();
                var defs = new List<ProductDefinition>();
                foreach (var d in products) defs.Add(new ProductDefinition(d.id, d.consumable ? ProductType.Consumable : ProductType.NonConsumable));
                store.FetchProducts(defs);
            }
            catch (Exception e) { LastError = e.Message; Debug.LogWarning("[ColorRPG] Store unavailable: " + e.Message); }
            busy = false;
        }

        static string FirstId(ICart cart)
        {
            if (cart == null) return "";
            foreach (var it in cart.Items()) if (it != null && it.Product != null && it.Product.definition != null) return it.Product.definition.id;
            return "";
        }

        static void OnPending(PendingOrder order)
        {
            bool ok = true;
            foreach (var it in order.CartOrdered.Items())
            {
                if (it == null || it.Product == null || it.Product.definition == null) continue;
                string id = it.Product.definition.id;
                bool given = OnDeliver != null && OnDeliver(id);
                Debug.Log("[ColorRPG] Purchase delivered: " + id + " -> " + given);
                ok &= given;
            }
            if (ok) store.ConfirmPurchase(order);   // doar dupa ce jocul a dat obiectul
        }

        /// <summary>Porneste plata Google pentru produsul id.</summary>
        public static bool Buy(string id)
        {
            if (!Ready || store == null) return false;
            store.PurchaseProduct(id);
            return true;
        }
#else
        public static void Init(List<Def> products) { LastError = "Unity IAP not installed"; }
        public static bool Buy(string id) { return false; }
#endif
    }
}
