// TECH-001 §9 — PoolManager (Concrete Implementation)
// Prefab bazlı pool yönetimi. ServiceLocator'a register edilir.
// MonoBehaviour — pooled object'ler için parent Transform sağlar.

using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Utils
{
    /// <summary>
    /// Central pool manager. Creates and manages ObjectPool instances per prefab.
    /// Attach to a persistent GameObject (managed by GameBootstrapper).
    /// </summary>
    public class PoolManager : MonoBehaviour, IPoolManager
    {
        // Key: prefab instance ID → pool
        private readonly Dictionary<int, object> _pools = new();

        // Parent for each pool's objects
        private readonly Dictionary<int, Transform> _poolParents = new();

        // ─── IPoolManager ────────────────────────────

        public T Get<T>(T prefab) where T : Component
        {
            var pool = GetOrCreatePool(prefab);
            return pool.Get();
        }

        public T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            var pool = GetOrCreatePool(prefab);
            return pool.Get(position, rotation);
        }

        public void Release<T>(T obj) where T : Component
        {
            // Find which pool this object belongs to by checking all pools
            // In practice, we store a reverse lookup or tag on the object
            // For now, iterate pools (OK for small pool counts)
            foreach (var kvp in _pools)
            {
                if (kvp.Value is ObjectPool<T> pool)
                {
                    pool.Release(obj);
                    return;
                }
            }

            Debug.LogWarning($"[PoolManager] No pool found for {obj.name}. Destroying instead.");
            Destroy(obj.gameObject);
        }

        public void PreWarm<T>(T prefab, int count) where T : Component
        {
            var pool = GetOrCreatePool(prefab, preWarmCount: count);
            // Pool already pre-warmed during creation
            Debug.Log($"[PoolManager] Pre-warmed pool: {prefab.name} × {count}");
        }

        public void ClearAll()
        {
            foreach (var kvp in _pools)
            {
                // Use reflection-free approach: destroy parent transforms
                // which will destroy all children
            }

            foreach (var kvp in _poolParents)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }

            _pools.Clear();
            _poolParents.Clear();

            Debug.Log("[PoolManager] All pools cleared.");
        }

        // ─── Internal ────────────────────────────────

        private ObjectPool<T> GetOrCreatePool<T>(T prefab, int preWarmCount = 0) where T : Component
        {
            int id = prefab.GetInstanceID();

            if (_pools.TryGetValue(id, out var existingPool))
            {
                return existingPool as ObjectPool<T>;
            }

            // Create parent container
            var parentGo = new GameObject($"[Pool] {prefab.name}");
            parentGo.transform.SetParent(transform);
            _poolParents[id] = parentGo.transform;

            // Create pool
            var pool = new ObjectPool<T>(prefab, parentGo.transform, preWarmCount);
            _pools[id] = pool;

            Debug.Log($"[PoolManager] Created pool for: {prefab.name}");
            return pool;
        }

        private void OnDestroy()
        {
            ClearAll();
        }
    }
}
