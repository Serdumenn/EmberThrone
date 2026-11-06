using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);

            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = new List<Delegate>();
            }

            if (_subscribers[type].Contains(handler))
            {
                Debug.LogWarning($"[EventBus] Duplicate subscription for {type.Name} — ignored.");
                return;
            }

            _subscribers[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);

            if (_subscribers.TryGetValue(type, out var handlers))
            {
                handlers.Remove(handler);

                if (handlers.Count == 0)
                {
                    _subscribers.Remove(type);
                }
            }
        }

        public static void Publish<T>(T eventData) where T : struct
        {
            var type = typeof(T);

            if (!_subscribers.TryGetValue(type, out var handlers))
            {
                return;
            }

            var snapshot = new List<Delegate>(handlers);

            foreach (var handler in snapshot)
            {
                try
                {
                    ((Action<T>)handler).Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] Error in handler for {type.Name}: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        public static void Clear()
        {
            _subscribers.Clear();
            Debug.Log("[EventBus] All subscriptions cleared.");
        }

        public static int GetSubscriberCount<T>() where T : struct
        {
            var type = typeof(T);
            return _subscribers.TryGetValue(type, out var handlers) ? handlers.Count : 0;
        }
    }
}
