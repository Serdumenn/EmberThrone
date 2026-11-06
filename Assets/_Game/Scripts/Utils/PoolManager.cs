using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Utils
{
    public class PoolManager : MonoBehaviour, IPoolManager
    {
        private readonly Dictionary<int, object> _pools = new();

        private readonly Dictionary<int, Transform> _poolParents = new();

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
            Debug.Log($"[PoolManager] Pre-warmed pool: {prefab.name} × {count}");
        }

        public void ClearAll()
        {
            foreach (var kvp in _pools)
            {
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

        private ObjectPool<T> GetOrCreatePool<T>(T prefab, int preWarmCount = 0) where T : Component
        {
            int id = prefab.GetInstanceID();

            if (_pools.TryGetValue(id, out var existingPool))
            {
                return existingPool as ObjectPool<T>;
            }

            var parentGo = new GameObject($"[Pool] {prefab.name}");
            parentGo.transform.SetParent(transform);
            _poolParents[id] = parentGo.transform;

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
