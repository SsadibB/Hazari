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
        public int OwnerSeat { get; private set; } = -1;
        public int PointValue => HasData ? CardValueCalculator.Points(Data) : 0;

        public void Bind(CardData data)
        {
            Data = data;
            HasData = true;
        }

        public void SetOwner(int seat)
        {
            OwnerSeat = seat;
        }

        public void Clear()
        {
            Data = default;
            HasData = false;
        }
    }
}
