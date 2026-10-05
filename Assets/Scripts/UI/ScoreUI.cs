using Hazari.Rules;
using UnityEngine;

namespace Hazari.UI
{
    public sealed class ScoreUI : MonoBehaviour
    {
        readonly int[] _scores = new int[HazariRules.PlayerCount];

        public void SetScore(int playerId, int score)
        {
            if (playerId < 0 || playerId >= _scores.Length)
                return;

            _scores[playerId] = score;
        }

        public int GetScore(int playerId)
        {
            if (playerId < 0 || playerId >= _scores.Length)
                return 0;

            return _scores[playerId];
        }
    }
}
