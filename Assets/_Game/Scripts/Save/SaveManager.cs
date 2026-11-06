using System;
using System.IO;
using UnityEngine;
using EmberThrone.Core;

namespace EmberThrone.Save
{
    public class SaveManager : ISaveManager
    {
        private const string SAVE_FILE = "save.json";
        private const string BACKUP_FILE = "save_backup.json";

        private SaveData _currentData;
        private readonly string _savePath;
        private readonly string _backupPath;

        public SaveData CurrentData => _currentData;

        public SaveManager()
        {
            _savePath = Path.Combine(Application.persistentDataPath, SAVE_FILE);
            _backupPath = Path.Combine(Application.persistentDataPath, BACKUP_FILE);

            _currentData = HasSave() ? Load() : SaveData.CreateNew();

            EventBus.Subscribe<OnLevelCompleted>(OnLevelCompleted);

            Debug.Log($"[SaveManager] Initialized. Save path: {_savePath}");
        }

        public void Save()
        {
            try
            {
                _currentData.lastSaveTicks = DateTime.UtcNow.Ticks;

                string json = JsonUtility.ToJson(_currentData, prettyPrint: false);

                if (File.Exists(_savePath))
                {
                    File.Copy(_savePath, _backupPath, overwrite: true);
                }

                File.WriteAllText(_savePath, json);

                Debug.Log("[SaveManager] Game saved successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Save failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public SaveData Load()
        {
            try
            {
                if (!File.Exists(_savePath))
                {
                    Debug.Log("[SaveManager] No save file found.");
                    return null;
                }

                string json = File.ReadAllText(_savePath);
                var data = JsonUtility.FromJson<SaveData>(json);

                if (data == null)
                {
                    throw new Exception("Deserialization returned null");
                }

                data = MigrateIfNeeded(data);

                _currentData = data;
                Debug.Log($"[SaveManager] Save loaded. Level: {data.currentLevelId}, Coins: {data.coins}");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Load failed: {ex.Message}. Trying backup...");
                return TryLoadBackup();
            }
        }

        public bool HasSave()
        {
            return File.Exists(_savePath);
        }

        public void DeleteSave()
        {
            try
            {
                if (File.Exists(_savePath))
                    File.Delete(_savePath);

                if (File.Exists(_backupPath))
                    File.Delete(_backupPath);

                _currentData = SaveData.CreateNew();
                Debug.Log("[SaveManager] Save deleted. Fresh state created.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Delete failed: {ex.Message}");
            }
        }

        private void OnLevelCompleted(OnLevelCompleted evt)
        {
            if (_currentData == null) return;

            int levelIndex = evt.LevelId - 1;
            if (levelIndex >= 0 && levelIndex < _currentData.completedLevels.Length)
            {
                _currentData.completedLevels[levelIndex] = true;
            }

            if (evt.LevelId > _currentData.highestCompletedLevel)
            {
                _currentData.highestCompletedLevel = evt.LevelId;
                _currentData.currentLevelId = evt.LevelId + 1;
            }

            _currentData.totalLevelsCompleted++;

            Save();
        }

        private SaveData TryLoadBackup()
        {
            try
            {
                if (!File.Exists(_backupPath))
                {
                    Debug.LogWarning("[SaveManager] No backup found. Creating fresh save.");
                    return SaveData.CreateNew();
                }

                string json = File.ReadAllText(_backupPath);
                var data = JsonUtility.FromJson<SaveData>(json);

                if (data != null)
                {
                    Debug.Log("[SaveManager] Backup loaded successfully.");
                    _currentData = data;

                    Save();
                    return data;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Backup load also failed: {ex.Message}");
            }

            Debug.LogWarning("[SaveManager] All recovery failed. Creating fresh save.");
            return SaveData.CreateNew();
        }

        private SaveData MigrateIfNeeded(SaveData data)
        {
            return data;
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<OnLevelCompleted>(OnLevelCompleted);
            Save();
            Debug.Log("[SaveManager] Disposed and final save written.");
        }
    }
}
