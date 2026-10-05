using Hazari.Rules;

namespace Hazari.Scoring
{
    public static class ScoreCalculator
    {
        public static int AwardForHand(HandResult result)
        {
            if (!result.IsValid)
                return 0;

            return result.CategoryStrength * 20 + result.PrimaryValue;
        }
    }
}
