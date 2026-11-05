// TECH-001 §3.3 — EventBus (Publish/Subscribe)
// Loosely coupled iletişim — bileşenler birbirini bilmeden haberleşir.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Core
{
    /// <summary>
    /// Type-safe event bus for decoupled communication.
    /// Usage: EventBus.Subscribe&lt;OnLevelCompleted&gt;(HandleLevelCompleted);
    ///        EventBus.Publish(new OnLevelCompleted { ... });
    ///        EventBus.Unsubscribe&lt;OnLevelCompleted&gt;(HandleLevelCompleted);
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        /// <summary>
        /// Subscribe to an event type.
        /// </summary>
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

        /// <summary>
        /// Unsubscribe from an event type. Always call in OnDestroy/OnDisable.
        /// </summary>
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

        /// <summary>
        /// Publish an event to all subscribers of that type.
        /// </summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            var type = typeof(T);

            if (!_subscribers.TryGetValue(type, out var handlers))
            {
                return; // No subscribers — silent, not an error
            }

            // Iterate on a copy to allow subscribe/unsubscribe during publish
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

        /// <summary>
        /// Clear all subscriptions. Called on scene transitions or reset.
        /// </summary>
        public static void Clear()
        {
            _subscribers.Clear();
            Debug.Log("[EventBus] All subscriptions cleared.");
        }

        /// <summary>
        /// Get subscriber count for debugging.
        /// </summary>
        public static int GetSubscriberCount<T>() where T : struct
        {
            var type = typeof(T);
            return _subscribers.TryGetValue(type, out var handlers) ? handlers.Count : 0;
        }
    }
}
