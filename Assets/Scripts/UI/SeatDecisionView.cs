using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Hazari.UI
{
    /// <summary>
    /// Turn cue beside a player. It pulses without covering that player's cards.
    /// </summary>
    public sealed class SeatDecisionView : MonoBehaviour
    {
        [SerializeField] Text label;

        public void Show(string message, bool pulse)
        {
            transform.DOKill();
            transform.localScale = Vector3.one;

            if (string.IsNullOrEmpty(message))
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (label != null)
                label.text = message;

            if (pulse)
                transform.DOScale(1.08f, 0.45f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }
}
