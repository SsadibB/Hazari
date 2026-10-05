using Hazari.Utilities;
using PlayFab;

namespace Hazari.Backend
{
    /// <summary>
    /// Persistent PlayFab access. Login happens once. Profile and stats are cached. This is not the live match.
    /// </summary>
    public sealed class PlayFabManager : PersistentSingleton<PlayFabManager>
    {
        public bool IsLoggedIn => PlayFabClientAPI.IsClientLoggedIn();
    }
}
