using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

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

        public static void Unregister<T>() where T : class
        {
            var type = typeof(T);

            if (_services.Remove(type))
            {
                Debug.Log($"[ServiceLocator] Unregistered: {type.Name}");
            }
        }

        public static void Clear()
        {
            _services.Clear();
            Debug.Log("[ServiceLocator] All services cleared.");
        }

        public static bool Has<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }
    }
}
