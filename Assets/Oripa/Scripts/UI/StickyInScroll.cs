using UnityEngine;
using UnityEngine.UI;

namespace Oripa.UI
{
    /// <summary>
    /// 세로 스크롤 안의 줄을 화면 위에 붙여 둔다 (position: sticky).
    /// 이 오브젝트는 레이아웃 자리만 지키고, 실제 내용은 자식 sticky가 들고 있다.
    /// 자리가 pinTop보다 위로 올라가면 sticky를 스크롤 밖의 pinnedLayer로 옮겨 그 높이에 고정하고,
    /// 다시 내려오면 제자리로 돌려놓는다. above를 지정하면 그 줄 바로 밑에 붙는다 (타이틀 밑에 탭 줄).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]   // ScrollRect가 내용을 움직인 다음에 맞춘다
    public class StickyInScroll : MonoBehaviour
    {
        public RectTransform sticky;
        public StickyInScroll above;
        [Tooltip("뷰 위쪽에서 이만큼 아래에 붙는다 (above가 있으면 무시).")]
        public float pinTop;
        [Tooltip("켜면 스크롤 위치와 상관없이 항상 고정 (타이틀).")]
        public bool alwaysPinned;
        [Tooltip("켜면 내용을 화면(기둥) 폭 전체로 편다.")]
        public bool fullWidth;
        [Tooltip("고정될 때 옮겨 가는 곳 (스크롤 밖, 스크롤보다 위에 그려지는 층).")]
        public RectTransform pinnedLayer;

        ScrollRect scroll;
        RectTransform viewport;
        bool pinned;

        RectTransform Host => (RectTransform)transform;
        public float PinnedTop => above != null ? above.PinnedBottom : pinTop;
        public float PinnedBottom => PinnedTop + Host.rect.height;

        void OnDisable() => Unpin();

        void LateUpdate()
        {
            if (!Application.isPlaying || sticky == null) return;
            if (viewport == null)
            {
                scroll = transform.parent != null ? transform.parent.GetComponentInParent<ScrollRect>() : null;
                if (scroll == null) return;
                viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            }

            var host = Host;
            float yTop = viewport.rect.yMax - viewport.InverseTransformPoint(host.TransformPoint(new Vector3(0f, host.rect.yMax, 0f))).y;
            bool want = pinnedLayer != null && (alwaysPinned || yTop <= PinnedTop);
            if (want && !pinned) Pin();
            else if (!want && pinned) Unpin();

            // 좌우 범위: fullWidth면 기둥 폭 전체, 아니면 원래 자리와 같은 폭
            var cols = pinnedLayer != null ? pinnedLayer : viewport;
            var parent = (RectTransform)sticky.parent;
            float left, right;
            if (fullWidth)
            {
                left = parent.InverseTransformPoint(cols.TransformPoint(new Vector3(cols.rect.xMin, 0f, 0f))).x;
                right = parent.InverseTransformPoint(cols.TransformPoint(new Vector3(cols.rect.xMax, 0f, 0f))).x;
            }
            else
            {
                left = parent.InverseTransformPoint(host.TransformPoint(new Vector3(host.rect.xMin, 0f, 0f))).x;
                right = parent.InverseTransformPoint(host.TransformPoint(new Vector3(host.rect.xMax, 0f, 0f))).x;
            }

            if (pinned)
            {
                // 층 위쪽에서 (뷰 위쪽까지 거리 + PinnedTop) 아래에 붙인다
                float viewTop = parent.InverseTransformPoint(viewport.TransformPoint(new Vector3(0f, viewport.rect.yMax, 0f))).y;
                float top = viewTop - PinnedTop;
                Place(parent, left, right, top, top - host.rect.height);
            }
            else
            {
                Place(parent, left, right, host.rect.yMax, host.rect.yMin);
            }
        }

        /// <summary>parent 좌표의 좌/우/위/아래 값으로 sticky를 놓는다.</summary>
        void Place(RectTransform parent, float left, float right, float top, float bottom)
        {
            var r = parent.rect;
            sticky.anchorMin = Vector2.zero;
            sticky.anchorMax = Vector2.one;
            sticky.pivot = new Vector2(0.5f, 0.5f);
            sticky.offsetMin = new Vector2(left - r.xMin, bottom - r.yMin);
            sticky.offsetMax = new Vector2(right - r.xMax, top - r.yMax);
        }

        void Pin()
        {
            sticky.SetParent(pinnedLayer, false);
            if (above == null) sticky.SetAsFirstSibling(); else sticky.SetAsLastSibling();
            var fwd = sticky.GetComponent<DragForwarder>();
            if (fwd == null) fwd = sticky.gameObject.AddComponent<DragForwarder>();
            fwd.target = scroll;
            pinned = true;
        }

        void Unpin()
        {
            if (!pinned || sticky == null) return;
            pinned = false;
            sticky.SetParent(transform, false);
            sticky.anchorMin = Vector2.zero;
            sticky.anchorMax = Vector2.one;
            sticky.offsetMin = Vector2.zero;
            sticky.offsetMax = Vector2.zero;
        }
    }
}
