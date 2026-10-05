namespace Hazari.Core
{
    /// <summary>
    /// Match flow. Visuals follow this value; they do not define it.
    /// </summary>
    public enum GameState
    {
        Waiting = 0,
        Dealing = 1,
        Arranging = 2,
        Ready = 3,
        Playing = 4,
        RoundResult = 5,
        NextRound = 6,
        MatchResult = 7
    }
}
