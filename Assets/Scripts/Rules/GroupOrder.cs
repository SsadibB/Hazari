using System.Collections.Generic;
using Hazari.Cards;

namespace Hazari.Rules
{
    /// <summary>
    /// Orders the three 3-card groups from strongest to weakest and always leaves the 4-card group last.
    /// </summary>
    public static class GroupOrder
    {
        public static void OrderStrongestFirst(CardData[] hand, out int[] sizes)
        {
            sizes = new int[] { 3, 3, 3, 4 };
            if (hand == null || hand.Length != HazariRules.CardsPerPlayer)
                return;

            var threes = new List<Slice>();
            Slice four = default;
            var hasFour = false;
            var cursor = 0;
            for (var group = 0; group < HazariRules.GroupSizes.Length; group++)
            {
                var size = HazariRules.GroupSizes[group];
                var cards = new CardData[size];
                for (var i = 0; i < size; i++)
                    cards[i] = hand[cursor + i];
                SortHighToLow(cards);

                var slice = new Slice(cursor, cards, HandEvaluator.Evaluate(cards));
                if (size == 4)
                {
                    four = slice;
                    hasFour = true;
                }
                else
                {
                    threes.Add(slice);
                }

                cursor += size;
            }

            threes.Sort(CompareStrongest);

            cursor = 0;
            for (var group = 0; group < threes.Count; group++)
            {
                var cards = threes[group].Cards;
                for (var i = 0; i < cards.Length; i++)
                    hand[cursor + i] = cards[i];
                cursor += cards.Length;
            }

            if (!hasFour)
                return;

            var last = four.Cards;
            for (var i = 0; i < last.Length; i++)
                hand[cursor + i] = last[i];
        }

        static void SortHighToLow(CardData[] cards)
        {
            for (var i = 0; i < cards.Length; i++)
            {
                for (var j = i + 1; j < cards.Length; j++)
                {
                    if (CompareCard(cards[i], cards[j]) <= 0)
                        continue;

                    var swap = cards[i];
                    cards[i] = cards[j];
                    cards[j] = swap;
                }
            }
        }

        static int CompareCard(CardData left, CardData right)
        {
            var rank = right.Rank.CompareTo(left.Rank);
            if (rank != 0)
                return rank;
            return left.Suit.CompareTo(right.Suit);
        }

        static int CompareStrongest(Slice left, Slice right)
        {
            var strength = HazariRules.Compare(right.Result, left.Result);
            if (strength != 0)
                return strength;
            return left.Order.CompareTo(right.Order);
        }

        struct Slice
        {
            public Slice(int order, CardData[] cards, HandResult result)
            {
                Order = order;
                Cards = cards;
                Result = result;
            }

            public int Order { get; }
            public CardData[] Cards { get; }
            public HandResult Result { get; }
        }
    }
}
