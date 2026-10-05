using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hazari.Cards
{
    public sealed class CardSpriteLibrary : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public string cardId;
            public Sprite sprite;
        }

        [SerializeField] Sprite cardBack;
        [SerializeField] Entry[] faces;

        readonly Dictionary<string, Sprite> _faces = new Dictionary<string, Sprite>();

        public Sprite CardBack => cardBack;

        void Awake()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _faces.Clear();
            if (faces == null)
                return;

            for (var i = 0; i < faces.Length; i++)
            {
                if (string.IsNullOrEmpty(faces[i].cardId) || faces[i].sprite == null)
                    continue;

                _faces[faces[i].cardId] = faces[i].sprite;
            }
        }

        public Sprite GetFace(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return null;

            if (_faces.Count == 0)
                Rebuild();

            Sprite sprite;
            return _faces.TryGetValue(cardId, out sprite) ? sprite : null;
        }
    }
}
