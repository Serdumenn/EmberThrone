using System;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.Purchasing;

namespace EmberThrone.Game
{
    public sealed class PurchaseService : MonoBehaviour
    {
        public static bool SimulateInEditor = true;

        public event Action ProductsChanged;
        public event Action<string> PurchaseSucceeded;
        public event Action<string, string> PurchaseFailed;

        StoreController _store;
        List<ProductDefinition> _definitions;
        Func<string, string, bool> _grant;
        string _pending;
        int _simulated;

        public bool IsReady { get; private set; }
        public bool IsPurchasing => !string.IsNullOrEmpty(_pending);
        public int Completed { get; private set; }

        static bool Simulated => Application.isEditor && SimulateInEditor;

        public void Initialize(IEnumerable<StoreProduct> products, Func<string, string, bool> grantAndSave)
        {
            if (_grant != null) return;
            _grant = grantAndSave;
            _definitions = new List<ProductDefinition>();
            foreach (var p in products) _definitions.Add(new ProductDefinition(p.Id, ProductType.Consumable));
            if (Simulated)
            {
                IsReady = true;
                return;
            }
            Connect();
        }

        async void Connect()
        {
            _store = UnityIAPServices.StoreController();
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _store.OnPurchaseFailed += OnPurchaseFailed;
            _store.OnPurchaseDeferred += OnPurchaseDeferred;
            _store.OnStoreConnected += OnStoreConnected;
            _store.OnStoreDisconnected += OnStoreDisconnected;
            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += OnProductsFetchFailed;
            _store.OnPurchasesFetched += OnPurchasesFetched;
            _store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            try
            {
                await _store.Connect();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[IAP] Connect failed: " + e.Message);
            }
        }

        public string PriceOf(StoreProduct product)
        {
            if (Simulated || !IsReady || _store == null) return product.Price;
            var p = _store.GetProductById(product.Id);
            return p != null && p.availableToPurchase ? p.metadata.localizedPriceString : product.Price;
        }

        public bool Buy(string productId)
        {
            if (!IsReady || IsPurchasing) return false;
            if (Simulated)
            {
                _simulated++;
                bool saved = _grant != null && _grant(productId, "editor-" + _simulated + "-" + productId);
                if (!saved)
                {
                    PurchaseFailed?.Invoke(productId, "NotSaved");
                    return false;
                }
                Completed++;
                PurchaseSucceeded?.Invoke(productId);
                return true;
            }
            var product = _store.GetProductById(productId);
            if (product == null || !product.availableToPurchase) return false;
            _pending = productId;
            _store.PurchaseProduct(product);
            return true;
        }

        void OnStoreConnected() => _store.FetchProducts(_definitions);

        void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            IsReady = false;
            Debug.LogWarning("[IAP] Store disconnected: " + failure.Message);
            ProductsChanged?.Invoke();
        }

        void OnProductsFetched(List<Product> products)
        {
            IsReady = true;
            _store.FetchPurchases();
            ProductsChanged?.Invoke();
        }

        void OnProductsFetchFailed(ProductFetchFailed failure) => Debug.LogWarning("[IAP] Product fetch failed: " + failure.FailureReason);

        void OnPurchasesFetched(Orders orders)
        {
            foreach (var order in orders.ConfirmedOrders)
                foreach (var item in order.CartOrdered.Items())
                    _grant?.Invoke(item.Product.definition.id, order.Info.TransactionID);
            ProductsChanged?.Invoke();
        }

        void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure) => Debug.LogWarning("[IAP] Purchases fetch failed: " + failure.Message);

        void OnPurchasePending(PendingOrder order)
        {
            bool saved = true;
            string productId = null;
            foreach (var item in order.CartOrdered.Items())
            {
                productId = item.Product.definition.id;
                if (_grant == null || !_grant(productId, order.Info.TransactionID)) saved = false;
            }
            if (!saved)
            {
                Debug.LogError("[IAP] Grant for " + productId + " was not saved; the order stays pending.");
                return;
            }
            _store.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(Order order)
        {
            string productId = FirstProductId(order);
            switch (order)
            {
                case ConfirmedOrder _:
                    ClearPending(productId);
                    Completed++;
                    PurchaseSucceeded?.Invoke(productId);
                    break;
                case FailedOrder failed:
                    ClearPending(productId);
                    PurchaseFailed?.Invoke(productId, failed.FailureReason.ToString());
                    break;
            }
        }

        void OnPurchaseFailed(FailedOrder order)
        {
            string productId = FirstProductId(order);
            ClearPending(productId);
            PurchaseFailed?.Invoke(productId, order.FailureReason.ToString());
        }

        void OnPurchaseDeferred(DeferredOrder order)
        {
            string productId = FirstProductId(order);
            ClearPending(productId);
            PurchaseFailed?.Invoke(productId, "Deferred");
        }

        void ClearPending(string productId)
        {
            if (productId == null || productId == _pending) _pending = null;
        }

        static string FirstProductId(Order order)
        {
            foreach (var item in order.CartOrdered.Items()) return item.Product.definition.id;
            return null;
        }
    }
}
