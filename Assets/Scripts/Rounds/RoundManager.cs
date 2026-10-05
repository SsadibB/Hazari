using UnityEngine;

namespace Hazari.Rounds
{
    /// <summary>
    /// Round flow. Scoring and the winner are applied from authoritative state, not from UI.
    /// </summary>
    public sealed class RoundManager : MonoBehaviour
    {
        public int CurrentRound { get; private set; }

        public void ResetMatch()
        {
            CurrentRound = 0;
        }
    }
}
