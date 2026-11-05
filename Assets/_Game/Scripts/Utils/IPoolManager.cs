// TECH-001 §9 — IPoolManager Interface
// ServiceLocator'a register edilir. Prefab bazlı pool yönetimi.

using UnityEngine;

namespace EmberThrone.Utils
{
    /// <summary>
    /// Central pool management contract.
    /// Usage: ServiceLocator.Get<IPoolManager>().Get<T>(prefab);
    /// </summary>
    public interface IPoolManager
    {
        /// <summary>Get an object from the pool for the given prefab.</summary>
        T Get<T>(T prefab) where T : Component;

        /// <summary>Get an object with position and rotation.</summary>
        T Get<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component;

        /// <summary>Return an object to its pool.</summary>
        void Release<T>(T obj) where T : Component;

        /// <summary>Pre-warm a pool for a specific prefab.</summary>
        void PreWarm<T>(T prefab, int count) where T : Component;

        /// <summary>Release all objects and clear all pools.</summary>
        void ClearAll();
    }
}
