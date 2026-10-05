using System.Collections.Generic;

namespace Hazari.Players
{
    /// <summary>
    /// Card ids owned by one player, in arrangement order.
    /// </summary>
    public sealed class PlayerHand
    {
        readonly List<string> _cardIds = new List<string>();

        public IReadOnlyList<string> CardIds => _cardIds;
        public int Count => _cardIds.Count;

        public void SetCards(IReadOnlyList<string> cardIds)
        {
            _cardIds.Clear();
            if (cardIds == null)
                return;

            for (var i = 0; i < cardIds.Count; i++)
                _cardIds.Add(cardIds[i]);
        }

        public void Clear()
        {
            _cardIds.Clear();
        }
    }
}
