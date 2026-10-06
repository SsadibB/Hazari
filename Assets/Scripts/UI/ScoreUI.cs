using Hazari.Rules;
using UnityEngine;

namespace Hazari.UI
{
    public sealed class ScoreUI : MonoBehaviour
    {
        readonly int[] _scores = new int[HazariRules.PlayerCount];
        readonly int[] _roundScores = new int[HazariRules.PlayerCount];

        public void SetScore(int playerId, int score)
        {
            SetTotalScore(playerId, score);
        }

        public void SetTotalScore(int playerId, int totalScore)
        {
            if (playerId < 0 || playerId >= _scores.Length)
                return;

            _scores[playerId] = totalScore;
        }

        public void SetRoundScore(int playerId, int roundScore)
        {
            if (playerId < 0 || playerId >= _roundScores.Length)
                return;

            _roundScores[playerId] = roundScore;
        }

        public int GetScore(int playerId)
        {
            return GetTotalScore(playerId);
        }

        public int GetTotalScore(int playerId)
        {
            if (playerId < 0 || playerId >= _scores.Length)
                return 0;

            return _scores[playerId];
        }

        public int GetRoundScore(int playerId)
        {
            if (playerId < 0 || playerId >= _roundScores.Length)
                return 0;

            return _roundScores[playerId];
        }
    }
}
