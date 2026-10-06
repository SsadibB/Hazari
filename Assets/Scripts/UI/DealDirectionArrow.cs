using DG.Tweening;
using UnityEngine;

namespace Hazari.UI
{
    /// <summary>
    /// One arrow. The artwork points left, and it turns toward the player who takes the center cards.
    /// </summary>
    public sealed class DealDirectionArrow : MonoBehaviour
    {
        [SerializeField] RectTransform visual;
        [SerializeField] CanvasGroup canvasGroup;

        Tween _pulse;

        public void ShowToward(int seat)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            KillMotion();

            var root = (RectTransform)transform;
            root.anchoredPosition = OffsetFor(seat);
            if (visual != null)
            {
                visual.localRotation = Quaternion.Euler(0f, 0f, AngleFor(seat));
                visual.localScale = Vector3.one * 0.92f;
                _pulse = visual.DOScale(1.06f, 0.28f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.12f);
            }
        }

        public void Hide()
        {
            KillMotion();
            if (visual != null)
                visual.DOKill();

            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.16f).OnComplete(() => gameObject.SetActive(false));
                return;
            }

            gameObject.SetActive(false);
        }

        void KillMotion()
        {
            if (_pulse != null)
                _pulse.Kill();
            ((RectTransform)transform).DOKill();
        }

        static float AngleFor(int seat)
        {
            switch (seat)
            {
                case 2: return -90f;
                case 3: return 180f;
                case 0: return 90f;
                default: return 0f;
            }
        }

        static Vector2 OffsetFor(int seat)
        {
            switch (seat)
            {
                case 1: return new Vector2(-86f, 0f);
                case 2: return new Vector2(0f, 86f);
                case 0: return new Vector2(0f, -86f);
                default: return new Vector2(86f, 0f);
            }
        }
    }
}
