using System.Collections.Generic;
using UnityEngine;

namespace EmberThrone.Utils
{
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _available = new();
        private readonly HashSet<T> _inUse = new();

        public int AvailableCount => _available.Count;

        public int InUseCount => _inUse.Count;

        public int TotalCount => _available.Count + _inUse.Count;

        public ObjectPool(T prefab, Transform parent, int preWarmCount = 0)
        {
            _prefab = prefab;
            _parent = parent;

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

        public T Get()
        {
            T obj;

            if (_available.Count > 0)
            {
                obj = _available.Dequeue();
            }
            else
            {
                obj = CreateNewInstance();
            }

            obj.gameObject.SetActive(true);
            _inUse.Add(obj);
            return obj;
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            var obj = Get();
            obj.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }

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

        public void ReleaseAll()
        {
            var inUseCopy = new List<T>(_inUse);
            foreach (var obj in inUseCopy)
            {
                Release(obj);
            }
        }

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
