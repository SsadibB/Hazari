using System.Collections.Generic;

namespace Hazari.Cards
{
    /// <summary>
    /// Point value of a card. Twos through nines are 5. Tens through aces are 10. A full deck is 360.
    /// </summary>
    public static class CardValueCalculator
    {
        public const int LowValue = 5;
        public const int HighValue = 10;
        public const int DeckTotal = 360;

        public static int Points(Rank rank)
        {
            return (int)rank >= (int)Rank.Ten ? HighValue : LowValue;
        }

        public static int Points(CardData card)
        {
            return Points(card.Rank);
        }

        public static int Sum(IReadOnlyList<CardData> cards)
        {
            if (cards == null)
                return 0;

            var total = 0;
            for (var i = 0; i < cards.Count; i++)
                total += Points(cards[i]);
            return total;
        }

        public static string SelfCheck()
        {
            var total = 0;
            var lows = 0;
            var highs = 0;
            foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
                {
                    var points = Points(rank);
                    total += points;
                    if (points == LowValue)
                        lows++;
                    else
                        highs++;
                }
            }

            if (lows == 32 && highs == 20 && total == DeckTotal)
                return "Card values total 360.";

            return "Card values failed. Lows " + lows + ", highs " + highs + ", total " + total + ".";
        }
    }
}
