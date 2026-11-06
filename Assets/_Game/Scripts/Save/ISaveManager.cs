namespace EmberThrone.Save
{
    public interface ISaveManager
    {
        void Save();

        SaveData Load();

        bool HasSave();

        void DeleteSave();

        SaveData CurrentData { get; }
    }
}
