using System.Collections.Generic;
using Hazari.Cards;

namespace Hazari.Cards
{
    /// <summary>
    /// High rank on the left, low rank on the right. Suit does not change the order.
    /// </summary>
    public static class CombinationSorter
    {
        public static List<string> Sort(IReadOnlyList<string> cardIds)
        {
            var cards = new List<CardData>();
            if (cardIds != null)
            {
                for (var i = 0; i < cardIds.Count; i++)
                {
                    CardData data;
                    if (CardData.TryParse(cardIds[i], out data))
                        cards.Add(data);
                }
            }

            cards.Sort(CompareHighToLow);
            var ordered = new List<string>();
            for (var i = 0; i < cards.Count; i++)
                ordered.Add(cards[i].CardId);
            return ordered;
        }

        static int CompareHighToLow(CardData left, CardData right)
        {
            var rank = right.Rank.CompareTo(left.Rank);
            if (rank != 0)
                return rank;
            return left.Suit.CompareTo(right.Suit);
        }
    }
}
