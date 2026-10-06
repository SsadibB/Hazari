using System.Collections.Generic;
using Hazari.Cards;

namespace Hazari.Scoring
{
    /// <summary>
    /// Match points come from the cards taken, not from a fixed round bonus.
    /// </summary>
    public static class ScoreCalculator
    {
        public static int PointsIn(IReadOnlyList<CardData> cards)
        {
            return CardValueCalculator.Sum(cards);
        }
    }
}
