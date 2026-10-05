using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hazari.Cards
{
    /// <summary>
    /// 52-card deck. Shuffle uses Fisher-Yates. Online matches must pass the authoritative order in later.
    /// </summary>
    public sealed class DeckManager : MonoBehaviour
    {
        readonly List<CardData> _cards = new List<CardData>();

        public int Count => _cards.Count;
        public IReadOnlyList<CardData> Cards => _cards;

        public void Clear()
        {
            _cards.Clear();
        }

        public void BuildStandardDeck()
        {
            _cards.Clear();
            var seen = new HashSet<string>();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    var card = CardData.Create(suit, rank);
                    if (!seen.Add(card.CardId))
                        throw new InvalidOperationException("Duplicate card " + card.CardId);

                    _cards.Add(card);
                }
            }

            if (_cards.Count != 52)
                throw new InvalidOperationException("Deck built " + _cards.Count + " cards.");
        }

        public void Shuffle(int seed)
        {
            var random = new System.Random(seed);
            for (var i = _cards.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var swap = _cards[i];
                _cards[i] = _cards[j];
                _cards[j] = swap;
            }
        }

        public CardData Draw()
        {
            if (_cards.Count == 0)
                throw new InvalidOperationException("Cannot draw from an empty deck.");

            var card = _cards[0];
            _cards.RemoveAt(0);
            return card;
        }
    }
}
