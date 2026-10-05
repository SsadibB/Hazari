using Hazari.Utilities;

namespace Hazari.Backend
{
    public sealed class CachedPlayerProfile
    {
        public string PlayFabId = string.Empty;
        public string DisplayName = string.Empty;
        public string AvatarId = string.Empty;
        public int GamesPlayed;
        public int GamesWon;
        public int GamesLost;
        public float WinRate;
        public int TotalScore;
        public int CurrentStreak;
        public int BestStreak;
    }

    /// <summary>
    /// Cached display name, avatar, and career stats. Callers read the cache instead of requesting PlayFab every frame.
    /// </summary>
    public sealed class PlayFabProfileManager : PersistentSingleton<PlayFabProfileManager>
    {
        public CachedPlayerProfile Cache { get; private set; }
        public bool HasCache => Cache != null;

        public void SetCache(CachedPlayerProfile profile)
        {
            Cache = profile;
        }

        public void ClearCache()
        {
            Cache = null;
        }
    }
}
