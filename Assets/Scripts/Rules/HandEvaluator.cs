using System.Collections.Generic;
using Hazari.Cards;

namespace Hazari.Rules
{
    public static class HandEvaluator
    {
        public static HandResult Evaluate(IReadOnlyList<CardData> cards)
        {
            if (cards == null || (cards.Count != 3 && cards.Count != 4))
                return HandResult.Invalid;

            var seen = new HashSet<string>();
            var ranks = new int[cards.Count];
            var suits = new Suit[cards.Count];
            for (var i = 0; i < cards.Count; i++)
            {
                if (string.IsNullOrEmpty(cards[i].CardId) || !seen.Add(cards[i].CardId))
                    return HandResult.Invalid;

                ranks[i] = (int)cards[i].Rank;
                suits[i] = cards[i].Suit;
            }

            var counts = new Dictionary<int, int>();
            for (var i = 0; i < ranks.Length; i++)
            {
                int count;
                counts.TryGetValue(ranks[i], out count);
                counts[ranks[i]] = count + 1;
            }

            var bestCount = 0;
            var bestRank = 0;
            foreach (var pair in counts)
            {
                if (pair.Value > bestCount || (pair.Value == bestCount && pair.Key > bestRank))
                {
                    bestCount = pair.Value;
                    bestRank = pair.Key;
                }
            }

            if (bestCount == 4)
                return HandResult.Of(HandCategory.FourOfAKind, bestRank, 0, 0);

            if (bestCount == 3)
            {
                var kicker = 0;
                for (var i = 0; i < ranks.Length; i++)
                {
                    if (ranks[i] != bestRank && ranks[i] > kicker)
                        kicker = ranks[i];
                }

                return HandResult.Of(HandCategory.Trail, bestRank, kicker, 0);
            }

            int straightHigh;
            var straight = TryStraightHigh(ranks, out straightHigh);
            var sameSuit = true;
            for (var i = 1; i < suits.Length; i++)
            {
                if (suits[i] != suits[0])
                {
                    sameSuit = false;
                    break;
                }
            }

            if (straight && sameSuit)
                return HandResult.Of(HandCategory.PureSequence, straightHigh, 0, 0);

            if (straight)
                return HandResult.Of(HandCategory.Sequence, straightHigh, 0, 0);

            if (sameSuit)
                return HighCardResult(HandCategory.Color, ranks);

            if (bestCount == 2)
            {
                var pairRanks = new List<int>();
                foreach (var pair in counts)
                {
                    if (pair.Value == 2)
                        pairRanks.Add(pair.Key);
                }

                pairRanks.Sort();
                if (pairRanks.Count == 2)
                    return HandResult.Of(HandCategory.TwoPair, pairRanks[1], pairRanks[0], 0);

                var kickers = new List<int>();
                for (var i = 0; i < ranks.Length; i++)
                {
                    if (ranks[i] != bestRank)
                        kickers.Add(ranks[i]);
                }

                kickers.Sort();
                var second = kickers.Count > 0 ? kickers[kickers.Count - 1] : 0;
                var third = kickers.Count > 1 ? kickers[kickers.Count - 2] : 0;
                return HandResult.Of(HandCategory.Pair, bestRank, second, third);
            }

            return HighCardResult(HandCategory.HighCard, ranks);
        }

        public static string DisplayName(HandCategory category)
        {
            switch (category)
            {
                case HandCategory.FourOfAKind: return "Four of a Kind";
                case HandCategory.Trail: return "Trail";
                case HandCategory.PureSequence: return "Pure Sequence";
                case HandCategory.Sequence: return "Sequence";
                case HandCategory.Color: return "Color";
                case HandCategory.TwoPair: return "Two Pair";
                case HandCategory.Pair: return "Pair";
                case HandCategory.HighCard: return "High Card";
                default: return "Invalid";
            }
        }

        public static string SelfCheck()
        {
            var failures = new List<string>();
            CheckBeats(failures, "AAA trail", Cards("AS", "AH", "AD"), "pair", Cards("KS", "KH", "2D"));
            CheckBeats(failures, "pure", Cards("AS", "KS", "QS"), "sequence", Cards("AH", "KD", "QC"));
            CheckBeats(failures, "sequence", Cards("2H", "3D", "4C"), "color", Cards("AS", "9S", "2S"));
            CheckBeats(failures, "color", Cards("AS", "9S", "2S"), "pair", Cards("KH", "KD", "2C"));
            CheckBeats(failures, "pair", Cards("KH", "KD", "2C"), "high", Cards("AS", "QD", "9C"));
            CheckBeats(failures, "AKQ", Cards("AH", "KD", "QC"), "A23", Cards("AS", "2D", "3C"));
            CheckBeats(failures, "four", Cards("AS", "AH", "AD", "AC"), "trail", Cards("KS", "KH", "KD", "2C"));

            var tie = HazariRules.Compare(HandEvaluator.Evaluate(Cards("AS", "AH", "AD")), HandEvaluator.Evaluate(Cards("AC", "AD", "AH")));
            if (tie != 0)
                failures.Add("Identical trails did not tie.");

            var wheel = HandEvaluator.Evaluate(Cards("AS", "2D", "3C"));
            if (wheel.Category != HandCategory.Sequence || wheel.PrimaryValue != (int)Rank.Three)
                failures.Add("A23 should be a sequence high of 3.");

            if (failures.Count == 0)
                return "Hand checks passed.";

            return string.Join("\n", failures.ToArray());
        }

        static void CheckBeats(List<string> failures, string name, CardData[] winner, string loserName, CardData[] loser)
        {
            var left = Evaluate(winner);
            var right = Evaluate(loser);
            if (!left.IsValid || !right.IsValid || HazariRules.Compare(left, right) <= 0)
                failures.Add(name + " should beat " + loserName + ".");
        }

        static CardData[] Cards(params string[] ids)
        {
            var cards = new CardData[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                CardData data;
                if (!CardData.TryParse(ids[i], out data))
                    throw new System.InvalidOperationException(ids[i]);
                cards[i] = data;
            }

            return cards;
        }

        static HandResult HighCardResult(HandCategory category, int[] ranks)
        {
            var ordered = (int[])ranks.Clone();
            System.Array.Sort(ordered);
            var highest = ordered[ordered.Length - 1];
            var second = ordered.Length > 1 ? ordered[ordered.Length - 2] : 0;
            var packed = 0;
            for (var i = ordered.Length - 3; i >= 0; i--)
                packed = packed * 16 + ordered[i];

            return HandResult.Of(category, highest, second, packed);
        }

        static bool TryStraightHigh(int[] ranks, out int high)
        {
            high = 0;
            var unique = new List<int>();
            for (var i = 0; i < ranks.Length; i++)
            {
                if (!unique.Contains(ranks[i]))
                    unique.Add(ranks[i]);
            }

            if (unique.Count != ranks.Length)
                return false;

            unique.Sort();
            if (IsSequential(unique))
            {
                high = unique[unique.Count - 1];
                return true;
            }

            var aceLow = new List<int>();
            for (var i = 0; i < unique.Count; i++)
                aceLow.Add(unique[i] == (int)Rank.Ace ? 1 : unique[i]);

            aceLow.Sort();
            if (!IsSequential(aceLow))
                return false;

            high = aceLow[aceLow.Count - 1];
            return true;
        }

        static bool IsSequential(List<int> ordered)
        {
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i] != ordered[i - 1] + 1)
                    return false;
            }

            return true;
        }
    }
}
