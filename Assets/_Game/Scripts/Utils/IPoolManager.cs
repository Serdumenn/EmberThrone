using UnityEngine;

namespace EmberThrone.Utils
{
    public interface IPoolManager
    {
        T Get<T>(T prefab) where T : Component;

        T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component;

        void Release<T>(T obj) where T : Component;

        void PreWarm<T>(T prefab, int count) where T : Component;

        void ClearAll();
    }
}
