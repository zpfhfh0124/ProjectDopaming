using UnityEngine;
using UnityEngine.UI;

namespace Oripa.UI
{
    /// <summary>보유 카드 목록 (HTML의 #inv). 최근에 뽑은 카드가 앞에 온다.</summary>
    public static class CollectionScreen
    {
        public const float CardHeight = 320f;

        public static void Build(Transform parent, GachaApp app, float width)
        {
            var prizes = app.State.prizes;
            if (prizes.Count == 0)
            {
                // .empty : 점선 테두리 상자 (점선 대신 실선)
                var box = UIKit.BorderBox(parent, "Empty", Theme.Bg, Theme.Border, UIKit.Px(10));
                var outer = box.transform.parent;
                outer.GetComponent<Image>().Layout(UIKit.Px(70));
                UIKit.Label(box.transform, "아직 뽑은 카드가 없어요. 위에서 팩을 골라 뽑아 보세요.", UIKit.Fs(15), Theme.Muted)
                    .rectTransform.Stretch(UIKit.Px(14), 0, UIKit.Px(14), 0);
                return;
            }

            var gridRt = UIKit.Rect("Inventory", parent);
            var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
            const int columns = 2; // minmax(150px,1fr) → 휴대폰 폭에서 2열
            float gap = UIKit.Px(10);
            grid.cellSize = new Vector2((width - gap * (columns - 1)) / columns, CardHeight);
            grid.spacing = new Vector2(gap, gap);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;

            for (int i = prizes.Count - 1; i >= 0; i--)
                CardView.Create(gridRt, prizes[i], true, app.SellCard);
        }
    }
}
