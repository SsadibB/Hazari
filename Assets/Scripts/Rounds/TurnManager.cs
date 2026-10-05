using UnityEngine;

namespace Hazari.Rounds
{
    /// <summary>
    /// Whose action is legal. Online matches read the turn from Fusion and only mirror it here.
    /// </summary>
    public sealed class TurnManager : MonoBehaviour
    {
        public int CurrentPlayerId { get; private set; } = -1;

        public bool IsPlayerTurn(int playerId)
        {
            return playerId == CurrentPlayerId;
        }

        public void SetCurrentPlayer(int playerId)
        {
            CurrentPlayerId = playerId;
        }
    }
}
