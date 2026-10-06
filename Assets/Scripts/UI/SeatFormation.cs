using UnityEngine;

namespace Hazari.UI
{
    /// <summary>
    /// Shared overlap math so the scene and the match use the same hand shape.
    /// </summary>
    public static class SeatFormation
    {
        public static readonly Vector2 BottomCardSize = new Vector2(134f, 186f);
        public const float BottomStep = 100f;

        public static readonly Vector2 TopCardSize = new Vector2(40f, 58f);
        public const float TopStep = 15f;
        public const float TopRotation = 180f;

        public static readonly Vector2 SideCardSize = new Vector2(46f, 68f);
        public const float SideStep = 14f;
        public const float LeftRotation = -90f;
        public const float RightRotation = 90f;
        public static readonly Vector2 CenterCardSize = new Vector2(90f, 126f);
        public const float CenterStep = 86f;
        public const float GroupGap = 28f;

        public static void Place(RectTransform rect, int index, int count, bool vertical, Vector2 size, float step, float rotation)
        {
            Place(rect, index, count, vertical, size, step, rotation, false);
        }

        public static void Place(RectTransform rect, int index, int count, bool vertical, Vector2 size, float step, float rotation, bool groupGaps)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            rect.anchoredPosition = Slot(index, count, vertical, step, groupGaps);
            rect.SetSiblingIndex(index);
        }

        public static Vector2 Slot(int index, int count, bool vertical, float step, bool groupGaps)
        {
            var extra = groupGaps && !vertical ? GroupExtra(index) : 0f;
            var totalExtra = groupGaps && !vertical && count > 0 ? GroupExtra(count - 1) : 0f;
            var span = (count <= 1 ? 0f : (count - 1) * step) + totalExtra;
            var origin = -span * 0.5f;
            var along = vertical ? origin + (count - 1 - index) * step : origin + index * step + extra;
            return vertical ? new Vector2(0f, along) : new Vector2(along, 0f);
        }

        static float GroupExtra(int index)
        {
            var extra = 0f;
            if (index >= 3)
                extra += GroupGap;
            if (index >= 6)
                extra += GroupGap;
            if (index >= 9)
                extra += GroupGap;
            return extra;
        }
    }
}
