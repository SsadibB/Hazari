using System.Collections.Generic;
using Hazari.Cards;
using Hazari.Rules;

namespace Hazari.AI
{
    public static class SimpleHazariAi
    {
        public static List<CardData> Arrange(IReadOnlyList<CardData> dealt)
        {
            var pool = new List<CardData>();
            if (dealt != null)
            {
                for (var i = 0; i < dealt.Count; i++)
                    pool.Add(dealt[i]);
            }

            var arranged = new List<CardData>();
            for (var group = 0; group < HazariRules.GroupSizes.Length; group++)
            {
                var size = HazariRules.GroupSizes[group];
                if (pool.Count == size)
                {
                    arranged.AddRange(pool);
                    pool.Clear();
                    break;
                }

                var best = BestSubset(pool, size);
                for (var i = 0; i < best.Count; i++)
                {
                    arranged.Add(best[i]);
                    pool.Remove(best[i]);
                }
            }

            return arranged;
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
