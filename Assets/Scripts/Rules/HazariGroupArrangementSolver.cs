using System;
using System.Collections.Generic;
using Hazari.Cards;

namespace Hazari.Rules
{
    /// <summary>
    /// Expert solver for Hazari 13-card hands.
    /// Evaluates combinations and produces the best legal 3 + 3 + 3 + 4 arrangement:
    /// - Group 1 = strongest 3-card group
    /// - Group 2 = second strongest 3-card group
    /// - Group 3 = weakest 3-card group
    /// - Group 4 = 4-card group ALWAYS
    /// </summary>
    public static class HazariGroupArrangementSolver
    {
        public struct ArrangementResult
        {
            public CardData[] ArrangedCards;
            public HandResult Group1Result;
            public HandResult Group2Result;
            public HandResult Group3Result;
            public HandResult Group4Result;
            public long OverallScore;

            public bool IsValid => ArrangedCards != null && ArrangedCards.Length == HazariRules.CardsPerPlayer;
        }

        public static ArrangementResult Solve(IReadOnlyList<CardData> dealtCards)
        {
            if (dealtCards == null || dealtCards.Count != HazariRules.CardsPerPlayer)
                return default;

            var cards = new CardData[HazariRules.CardsPerPlayer];
            for (var i = 0; i < HazariRules.CardsPerPlayer; i++)
                cards[i] = dealtCards[i];

            // 13 C 3 = 286 combinations
            var combo3Masks = new int[286];
            var combo3Results = new HandResult[286];
            var combo3Scores = new long[286];
            var maskToCombo3 = new int[1 << HazariRules.CardsPerPlayer];
            for (var i = 0; i < maskToCombo3.Length; i++)
                maskToCombo3[i] = -1;

            var c3Idx = 0;
            var temp3 = new CardData[3];
            for (var i = 0; i < 11; i++)
            {
                for (var j = i + 1; j < 12; j++)
                {
                    for (var k = j + 1; k < 13; k++)
                    {
                        var mask = (1 << i) | (1 << j) | (1 << k);
                        temp3[0] = cards[i];
                        temp3[1] = cards[j];
                        temp3[2] = cards[k];
                        var res = HandEvaluator.Evaluate(temp3);
                        var score = Get3CardScore(res, temp3);

                        combo3Masks[c3Idx] = mask;
                        combo3Results[c3Idx] = res;
                        combo3Scores[c3Idx] = score;
                        maskToCombo3[mask] = c3Idx;
                        c3Idx++;
                    }
                }
            }

            // 13 C 4 = 715 combinations
            var combo4Masks = new int[715];
            var combo4Results = new HandResult[715];
            var combo4Scores = new long[715];

            var c4Idx = 0;
            var temp4 = new CardData[4];
            for (var i = 0; i < 10; i++)
            {
                for (var j = i + 1; j < 11; j++)
                {
                    for (var k = j + 1; k < 12; k++)
                    {
                        for (var l = k + 1; l < 13; l++)
                        {
                            var mask = (1 << i) | (1 << j) | (1 << k) | (1 << l);
                            temp4[0] = cards[i];
                            temp4[1] = cards[j];
                            temp4[2] = cards[k];
                            temp4[3] = cards[l];
                            var res = HandEvaluator.Evaluate(temp4);
                            var score = Get4CardScore(res, temp4);

                            combo4Masks[c4Idx] = mask;
                            combo4Results[c4Idx] = res;
                            combo4Scores[c4Idx] = score;
                            c4Idx++;
                        }
                    }
                }
            }

            long bestScore = long.MinValue;
            int bestG1Mask = 0;
            int bestG2Mask = 0;
            int bestG3Mask = 0;
            int bestG4Mask = 0;
            HandResult bestRes1 = default;
            HandResult bestRes2 = default;
            HandResult bestRes3 = default;
            HandResult bestRes4 = default;

            var rem9 = new int[9];
            var rem6 = new int[6];

            for (var c4 = 0; c4 < 715; c4++)
            {
                var mask4 = combo4Masks[c4];
                var score4 = combo4Scores[c4];
                var res4 = combo4Results[c4];

                var remMask = 0x1FFF ^ mask4;
                var r9Count = 0;
                for (var b = 0; b < 13; b++)
                {
                    if ((remMask & (1 << b)) != 0)
                        rem9[r9Count++] = b;
                }

                // Partition 9 remaining into 3 sets of 3.
                // Group A must contain rem9[0]. Choose 2 from rem9[1..8] (28 pairs).
                var r0 = rem9[0];
                for (var a1 = 1; a1 < 8; a1++)
                {
                    var ra1 = rem9[a1];
                    for (var a2 = a1 + 1; a2 < 9; a2++)
                    {
                        var ra2 = rem9[a2];
                        var maskA = (1 << r0) | (1 << ra1) | (1 << ra2);
                        var idxA = maskToCombo3[maskA];
                        var scoreA = combo3Scores[idxA];
                        var resA = combo3Results[idxA];

                        var r6Count = 0;
                        for (var i = 1; i < 9; i++)
                        {
                            if (i != a1 && i != a2)
                                rem6[r6Count++] = rem9[i];
                        }

                        // Group B must contain rem6[0]. Choose 2 from rem6[1..5] (10 pairs).
                        var rb0 = rem6[0];
                        for (var b1 = 1; b1 < 5; b1++)
                        {
                            var rbb1 = rem6[b1];
                            for (var b2 = b1 + 1; b2 < 6; b2++)
                            {
                                var rbb2 = rem6[b2];
                                var maskB = (1 << rb0) | (1 << rbb1) | (1 << rbb2);
                                var idxB = maskToCombo3[maskB];
                                var scoreB = combo3Scores[idxB];
                                var resB = combo3Results[idxB];

                                var maskC = remMask ^ maskA ^ maskB;
                                var idxC = maskToCombo3[maskC];
                                var scoreC = combo3Scores[idxC];
                                var resC = combo3Results[idxC];

                                // Order {A, B, C} into G1 >= G2 >= G3
                                int m1, m2, m3;
                                long s1, s2, s3;
                                HandResult r1, r2, r3;

                                OrderThree(maskA, scoreA, resA,
                                           maskB, scoreB, resB,
                                           maskC, scoreC, resC,
                                           out m1, out s1, out r1,
                                           out m2, out s2, out r2,
                                           out m3, out s3, out r3);

                                var totalScore = s1 + s2 + s3 + (long)(score4 * 1.35f);

                                // Strategic synergy: bonus for multiple winning-grade groups
                                var strongCount = 0;
                                if (r1.Category >= HandCategory.Color) strongCount++;
                                if (r2.Category >= HandCategory.Color) strongCount++;
                                if (r3.Category >= HandCategory.Color) strongCount++;
                                if (res4.Category >= HandCategory.Color) strongCount++;

                                if (strongCount >= 3)
                                    totalScore += 2_000_000L;
                                else if (strongCount >= 2)
                                    totalScore += 750_000L;

                                if (totalScore > bestScore)
                                {
                                    bestScore = totalScore;
                                    bestG1Mask = m1;
                                    bestG2Mask = m2;
                                    bestG3Mask = m3;
                                    bestG4Mask = mask4;
                                    bestRes1 = r1;
                                    bestRes2 = r2;
                                    bestRes3 = r3;
                                    bestRes4 = res4;
                                }
                            }
                        }
                    }
                }
            }

            var arranged = new CardData[HazariRules.CardsPerPlayer];
            FillGroup(cards, bestG1Mask, arranged, 0, 3);
            FillGroup(cards, bestG2Mask, arranged, 3, 3);
            FillGroup(cards, bestG3Mask, arranged, 6, 3);
            FillGroup(cards, bestG4Mask, arranged, 9, 4);

            return new ArrangementResult
            {
                ArrangedCards = arranged,
                Group1Result = bestRes1,
                Group2Result = bestRes2,
                Group3Result = bestRes3,
                Group4Result = bestRes4,
                OverallScore = bestScore
            };
        }

