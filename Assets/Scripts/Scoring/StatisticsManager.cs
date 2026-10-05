using UnityEngine;

namespace Hazari.Scoring
{
    /// <summary>
    /// Local match totals. Persistent career stats are written only by PlayFab after a finalized match.
    /// </summary>
    public sealed class StatisticsManager : MonoBehaviour
    {
        public bool MatchResultSubmitted { get; private set; }

        public void ResetMatch()
        {
            MatchResultSubmitted = false;
        }

        public void MarkMatchResultSubmitted()
        {
            MatchResultSubmitted = true;
        }
    }
}
