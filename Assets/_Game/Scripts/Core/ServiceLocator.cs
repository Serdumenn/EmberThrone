// TECH-001 §3.3 — Service Locator Pattern
// FindObjectOfType YASAK — tüm servisler bu locator üzerinden erişilir.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Core
{
    /// <summary>
    /// Central service registry. All managers register here at boot time.
    /// Usage: ServiceLocator.Register&lt;IMyService&gt;(instance);
    ///        var svc = ServiceLocator.Get&lt;IMyService&gt;();
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        /// <summary>
        /// Register a service instance by its interface/base type.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);

            if (service == null)
            {
                Debug.LogError($"[ServiceLocator] Cannot register null for {type.Name}.");
                return;
            }

            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Overwriting existing service: {type.Name}");
            }

            _services[type] = service;
            Debug.Log($"[ServiceLocator] Registered: {type.Name}");
        }

        /// <summary>
        /// Retrieve a registered service. Returns null and logs error if not found.
        /// </summary>
        public static T Get<T>() where T : class
        {
            var type = typeof(T);

            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }

            Debug.LogError($"[ServiceLocator] Service not found: {type.Name}. Was it registered in GameBootstrapper?");
            return null;
        }

        /// <summary>
        /// Try to retrieve a service without logging errors.
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);

            if (_services.TryGetValue(type, out var obj))
            {
                service = obj as T;
                return service != null;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Unregister a service (for cleanup / scene transitions).
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            var type = typeof(T);

            if (_services.Remove(type))
            {
                Debug.Log($"[ServiceLocator] Unregistered: {type.Name}");
            }
        }

        /// <summary>
        /// Clear all registered services. Called on application quit or full reset.
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
            Debug.Log("[ServiceLocator] All services cleared.");
        }

        /// <summary>
        /// Check if a service is registered.
        /// </summary>
        public static bool Has<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
    }
}
