using Hazari.Rules;
using UnityEngine;

namespace Hazari.Players
{
    /// <summary>
    /// Four seats. Single-player fills three with AI. Online fills them from Fusion players.
    /// </summary>
    public sealed class PlayerManager : MonoBehaviour
    {
        readonly PlayerData[] _players = new PlayerData[HazariRules.PlayerCount];

        public int PlayerCount => _players.Length;

        void Awake()
        {
            for (var i = 0; i < _players.Length; i++)
                _players[i] = new PlayerData { PlayerId = i };
        }

        public PlayerData GetPlayer(int playerId)
        {
            if (playerId < 0 || playerId >= _players.Length)
                return null;

            return _players[playerId];
        }
    }
}
