using System.Collections.Generic;
using UnityEngine;

namespace Hazari.Cards
{
    /// <summary>
    /// Order of the local player's cards. Order is card ids, not transform positions.
    /// Sort saves the manual order and restores that same order. It does not restore the deal.
    /// </summary>
    public sealed class CardArrangementManager : MonoBehaviour
    {
        readonly List<string> _currentCardOrder = new List<string>();
        readonly List<string> _manualCardOrder = new List<string>();
        readonly List<string> _sortedCardOrder = new List<string>();

        public IReadOnlyList<string> CurrentCardOrder => _currentCardOrder;
        public IReadOnlyList<string> ManualCardOrder => _manualCardOrder;
        public IReadOnlyList<string> SortedCardOrder => _sortedCardOrder;
        public bool IsSorted { get; private set; }

        public bool DraggingEnabled => true;

        public void LoadDealt(IReadOnlyList<string> cardIds)
        {
            IsSorted = false;
            Replace(_currentCardOrder, cardIds);
            Replace(_manualCardOrder, cardIds);
            _sortedCardOrder.Clear();
        }

        public void NotifyManualOrder(IReadOnlyList<string> cardIds)
        {
            Replace(_currentCardOrder, cardIds);
            if (!IsSorted)
                Replace(_manualCardOrder, cardIds);
        }

        public void AcceptRearrange(IReadOnlyList<string> cardIds)
        {
            Replace(_currentCardOrder, cardIds);
            Replace(_manualCardOrder, cardIds);
            IsSorted = false;
        }

        public bool RequestSort()
        {
            if (IsSorted || _currentCardOrder.Count == 0)
                return false;

            Replace(_manualCardOrder, _currentCardOrder);
            var sorted = CombinationSorter.Sort(_currentCardOrder);
            Replace(_sortedCardOrder, sorted);
            Replace(_currentCardOrder, sorted);
            IsSorted = true;
            return true;
        }

        public bool RequestUnsort()
        {
            if (!IsSorted)
                return false;

            Replace(_currentCardOrder, _manualCardOrder);
            IsSorted = false;
            return true;
        }

        public void SetManualOrder(IReadOnlyList<string> cardIds)
        {
            Replace(_manualCardOrder, cardIds);
            if (!IsSorted)
                Replace(_currentCardOrder, cardIds);
        }

        public static readonly int[] GroupSizes = { 3, 3, 3, 4 };

        static void Replace(List<string> target, IReadOnlyList<string> source)
        {
            target.Clear();
            if (source == null)
                return;

            for (var i = 0; i < source.Count; i++)
                target.Add(source[i]);
        }
    }
}
