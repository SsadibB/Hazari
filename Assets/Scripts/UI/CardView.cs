using System;
using DG.Tweening;
using Hazari.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hazari.UI
{
    /// <summary>
    /// One card already placed in the scene. Dragging reorders that card. It does not create another card.
    /// </summary>
    public sealed class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
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
        Shadow _shadow;

        public CardData Data => card != null && card.HasData ? card.Data : default;
        public RectTransform Rect => rect != null ? rect : (RectTransform)transform;
        public bool IsDragging => _dragging;
        public bool DragEnabled { get; set; }
        public Action<CardView> Clicked;
        public Action<CardView> DragMoved;
        public Action<CardView> DragEnded;

        public void CaptureHome()
        {
            if (rect == null)
                rect = (RectTransform)transform;
            _home = rect.anchoredPosition;
            _homeSibling = transform.GetSiblingIndex();
        }

        public void SetHome(Vector2 anchoredPosition, int siblingIndex, bool animate = false)
        {
            if (rect == null)
                rect = (RectTransform)transform;

            _home = anchoredPosition;
            _homeSibling = siblingIndex;
            if (_dragging)
                return;

            transform.SetSiblingIndex(siblingIndex);
            rect.DOKill();
            if (animate)
                rect.DOAnchorPos(_home, 0.22f).SetEase(Ease.OutCubic);
            else
                rect.anchoredPosition = _home;
        }

        public void Show(CardData data, Sprite sprite)
        {
            if (card != null)
                card.Bind(data);
            if (cardUi != null)
                cardUi.ShowFace(sprite);
        }

        public void SetOwner(int seat)
        {
            if (card != null)
                card.SetOwner(seat);
        }

        public void SetLifted(bool lifted)
        {
            if (rect == null || _dragging)
                return;

            rect.DOKill();
            rect.DOAnchorPos(_home + (lifted ? new Vector2(0f, 28f) : Vector2.zero), 0.16f).SetEase(Ease.OutCubic);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!DragEnabled)
                return;

            _dragging = true;
            _moved = false;
            if (rect == null)
                rect = (RectTransform)transform;

            rect.DOKill();
            transform.SetAsLastSibling();
            rect.localScale = Vector3.one;
            EnsureShadow().effectColor = new Color(0f, 0f, 0f, 0.45f);

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

            if (DragMoved != null)
                DragMoved(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging)
                return;

            _dragging = false;
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            rect.DOKill();
            rect.localScale = Vector3.one;
            if (_shadow != null)
                _shadow.effectColor = new Color(0f, 0f, 0f, 0.18f);

            if (DragEnded != null)
                DragEnded(this);
            else
                rect.DOAnchorPos(_home, 0.18f).SetEase(Ease.OutCubic);
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

        void OnDisable()
        {
            if (rect != null)
                rect.DOKill();
        }

        Shadow EnsureShadow()
        {
            if (_shadow == null)
            {
                _shadow = GetComponent<Shadow>();
                if (_shadow == null)
                    _shadow = gameObject.AddComponent<Shadow>();
                _shadow.effectDistance = new Vector2(0f, -5f);
                _shadow.useGraphicAlpha = true;
            }

            return _shadow;
        }
    }
}
