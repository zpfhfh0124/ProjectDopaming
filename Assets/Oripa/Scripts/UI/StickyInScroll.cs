using UnityEngine;
using UnityEngine.UI;

namespace Oripa.UI
{
    /// <summary>
    /// 세로 스크롤 안의 줄을 화면 위에 붙여 둔다 (position: sticky).
    /// 이 오브젝트는 레이아웃 자리만 지키고, 실제 내용은 자식 sticky가 들고 있다.
    /// 스크롤해서 자리가 pinTop 위로 올라가면 sticky를 그만큼 내려서 그 위치에 붙어 있게 한다.
    /// above를 지정하면 그 줄 바로 밑에 붙는다 (타이틀 밑에 탭 줄).
    /// </summary>
    [DisallowMultipleComponent]
    public class StickyInScroll : MonoBehaviour
    {
        public RectTransform sticky;
        public StickyInScroll above;
        [Tooltip("뷰 위쪽에서 이만큼 아래에 붙는다 (above가 있으면 무시).")]
        public float pinTop;

        RectTransform viewport;

        public float PinnedTop => above != null ? above.PinnedBottom : pinTop;
        public float PinnedBottom => PinnedTop + ((RectTransform)transform).rect.height;

        void OnEnable()
        {
            if (Application.isPlaying) Canvas.willRenderCanvases += Apply;
        }

        void OnDisable()
        {
            Canvas.willRenderCanvases -= Apply;
            if (sticky != null) sticky.anchoredPosition = Vector2.zero;
        }

        // 레이아웃과 ScrollRect 이동이 모두 끝난 뒤(그리기 직전)에 맞춘다.
        void Apply()
        {
            if (sticky == null || this == null) return;
            if (viewport == null)
            {
                var scroll = transform.parent != null ? transform.parent.GetComponentInParent<ScrollRect>() : null;
                if (scroll == null) return;
                viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            }
            var rt = (RectTransform)transform;
            var topWorld = rt.TransformPoint(new Vector3(0f, rt.rect.yMax, 0f));
            float yTop = viewport.rect.yMax - viewport.InverseTransformPoint(topWorld).y;   // 뷰 위쪽에서 이 줄 위쪽까지
            float down = Mathf.Max(0f, PinnedTop - yTop);
            sticky.anchoredPosition = new Vector2(0f, -down);
        }
    }
}
