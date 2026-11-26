using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Oripa.UI
{
    /// <summary>
    /// 가로로 넘기는 배너. content 아래 켜져 있는 자식 하나가 한 페이지다 (페이지 폭 = 배너 폭).
    /// - 손을 떼면 가장 가까운 페이지로 붙는다 (빠르게 밀면 다음/이전 페이지).
    /// - autoInterval 초마다 다음 페이지로 넘어간다 (0이면 끔).
    /// - 세로로 끄는 동작은 바깥 세로 스크롤(페이지 전체)로 넘겨서 배너 위에서도 화면이 스크롤된다.
    /// - dots 아래에 페이지 점을 Play 시작 때 만든다.
    /// 페이지를 늘리려면 Scene에서 Pages 아래 Page를 복제하면 된다.
    /// </summary>
    public class BannerCarousel : ScrollRect
    {
        public float autoInterval = 4f;
        public float snapSpeed = 12f;
        public RectTransform dots;

        ScrollRect parentScroll;
        bool routeToParent;
        bool dragging;
        int page;
        float target = -1f;
        float idle;

        int PageCount
        {
            get
            {
                if (content == null) return 0;
                int n = 0;
                for (int i = 0; i < content.childCount; i++)
                    if (content.GetChild(i).gameObject.activeSelf) n++;
                return n;
            }
        }

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;
            if (transform.parent != null) parentScroll = transform.parent.GetComponentInParent<ScrollRect>();
            BuildDots();
            UpdateDots();
        }

        public override void OnInitializePotentialDrag(PointerEventData e)
        {
            if (parentScroll != null) parentScroll.OnInitializePotentialDrag(e);
            base.OnInitializePotentialDrag(e);
        }

        // 마우스 휠은 배너가 아니라 바깥 세로 스크롤로 넘긴다.
        public override void OnScroll(PointerEventData e)
        {
            if (parentScroll != null) parentScroll.OnScroll(e);
        }

        public override void OnBeginDrag(PointerEventData e)
        {
            routeToParent = Mathf.Abs(e.delta.y) > Mathf.Abs(e.delta.x);
            if (routeToParent)
            {
                if (parentScroll != null) parentScroll.OnBeginDrag(e);
                return;
            }
            dragging = true;
            target = -1f;
            base.OnBeginDrag(e);
        }

        public override void OnDrag(PointerEventData e)
        {
            if (routeToParent)
            {
                if (parentScroll != null) parentScroll.OnDrag(e);
                return;
            }
            base.OnDrag(e);
        }

        public override void OnEndDrag(PointerEventData e)
        {
            if (routeToParent)
            {
                if (parentScroll != null) parentScroll.OnEndDrag(e);
                routeToParent = false;
                return;
            }
            base.OnEndDrag(e);
            dragging = false;

            int n = PageCount;
            if (n <= 1) return;
            float pos = horizontalNormalizedPosition * (n - 1);
            int p = Mathf.RoundToInt(pos);
            if (velocity.x < -300f) p = Mathf.FloorToInt(pos) + 1;      // 왼쪽으로 빠르게 밀면 다음
            else if (velocity.x > 300f) p = Mathf.CeilToInt(pos) - 1;   // 오른쪽으로 밀면 이전
            GoTo(p);
        }

        public void GoTo(int p)
        {
            int n = PageCount;
            if (n <= 0) return;
            page = Mathf.Clamp(p, 0, n - 1);
            target = n > 1 ? page / (float)(n - 1) : 0f;
            StopMovement();
            idle = 0f;
            UpdateDots();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (!Application.isPlaying) return;

            if (target >= 0f && !dragging)
            {
                float t = 1f - Mathf.Exp(-snapSpeed * Time.unscaledDeltaTime);
                float x = Mathf.Lerp(horizontalNormalizedPosition, target, t);
                if (Mathf.Abs(x - target) < 0.0005f) { x = target; target = -1f; }
                horizontalNormalizedPosition = x;
            }

            int n = PageCount;
            if (!dragging && autoInterval > 0f && n > 1)
            {
                idle += Time.unscaledDeltaTime;
                if (idle >= autoInterval) GoTo((page + 1) % n);
            }
        }

        void BuildDots()
        {
            if (dots == null) return;
            for (int i = dots.childCount - 1; i >= 0; i--) Destroy(dots.GetChild(i).gameObject);
            int n = PageCount;
            if (n <= 1) return;
            float size = UIKit.Px(7);
            for (int i = 0; i < n; i++)
            {
                var d = UIKit.Rect("Dot", dots);
                d.Layout(size, size);
                var img = d.gameObject.AddComponent<Image>();
                UIKit.SetRadius(img, size / 2f);
                img.raycastTarget = false;
            }
        }

        void UpdateDots()
        {
            if (dots == null) return;
            for (int i = 0; i < dots.childCount; i++)
            {
                var img = dots.GetChild(i).GetComponent<Image>();
                if (img != null) img.color = new Color(1f, 1f, 1f, i == page ? 0.95f : 0.45f);
            }
        }
    }
}
