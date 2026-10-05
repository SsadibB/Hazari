using Hazari.Players;
using UnityEngine;

namespace Hazari.AI
{
    /// <summary>
    /// Single-player opponent. Uses the same rules as a human and acts only on its own turn.
    /// Decisions are not implemented in this milestone.
    /// </summary>
    public sealed class AIPlayerController : MonoBehaviour
    {
        [SerializeField] int playerId;

        public int PlayerId => playerId;

        public void Bind(PlayerData player)
        {
            if (player == null)
                return;

            playerId = player.PlayerId;
            player.IsAi = true;
        }
    }
}