        static void OrderThree(
            int mA, long sA, HandResult rA,
            int mB, long sB, HandResult rB,
            int mC, long sC, HandResult rC,
            out int m1, out long s1, out HandResult r1,
            out int m2, out long s2, out HandResult r2,
            out int m3, out long s3, out HandResult r3)
        {
            // Compare A and B
            int cmpAB = HazariRules.Compare(rA, rB);
            int mHighAB = cmpAB >= 0 ? mA : mB;
            long sHighAB = cmpAB >= 0 ? sA : sB;
            HandResult rHighAB = cmpAB >= 0 ? rA : rB;

            int mLowAB = cmpAB >= 0 ? mB : mA;
            long sLowAB = cmpAB >= 0 ? sB : sA;
            HandResult rLowAB = cmpAB >= 0 ? rB : rA;

            if (HazariRules.Compare(rC, rHighAB) >= 0)
            {
                // C is greatest
                m1 = mC; s1 = sC; r1 = rC;
                m2 = mHighAB; s2 = sHighAB; r2 = rHighAB;
                m3 = mLowAB; s3 = sLowAB; r3 = rLowAB;
            }
            else if (HazariRules.Compare(rC, rLowAB) <= 0)
            {
                // C is smallest
                m1 = mHighAB; s1 = sHighAB; r1 = rHighAB;
                m2 = mLowAB; s2 = sLowAB; r2 = rLowAB;
                m3 = mC; s3 = sC; r3 = rC;
            }
            else
            {
                // C is in between
                m1 = mHighAB; s1 = sHighAB; r1 = rHighAB;
                m2 = mC; s2 = sC; r2 = rC;
                m3 = mLowAB; s3 = sLowAB; r3 = rLowAB;
            }
        }

        static void FillGroup(CardData[] source, int mask, CardData[] destination, int destOffset, int count)
        {
            var temp = new List<CardData>(count);
            for (var i = 0; i < source.Length; i++)
            {
                if ((mask & (1 << i)) != 0)
                    temp.Add(source[i]);
            }

            temp.Sort(CompareHighToLow);
            for (var i = 0; i < count && i < temp.Count; i++)
                destination[destOffset + i] = temp[i];
        }

