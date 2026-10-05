using UnityEngine;

namespace Hazari.Backend
{
    /// <summary>
    /// Writes career stats after a finalized match. A second call for the same match is ignored.
    /// </summary>
    public sealed class PlayFabStatisticsManager : MonoBehaviour
    {
        string _submittedMatchId = string.Empty;

        public bool HasSubmitted(string matchId)
        {
            return !string.IsNullOrEmpty(matchId) && _submittedMatchId == matchId;
        }

        public void MarkSubmitted(string matchId)
        {
            _submittedMatchId = matchId ?? string.Empty;
        }

        public void ResetSubmission()
        {
            _submittedMatchId = string.Empty;
        }
    }
}
