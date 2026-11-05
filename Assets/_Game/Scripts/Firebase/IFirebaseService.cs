// TECH-001 §6 — IFirebaseService Interface
// Firebase SDK initialization ve auth state yönetimi.
// SDK yüklenene kadar FirebaseStubService kullanılır.

namespace EmberThrone.Firebase
{
    /// <summary>
    /// Firebase initialization and state contract.
    /// </summary>
    public interface IFirebaseService
    {
        /// <summary>Is Firebase SDK initialized and ready?</summary>
        bool IsInitialized { get; }

        /// <summary>Initialize Firebase asynchronously.</summary>
        void Initialize();

        /// <summary>Is the current user authenticated?</summary>
        bool IsAuthenticated { get; }

        /// <summary>Anonymous user ID (Firebase Auth anonymous sign-in).</summary>
        string UserId { get; }
    }
}
