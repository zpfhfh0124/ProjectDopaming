using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Oripa.UI
{
    /// <summary>
    /// 페이지 세로 스크롤 안에 들어가는 가로 스크롤 줄 (탭 바 등).
    /// 가로로 끌면 이 줄이 움직이고, 세로로 끌거나 휠을 굴리면 바깥 세로 스크롤로 넘긴다.
    /// </summary>
    public class HorizontalStrip : ScrollRect
    {
        ScrollRect parentScroll;
        bool routeToParent;

        protected override void Start()
        {
            base.Start();
            if (transform.parent != null) parentScroll = transform.parent.GetComponentInParent<ScrollRect>();
        }

        public override void OnInitializePotentialDrag(PointerEventData e)
        {
            if (parentScroll != null) parentScroll.OnInitializePotentialDrag(e);
            base.OnInitializePotentialDrag(e);
        }

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
        }
    }
}
