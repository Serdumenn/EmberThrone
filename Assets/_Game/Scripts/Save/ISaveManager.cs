// TECH-001 §7 — ISaveManager Interface
// ServiceLocator'a register edilir. Concrete: SaveManager.

namespace EmberThrone.Save
{
    /// <summary>
    /// Save/Load contract. Register via ServiceLocator.
    /// Usage: ServiceLocator.Get<ISaveManager>().Save();
    /// </summary>
    public interface ISaveManager
    {
        /// <summary>Save current game state to disk.</summary>
        void Save();

        /// <summary>Load game state from disk. Returns null if no save exists.</summary>
        SaveData Load();

        /// <summary>Check if a save file exists.</summary>
        bool HasSave();

        /// <summary>Delete save file (for debug / reset).</summary>
        void DeleteSave();

        /// <summary>Get current in-memory save data (cached after Load).</summary>
        SaveData CurrentData { get; }
    }
}
