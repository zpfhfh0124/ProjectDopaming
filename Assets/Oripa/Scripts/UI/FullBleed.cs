using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Oripa.UI
{
    /// <summary>
    /// 세로 레이아웃 안에 있어도 좌우 여백을 무시하고 부모 폭 전체(화면 양 끝)까지 늘린다.
    /// 부모 레이아웃 그룹이 위치를 정한 뒤에 폭만 다시 잡는다 (높이와 세로 위치는 그대로).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class FullBleed : UIBehaviour, ILayoutSelfController
    {
        DrivenRectTransformTracker tracker;

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            tracker.Clear();
            SetDirty();
            base.OnDisable();
        }

        public void SetLayoutHorizontal()
        {
            var rt = (RectTransform)transform;
            var parent = rt.parent as RectTransform;
            if (parent == null) return;
            tracker.Clear();
            tracker.Add(this, rt, DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.SizeDeltaX |
                                  DrivenTransformProperties.AnchorMinX | DrivenTransformProperties.AnchorMaxX);
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, parent.rect.width);
        }

        public void SetLayoutVertical() { }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif

        void SetDirty()
        {
            if (!IsActive()) return;
            LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }
    }
}
