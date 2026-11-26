using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Oripa.UI
{
    /// <summary>스크롤 밖으로 옮겨진 고정 줄 위에서 끌거나 휠을 굴려도 페이지가 스크롤되게 넘겨 준다.</summary>
    public class DragForwarder : MonoBehaviour,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public ScrollRect target;

        public void OnInitializePotentialDrag(PointerEventData e) { if (target != null) target.OnInitializePotentialDrag(e); }
        public void OnBeginDrag(PointerEventData e) { if (target != null) target.OnBeginDrag(e); }
        public void OnDrag(PointerEventData e) { if (target != null) target.OnDrag(e); }
        public void OnEndDrag(PointerEventData e) { if (target != null) target.OnEndDrag(e); }
        public void OnScroll(PointerEventData e) { if (target != null) target.OnScroll(e); }
    }
}
