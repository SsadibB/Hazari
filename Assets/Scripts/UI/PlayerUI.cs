using UnityEngine;
using UnityEngine.UI;

namespace Hazari.UI
{
    /// <summary>
    /// Seat display. Avatar art is assigned to the existing Image. A new avatar object is not created per update.
    /// </summary>
    public sealed class PlayerUI : MonoBehaviour
    {
        [SerializeField] Image avatarImage;

        public void SetAvatar(Sprite sprite)
        {
            if (avatarImage == null)
                return;

            avatarImage.sprite = sprite;
            avatarImage.enabled = sprite != null;
        }
    }
}
