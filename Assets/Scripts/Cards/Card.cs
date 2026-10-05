using UnityEngine;

namespace Hazari.Cards
{
    /// <summary>
    /// Scene object for one card. Moving this transform does not change CardId, suit, rank, or owner.
    /// </summary>
    public sealed class Card : MonoBehaviour
    {
        public CardData Data { get; private set; }
        public bool HasData { get; private set; }

        public void Bind(CardData data)
        {
            Data = data;
            HasData = true;
        }

        public void Clear()
        {
            Data = default;
            HasData = false;
        }
    }
}
