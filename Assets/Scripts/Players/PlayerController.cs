using UnityEngine;

namespace Hazari.Players
{
    /// <summary>
    /// Local input for the human seat. Requests actions. It does not declare the winner, score, or turn.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] int localPlayerId;

        public int LocalPlayerId => localPlayerId;
    }
}
