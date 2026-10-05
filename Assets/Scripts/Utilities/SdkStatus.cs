using System;
using System.Text;

namespace Hazari.Utilities
{
    /// <summary>
    /// Reports whether the live-match, profile, and animation libraries are loaded.
    /// Compile references in the Networking and PlayFab scripts are the hard check for Fusion and PlayFab.
    /// </summary>
    public static class SdkStatus
    {
        public static bool IsFusionInstalled => IsTypeLoaded("Fusion.NetworkRunner");
        public static bool IsPlayFabInstalled => IsTypeLoaded("PlayFab.PlayFabSettings");
        public static bool IsDoTweenInstalled => IsTypeLoaded("DG.Tweening.DOTween");

        public static string Report()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Hazari SDK status");
            builder.AppendLine("Photon Fusion: " + Label(IsFusionInstalled));
            builder.AppendLine("PlayFab: " + Label(IsPlayFabInstalled));
            builder.AppendLine("DOTween: " + Label(IsDoTweenInstalled));
            return builder.ToString();
        }

        static string Label(bool installed)
        {
            return installed ? "installed" : "MISSING";
        }

        static bool IsTypeLoaded(string fullName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                if (assemblies[i].GetType(fullName) != null)
                    return true;
            }

            return false;
        }
    }
}
