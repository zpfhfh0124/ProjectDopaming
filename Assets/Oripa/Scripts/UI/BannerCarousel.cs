using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Oripa.UI
{
    /// <summary>
    /// 가로로 넘기는 배너. content 아래 켜져 있는 자식 하나가 한 페이지다 (페이지 폭 = 배너 폭).
    /// - 순환: 마지막 다음은 처음, 처음 이전은 마지막 (Play 때 양 끝에 복제 페이지를 하나씩 붙여서 이어 보이게 한다).
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

        const string CloneName = "LoopClone";

        ScrollRect parentScroll;
        bool routeToParent;
        bool dragging;
        bool loop;
        int realCount;   // 진짜 페이지 수 (복제 제외)
        int slot;        // content 안의 칸 번호 (순환이면 0과 마지막 칸은 복제)
        float target = -1f;
        float idle;
        bool pendingJump;

        int SlotCount
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

        int RealIndex(int s)
        {
            if (realCount <= 0) return 0;
            int r = loop ? s - 1 : s;
            return ((r % realCount) + realCount) % realCount;
        }

        float SlotPos(int s)
        {
            int m = SlotCount;
            return m > 1 ? s / (float)(m - 1) : 0f;
        }

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying) return;
            if (transform.parent != null) parentScroll = transform.parent.GetComponentInParent<ScrollRect>();
            SetupLoop();
            BuildDots();
            UpdateDots();
        }

        void SetupLoop()
        {
            if (content == null) return;
            var real = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < content.childCount; i++)
            {
                var c = content.GetChild(i);
                if (c.gameObject.activeSelf && c.name != CloneName) real.Add(c);
            }
            realCount = real.Count;
            loop = realCount > 1;
            if (loop)
            {
                var head = Instantiate(real[realCount - 1].gameObject, content);   // 맨 앞: 마지막 페이지 복제
                head.name = CloneName;
                head.transform.SetAsFirstSibling();
                var tail = Instantiate(real[0].gameObject, content);               // 맨 뒤: 첫 페이지 복제
                tail.name = CloneName;
                tail.transform.SetAsLastSibling();
            }
            Canvas.ForceUpdateCanvases();
            JumpTo(loop ? 1 : 0);
            pendingJump = true;   // 첫 프레임 레이아웃이 끝난 뒤 한 번 더 맞춘다
        }

        /// <summary>애니메이션 없이 바로 이동 (복제 칸에서 진짜 칸으로 몰래 옮길 때).</summary>
        void JumpTo(int s)
        {
            slot = s;
            target = -1f;
            StopMovement();
            horizontalNormalizedPosition = SlotPos(s);
        }

        /// <summary>복제 칸에 멈췄으면 같은 그림의 진짜 칸으로 옮긴다.</summary>
        void WrapIfOnClone()
        {
            if (!loop) return;
            if (slot <= 0) JumpTo(realCount);
            else if (slot >= realCount + 1) JumpTo(1);
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
            // 복제 칸으로 가는 중에 다시 잡으면 먼저 진짜 칸으로 옮겨 둔다 (끝에 막히지 않게)
            if (target >= 0f)
            {
                int m = SlotCount;
                int near = Mathf.RoundToInt(horizontalNormalizedPosition * (m - 1));
                if (loop && (near <= 0 || near >= realCount + 1))
                {
                    float offset = horizontalNormalizedPosition - SlotPos(near);
                    int dest = near <= 0 ? realCount : 1;
                    JumpTo(dest);
                    horizontalNormalizedPosition = SlotPos(dest) + offset;
                }
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

            int m = SlotCount;
            if (m <= 1) return;
            float pos = horizontalNormalizedPosition * (m - 1);
            int s = Mathf.RoundToInt(pos);
            if (velocity.x < -300f) s = Mathf.FloorToInt(pos) + 1;      // 왼쪽으로 빠르게 밀면 다음
            else if (velocity.x > 300f) s = Mathf.CeilToInt(pos) - 1;   // 오른쪽으로 밀면 이전
            GoToSlot(s);
        }

        /// <summary>진짜 페이지 번호(0부터)로 이동.</summary>
        public void GoTo(int realIndex)
        {
            if (realCount <= 0) return;
            int r = ((realIndex % realCount) + realCount) % realCount;
            GoToSlot(loop ? r + 1 : r);
        }

        void GoToSlot(int s)
        {
            int m = SlotCount;
            if (m <= 0) return;
            slot = Mathf.Clamp(s, 0, m - 1);
            target = SlotPos(slot);
            StopMovement();
            idle = 0f;
            UpdateDots();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (!Application.isPlaying) return;

            if (pendingJump && !dragging && target < 0f)
            {
                pendingJump = false;
                JumpTo(slot);
            }

            if (target >= 0f && !dragging)
            {
                float t = 1f - Mathf.Exp(-snapSpeed * Time.unscaledDeltaTime);
                float x = Mathf.Lerp(horizontalNormalizedPosition, target, t);
                if (Mathf.Abs(x - target) < 0.0005f)
                {
                    x = target;
                    target = -1f;
                }
                horizontalNormalizedPosition = x;
                if (target < 0f) WrapIfOnClone();
            }

            if (!dragging && autoInterval > 0f && realCount > 1)
            {
                idle += Time.unscaledDeltaTime;
                if (idle >= autoInterval) GoToSlot(loop ? slot + 1 : (slot + 1) % realCount);
            }
        }

        void BuildDots()
        {
            if (dots == null) return;
            for (int i = dots.childCount - 1; i >= 0; i--) Destroy(dots.GetChild(i).gameObject);
            if (realCount <= 1) return;
            float size = UIKit.Px(7);
            for (int i = 0; i < realCount; i++)
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
            int active = RealIndex(slot);
            for (int i = 0; i < dots.childCount; i++)
            {
                var img = dots.GetChild(i).GetComponent<Image>();
                if (img != null) img.color = new Color(1f, 1f, 1f, i == active ? 0.95f : 0.45f);
            }
        }
    }
}
