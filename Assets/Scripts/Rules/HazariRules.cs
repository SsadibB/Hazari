namespace Hazari.Rules
{
    /// <summary>
    /// Shared Hazari constants and the only comparison entry point. Single-player and online both use this type.
    /// </summary>
    public static class HazariRules
    {
        public const int PlayerCount = 4;
        public const int CardsPerPlayer = 13;
        public const int DeckSize = 52;
        public static readonly int[] GroupSizes = { 3, 3, 3, 4 };

        public static int GroupStart(int groupIndex)
        {
            return groupIndex >= 3 ? 9 : groupIndex * 3;
        }

        public static int Compare(HandResult left, HandResult right)
        {
            if (!left.IsValid && !right.IsValid)
                return 0;
            if (!left.IsValid)
                return -1;
            if (!right.IsValid)
                return 1;

            var category = left.CategoryStrength.CompareTo(right.CategoryStrength);
            if (category != 0)
                return category;

            var primary = left.PrimaryValue.CompareTo(right.PrimaryValue);
            if (primary != 0)
                return primary;

            var secondary = left.SecondaryValue.CompareTo(right.SecondaryValue);
            if (secondary != 0)
                return secondary;

            return left.TieBreakValue.CompareTo(right.TieBreakValue);
        }
    }
}
