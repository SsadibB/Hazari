using Hazari.Utilities;
using UnityEditor;
using UnityEngine;

namespace Hazari.EditorTools
{
    public static class HazariSdkStatusMenu
    {
        [MenuItem("Hazari/Verify Installed SDKs")]
        public static void Verify()
        {
            Debug.Log(SdkStatus.Report());
        }
    }
}
