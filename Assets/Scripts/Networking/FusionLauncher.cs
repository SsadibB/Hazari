using Fusion;
using Hazari.Utilities;
using UnityEngine;

namespace Hazari.Networking
{
    /// <summary>
    /// Starts and holds the Fusion runner. Live match rules stay in Fusion state, not in this launcher.
    /// </summary>
    public sealed class FusionLauncher : PersistentSingleton<FusionLauncher>
    {
        NetworkRunner _runner;

        public NetworkRunner Runner => _runner;
        public bool HasSession => _runner != null && _runner.IsRunning;

        public void AttachRunner(NetworkRunner runner)
        {
            _runner = runner;
        }
    }
}
