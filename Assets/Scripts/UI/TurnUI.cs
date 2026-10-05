using UnityEngine;
using UnityEngine.UI;

namespace Hazari.UI
{
    /// <summary>
    /// Turn banner. The same object is updated for "your turn" and "waiting". It is not recreated each turn.
    /// </summary>
    public sealed class TurnUI : MonoBehaviour
    {
        [SerializeField] Text label;

        public string Message { get; private set; } = string.Empty;

        public void SetMessage(string message)
        {
            Message = message ?? string.Empty;
            if (label != null)
                label.text = Message;
        }
    }
}
