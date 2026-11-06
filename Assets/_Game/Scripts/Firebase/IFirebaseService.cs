namespace EmberThrone.Firebase
{
    public interface IFirebaseService
    {
        bool IsInitialized { get; }

        void Initialize();

        bool IsAuthenticated { get; }

        string UserId { get; }
    }
}
