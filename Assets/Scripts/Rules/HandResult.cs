namespace Hazari.Rules
{
    public enum HandCategory
    {
        Invalid = 0,
        HighCard = 1,
        Pair = 2,
        TwoPair = 3,
        Color = 4,
        Sequence = 5,
        PureSequence = 6,
        Trail = 7,
        FourOfAKind = 8
    }

    /// <summary>
    /// Structured hand value. Comparison uses these numbers, never card scene objects.
    /// </summary>
    public readonly struct HandResult
    {
        public HandResult(
            HandCategory category,
            int categoryStrength,
            int primaryValue,
            int secondaryValue,
            int tieBreakValue,
            bool isValid)
        {
            Category = category;
            CategoryStrength = categoryStrength;
            PrimaryValue = primaryValue;
            SecondaryValue = secondaryValue;
            TieBreakValue = tieBreakValue;
            IsValid = isValid;
        }

        public HandCategory Category { get; }
        public int CategoryStrength { get; }
        public int PrimaryValue { get; }
        public int SecondaryValue { get; }
        public int TieBreakValue { get; }
        public bool IsValid { get; }

        public static HandResult Invalid => new HandResult(HandCategory.Invalid, 0, 0, 0, 0, false);

        public static HandResult Of(HandCategory category, int primary, int secondary, int tieBreak)
        {
            return new HandResult(category, (int)category, primary, secondary, tieBreak, true);
        }
    }
}
