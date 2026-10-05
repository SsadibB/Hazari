using System;
using Hazari.Cards;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hazari.UI
{
    /// <summary>
    /// One card already placed in the scene. Dragging swaps order. It does not create another card.
    /// </summary>
    public sealed class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    {
        [SerializeField] Card card;
        [SerializeField] CardUI cardUi;
        [SerializeField] RectTransform rect;
        [SerializeField] CanvasGroup canvasGroup;

        Vector2 _home;
        int _homeSibling;
        Vector3 _dragOffset;
        bool _dragging;
        bool _moved;

        public CardData Data => card != null && card.HasData ? card.Data : default;
        public bool DragEnabled { get; set; }
        public Action<CardView> Clicked;
        public Action<CardView, CardView> DroppedOn;

        public void CaptureHome()
        {
            if (rect == null)
                rect = (RectTransform)transform;
            _home = rect.anchoredPosition;
            _homeSibling = transform.GetSiblingIndex();
        }

        public void SetHome(Vector2 anchoredPosition, int siblingIndex)
        {
            if (rect == null)
                rect = (RectTransform)transform;

            _home = anchoredPosition;
            _homeSibling = siblingIndex;
            transform.SetSiblingIndex(siblingIndex);
            if (!_dragging)
                rect.anchoredPosition = _home;
        }

        public void Show(CardData data, Sprite sprite)
        {
            if (card != null)
                card.Bind(data);
            if (cardUi != null)
                cardUi.ShowFace(sprite);
        }

        public void SetLifted(bool lifted)
        {
            if (rect == null || _dragging)
                return;

            rect.anchoredPosition = _home + (lifted ? new Vector2(0f, 28f) : Vector2.zero);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!DragEnabled)
                return;

            _dragging = true;
            _moved = false;
            transform.SetAsLastSibling();
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = false;

            Vector3 world;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out world))
                _dragOffset = rect.position - world;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging)
                return;

            _moved = true;
            Vector3 world;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out world))
                rect.position = world + _dragOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging)
                return;

            _dragging = false;
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;
            rect.anchoredPosition = _home;
            transform.SetSiblingIndex(_homeSibling);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!DragEnabled || eventData.pointerDrag == null)
                return;

            var other = eventData.pointerDrag.GetComponent<CardView>();
            if (other == null || other == this)
                return;

            if (DroppedOn != null)
                DroppedOn(other, this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!DragEnabled || _moved)
            {
                _moved = false;
                return;
            }

            if (Clicked != null)
                Clicked(this);
        }
    }
}
