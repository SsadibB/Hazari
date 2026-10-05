using System.Collections.Generic;
using Hazari.Cards;

namespace Hazari.Cards
{
    /// <summary>
    /// Helper order that clusters trails, pairs, then same-suit cards. It does not choose the final groups.
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

            var counts = new Dictionary<Rank, int>();
            for (var i = 0; i < cards.Count; i++)
            {
                int count;
                counts.TryGetValue(cards[i].Rank, out count);
                counts[cards[i].Rank] = count + 1;
            }

            var trips = new List<CardData>();
            var pairs = new List<CardData>();
            var singles = new List<CardData>();
            for (var i = 0; i < cards.Count; i++)
            {
                var count = counts[cards[i].Rank];
                if (count >= 3)
                    trips.Add(cards[i]);
                else if (count == 2)
                    pairs.Add(cards[i]);
                else
                    singles.Add(cards[i]);
            }

            trips.Sort(CompareRankThenSuit);
            pairs.Sort(CompareRankThenSuit);
            singles.Sort(CompareSuitThenRank);

            var ordered = new List<string>();
            Append(ordered, trips);
            Append(ordered, pairs);
            Append(ordered, singles);
            return ordered;
        }

        static void Append(List<string> target, List<CardData> source)
        {
            for (var i = 0; i < source.Count; i++)
                target.Add(source[i].CardId);
        }

        static int CompareRankThenSuit(CardData left, CardData right)
        {
            var rank = right.Rank.CompareTo(left.Rank);
            if (rank != 0)
                return rank;
            return left.Suit.CompareTo(right.Suit);
        }

        static int CompareSuitThenRank(CardData left, CardData right)
        {
            var suit = left.Suit.CompareTo(right.Suit);
            if (suit != 0)
                return suit;
            return right.Rank.CompareTo(left.Rank);
        }
    }
}
