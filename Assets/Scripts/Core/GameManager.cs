using Hazari.Utilities;

namespace Hazari.Core
{
    /// <summary>
    /// Local match coordinator. Online results, turns, and card ownership come from Fusion, not from this object.
    /// </summary>
    public sealed class GameManager : PersistentSingleton<GameManager>
    {
        public GameState CurrentState { get; private set; } = GameState.Waiting;

        public void SetState(GameState next)
        {
            CurrentState = next;
        }
    }
}