        static int CompareHighToLow(CardData left, CardData right)
        {
            var rank = right.Rank.CompareTo(left.Rank);
            if (rank != 0)
                return rank;
            return left.Suit.CompareTo(right.Suit);
        }

        static long Get3CardScore(HandResult hr, IReadOnlyList<CardData> cards)
        {
            long baseScore;
            switch (hr.Category)
            {
                case HandCategory.Trail:
                    baseScore = 18_000_000L + hr.PrimaryValue * 100_000L;
                    break;
                case HandCategory.PureSequence:
                    baseScore = 8_000_000L + hr.PrimaryValue * 50_000L;
                    break;
                case HandCategory.Sequence:
                    baseScore = 3_000_000L + hr.PrimaryValue * 30_000L;
                    break;
                case HandCategory.Color:
                    baseScore = 1_000_000L + hr.PrimaryValue * 20_000L + hr.SecondaryValue * 1_000L + hr.TieBreakValue;
                    break;
                case HandCategory.Pair:
                    baseScore = 200_000L + hr.PrimaryValue * 10_000L + hr.SecondaryValue * 500L;
                    break;
                case HandCategory.HighCard:
                    baseScore = 20_000L + hr.PrimaryValue * 1_000L + hr.SecondaryValue * 50L + hr.TieBreakValue;
                    break;
                default:
                    baseScore = 0;
                    break;
            }

            var cardPoints = CardValueCalculator.Sum(cards);
            if (hr.Category >= HandCategory.Sequence)
                baseScore += cardPoints * 500L;
            else if (hr.Category == HandCategory.Color)
                baseScore += cardPoints * 200L;
            else if (hr.Category <= HandCategory.HighCard)
                baseScore -= cardPoints * 300L;

            return baseScore;
        }

        static long Get4CardScore(HandResult hr, IReadOnlyList<CardData> cards)
        {
            long baseScore;
            switch (hr.Category)
            {
                case HandCategory.FourOfAKind:
                    baseScore = 25_000_000L + hr.PrimaryValue * 100_000L;
                    break;
                case HandCategory.Trail:
                    baseScore = 9_000_000L + hr.PrimaryValue * 50_000L + hr.SecondaryValue * 1_000L;
                    break;
                case HandCategory.PureSequence:
                    baseScore = 6_000_000L + hr.PrimaryValue * 40_000L;
                    break;
                case HandCategory.Sequence:
                    baseScore = 2_200_000L + hr.PrimaryValue * 25_000L;
                    break;
                case HandCategory.Color:
                    baseScore = 800_000L + hr.PrimaryValue * 20_000L + hr.SecondaryValue * 1_000L + hr.TieBreakValue;
                    break;
                case HandCategory.TwoPair:
                    baseScore = 350_000L + hr.PrimaryValue * 10_000L + hr.SecondaryValue * 1_000L;
                    break;
                case HandCategory.Pair:
                    baseScore = 120_000L + hr.PrimaryValue * 5_000L + hr.SecondaryValue * 200L;
                    break;
                case HandCategory.HighCard:
                    baseScore = 15_000L + hr.PrimaryValue * 400L + hr.SecondaryValue * 20L + hr.TieBreakValue;
                    break;
                default:
                    baseScore = 0;
                    break;
            }

            var cardPoints = CardValueCalculator.Sum(cards);
            if (hr.Category >= HandCategory.Color)
                baseScore += cardPoints * 600L;
            else if (hr.Category <= HandCategory.HighCard)
                baseScore -= cardPoints * 400L;

            return baseScore;
        }

        public static string SelfCheck()
        {
            var sampleIds = new[]
            {
                "AS", "AH", "AD", "KS", "QS", "JS", "8H", "8C", "5D", "10S", "7H", "3D", "2C"
            };
            var hand = new CardData[sampleIds.Length];
            for (var i = 0; i < sampleIds.Length; i++)
            {
                if (!CardData.TryParse(sampleIds[i], out hand[i]))
                    return "SelfCheck parse failed for " + sampleIds[i];
            }

            var solved = Solve(hand);
            if (!solved.IsValid)
                return "Solver returned invalid arrangement.";

            if (HazariRules.Compare(solved.Group1Result, solved.Group2Result) < 0)
                return "Solver violated: Group 1 < Group 2.";
            if (HazariRules.Compare(solved.Group2Result, solved.Group3Result) < 0)
                return "Solver violated: Group 2 < Group 3.";

            if (solved.Group1Result.Category != HandCategory.Trail)
                return $"Solver failed: expected G1 Trail, got {solved.Group1Result.Category}";
            if (solved.Group4Result.Category != HandCategory.PureSequence)
                return $"Solver failed: expected G4 PureSequence, got {solved.Group4Result.Category}";

            return $"HazariGroupArrangementSolver passed: G1={solved.Group1Result.Category}, G2={solved.Group2Result.Category}, G3={solved.Group3Result.Category}, G4={solved.Group4Result.Category}.";
        }
    }
}
