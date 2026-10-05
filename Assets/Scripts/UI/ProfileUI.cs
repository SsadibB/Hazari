using Hazari.Backend;
using UnityEngine;

namespace Hazari.UI
{
    /// <summary>
    /// Reads the cached PlayFab profile. It does not request profile data every frame.
    /// </summary>
    public sealed class ProfileUI : MonoBehaviour
    {
        public void RefreshFromCache()
        {
            var profile = PlayFabProfileManager.Instance;
            if (profile == null || !profile.HasCache)
                return;
        }
    }
}
