using System;

namespace Hazari.Cards
{
    public enum Suit
    {
        Clubs = 0,
        Diamonds = 1,
        Hearts = 2,
        Spades = 3
    }

    public enum Rank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }

    /// <summary>
    /// Identity of one card. Position, sorting animation, and sprites are visual state and do not live here.
    /// CardId format is rank then suit, such as AS for the Ace of Spades and 10H for the Ten of Hearts.
    /// </summary>
    public readonly struct CardData : IEquatable<CardData>
    {
        public CardData(string cardId, Suit suit, Rank rank)
        {
            CardId = cardId ?? string.Empty;
            Suit = suit;
            Rank = rank;
        }

        public string CardId { get; }
        public Suit Suit { get; }
        public Rank Rank { get; }

        public static CardData Create(Suit suit, Rank rank)
        {
            return new CardData(CreateId(rank, suit), suit, rank);
        }

        public static string CreateId(Rank rank, Suit suit)
        {
            return RankLabel(rank) + SuitLabel(suit);
        }

        public bool Equals(CardData other)
        {
            return string.Equals(CardId, other.CardId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CardData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return CardId == null ? 0 : StringComparer.Ordinal.GetHashCode(CardId);
        }

        public override string ToString()
        {
            return CardId ?? string.Empty;
        }

        public static bool TryParse(string cardId, out CardData data)
        {
            data = default;
            if (string.IsNullOrEmpty(cardId) || cardId.Length < 2)
                return false;

            var suitChar = cardId[cardId.Length - 1];
            Suit suit;
            switch (suitChar)
            {
                case 'C': suit = Suit.Clubs; break;
                case 'D': suit = Suit.Diamonds; break;
                case 'H': suit = Suit.Hearts; break;
                case 'S': suit = Suit.Spades; break;
                default: return false;
            }

            var rankLabel = cardId.Substring(0, cardId.Length - 1);
            Rank rank;
            switch (rankLabel)
            {
                case "2": rank = Rank.Two; break;
                case "3": rank = Rank.Three; break;
                case "4": rank = Rank.Four; break;
                case "5": rank = Rank.Five; break;
                case "6": rank = Rank.Six; break;
                case "7": rank = Rank.Seven; break;
                case "8": rank = Rank.Eight; break;
                case "9": rank = Rank.Nine; break;
                case "10": rank = Rank.Ten; break;
                case "J": rank = Rank.Jack; break;
                case "Q": rank = Rank.Queen; break;
                case "K": rank = Rank.King; break;
                case "A": rank = Rank.Ace; break;
                default: return false;
            }

            data = Create(suit, rank);
            return data.CardId == cardId;
        }

        static string RankLabel(Rank rank)
        {
            switch (rank)
            {
                case Rank.Two: return "2";
                case Rank.Three: return "3";
                case Rank.Four: return "4";
                case Rank.Five: return "5";
                case Rank.Six: return "6";
                case Rank.Seven: return "7";
                case Rank.Eight: return "8";
                case Rank.Nine: return "9";
                case Rank.Ten: return "10";
                case Rank.Jack: return "J";
                case Rank.Queen: return "Q";
                case Rank.King: return "K";
                case Rank.Ace: return "A";
                default: return "?";
            }
        }

        static string SuitLabel(Suit suit)
        {
            switch (suit)
            {
                case Suit.Clubs: return "C";
                case Suit.Diamonds: return "D";
                case Suit.Hearts: return "H";
                case Suit.Spades: return "S";
                default: return "?";
            }
        }
    }
}
