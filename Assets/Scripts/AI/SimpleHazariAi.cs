using System.Collections.Generic;
using Hazari.Cards;
using Hazari.Rules;

namespace Hazari.AI
{
    public static class SimpleHazariAi
    {
        public static List<CardData> Arrange(IReadOnlyList<CardData> dealt)
        {
            if (dealt == null || dealt.Count != HazariRules.CardsPerPlayer)
                return new List<CardData>(dealt ?? new CardData[0]);

            var solved = HazariGroupArrangementSolver.Solve(dealt);
            if (solved.IsValid)
                return new List<CardData>(solved.ArrangedCards);

            return new List<CardData>(dealt);
        }

        static List<CardData> BestSubset(List<CardData> pool, int size)
        {
            var best = new List<CardData>();
            var bestResult = HandResult.Invalid;
            var indexes = new int[size];
            Search(pool, size, 0, 0, indexes, ref best, ref bestResult);
            return best;
        }

        static void Search(List<CardData> pool, int size, int start, int depth, int[] indexes, ref List<CardData> best, ref HandResult bestResult)
        {
            if (depth == size)
            {
                var candidate = new CardData[size];
                for (var i = 0; i < size; i++)
                    candidate[i] = pool[indexes[i]];

                var result = HandEvaluator.Evaluate(candidate);
                if (best.Count == 0 || HazariRules.Compare(result, bestResult) > 0)
                {
                    best = new List<CardData>(candidate);
                    bestResult = result;
                }

                return;
            }

            for (var i = start; i <= pool.Count - (size - depth); i++)
            {
                indexes[depth] = i;
                Search(pool, size, i + 1, depth + 1, indexes, ref best, ref bestResult);
            }
        }
    }
}
