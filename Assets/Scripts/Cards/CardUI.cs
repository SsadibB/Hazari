using UnityEngine;
using UnityEngine.UI;

namespace Hazari.Cards
{
    /// <summary>
    /// Visuals for one reusable card prefab. Sprite changes do not change card identity.
    /// </summary>
    public sealed class CardUI : MonoBehaviour
    {
        [SerializeField] Image faceImage;
        [SerializeField] GameObject backRoot;

        void Awake()
        {
            if (faceImage == null)
                faceImage = GetComponent<Image>();
        }

        public void ShowFace(Sprite sprite)
        {
            if (faceImage != null)
                faceImage.sprite = sprite;

            if (backRoot != null)
                backRoot.SetActive(false);

            if (faceImage != null)
                faceImage.enabled = sprite != null;
        }

        public void ShowBack()
        {
            if (faceImage != null)
                faceImage.enabled = false;

            if (backRoot != null)
                backRoot.SetActive(true);
        }
    }
}
