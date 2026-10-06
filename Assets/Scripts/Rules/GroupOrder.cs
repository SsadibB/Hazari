using System.Collections.Generic;
using Hazari.Cards;

namespace Hazari.Rules
{
    /// <summary>
    /// Keeps the 3 + 3 + 3 + 4 split and orders those groups from strongest to weakest.
    /// </summary>
    public static class GroupOrder
    {
        public static void OrderStrongestFirst(CardData[] hand, out int[] sizes)
        {
            sizes = new int[HazariRules.GroupSizes.Length];
            if (hand == null || hand.Length != HazariRules.CardsPerPlayer)
                return;

            var groups = new List<Slice>();
            var cursor = 0;
            for (var group = 0; group < HazariRules.GroupSizes.Length; group++)
            {
                var size = HazariRules.GroupSizes[group];
                var cards = new CardData[size];
                for (var i = 0; i < size; i++)
                    cards[i] = hand[cursor + i];

                groups.Add(new Slice(cursor, cards, HandEvaluator.Evaluate(cards)));
                cursor += size;
            }

            groups.Sort(CompareStrongest);

            cursor = 0;
            for (var group = 0; group < groups.Count; group++)
            {
                var cards = groups[group].Cards;
                sizes[group] = cards.Length;
                for (var i = 0; i < cards.Length; i++)
                    hand[cursor + i] = cards[i];
                cursor += cards.Length;
            }
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
