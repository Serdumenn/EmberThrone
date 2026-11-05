// TECH-001 §9 — Generic Object Pool
// Tek prefab/component tipi için pool. PoolManager tarafından yönetilir.

using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Utils
{
    /// <summary>
    /// Generic object pool for a single prefab type.
    /// Pre-warms on creation, auto-expands when empty.
    /// Usage: Managed by PoolManager — do not use directly.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _available = new();
        private readonly HashSet<T> _inUse = new();

        /// <summary>Number of objects currently available in the pool.</summary>
        public int AvailableCount => _available.Count;

        /// <summary>Number of objects currently in use.</summary>
        public int InUseCount => _inUse.Count;

        /// <summary>Total objects managed by this pool.</summary>
        public int TotalCount => _available.Count + _inUse.Count;

        /// <summary>
        /// Create a new pool for the given prefab.
        /// </summary>
        /// <param name="prefab">Prefab to instantiate.</param>
        /// <param name="parent">Parent transform for pooled objects.</param>
        /// <param name="preWarmCount">Number of instances to create upfront.</param>
        public ObjectPool(T prefab, Transform parent, int preWarmCount = 0)
        {
            _prefab = prefab;
            _parent = parent;

            // Pre-warm
            for (int i = 0; i < preWarmCount; i++)
            {
                var obj = CreateNewInstance();
                obj.gameObject.SetActive(false);
                _available.Enqueue(obj);
            }

            if (preWarmCount > 0)
            {
                Debug.Log($"[ObjectPool] Pre-warmed {preWarmCount}x {typeof(T).Name} ({_prefab.name})");
            }
        }

        /// <summary>
        /// Get an object from the pool. Creates a new one if pool is empty.
        /// </summary>
        public T Get()
        {
            T obj;

            if (_available.Count > 0)
            {
                obj = _available.Dequeue();
            }
            else
            {
                // Auto-expand
                obj = CreateNewInstance();
            }

            obj.gameObject.SetActive(true);
            _inUse.Add(obj);
            return obj;
        }

        /// <summary>
        /// Get an object and set its position/rotation.
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            var obj = Get();
            obj.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Release(T obj)
        {
            if (obj == null) return;

            if (!_inUse.Remove(obj))
            {
                Debug.LogWarning($"[ObjectPool] Trying to release an object not managed by this pool: {obj.name}");
                return;
            }

            obj.gameObject.SetActive(false);
            obj.transform.SetParent(_parent);
            _available.Enqueue(obj);
        }

        /// <summary>
        /// Release all in-use objects back to the pool.
        /// </summary>
        public void ReleaseAll()
        {
            // Copy to avoid modification during iteration
            var inUseCopy = new List<T>(_inUse);
            foreach (var obj in inUseCopy)
            {
                Release(obj);
            }
        }

        /// <summary>
        /// Destroy all pooled objects and clear the pool.
        /// </summary>
        public void Clear()
        {
            foreach (var obj in _available)
            {
                if (obj != null) Object.Destroy(obj.gameObject);
            }

            foreach (var obj in _inUse)
            {
                if (obj != null) Object.Destroy(obj.gameObject);
            }

            _available.Clear();
            _inUse.Clear();
        }

        private T CreateNewInstance()
        {
            var obj = Object.Instantiate(_prefab, _parent);
            obj.gameObject.name = $"{_prefab.name}_pooled";
            return obj;
        }
    }
}
