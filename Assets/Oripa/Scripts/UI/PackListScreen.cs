using UnityEngine;
using UnityEngine.UI;

namespace Oripa.UI
{
    /// <summary>팩 카드 목록 (HTML의 .grid > .pack).</summary>
    public static class PackListScreen
    {
        /// <summary>HTML의 .art.im aspect-ratio:1402/1122</summary>
        const float ArtAspect = 1402f / 1122f;

        public static void Build(Transform parent, GachaApp app, float width)
        {
            foreach (var pack in app.Catalog.Packs)
                BuildPack(parent, app, pack, width);
        }

        static void BuildPack(Transform parent, GachaApp app, PackDefinition pack, float width)
        {
            var state = app.State;
            var ps = state.GetPack(pack);
            float border = 3f;

            // .pack : 흰 면 + 1px 테두리 + 둥근 14px + overflow:hidden
            var outer = UIKit.Panel(parent, "Pack_" + pack.Id, Theme.Border, UIKit.Px(14));
            var inner = UIKit.Panel(outer.transform, "Fill", Theme.Surface, UIKit.Px(14) - border);
            inner.rectTransform.Stretch(border, border, border, border);
            inner.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            UIKit.Vertical(inner.gameObject, new RectOffset(0, 0, 0, 0), 0);
            // 바깥 상자 높이는 안쪽 내용 + 테두리
            UIKit.Vertical(outer.gameObject, UIKit.Pad(border), 0);

            // .art
            var tex = UIKit.LoadPackArt(pack.Art);
            float innerWidth = width - border * 2;
            if (tex != null)
            {
                var artRt = UIKit.Rect("Art", inner.transform);
                artRt.Layout(innerWidth / ArtAspect);
                var raw = artRt.gameObject.AddComponent<RawImage>();
                raw.texture = tex;
                raw.raycastTarget = false;
            }
            else
            {
                var art = UIKit.Panel(inner.transform, "Art", Theme.Hex(pack.ThemeColor));
                art.Layout(UIKit.Px(120));
                art.raycastTarget = false;
                UIKit.Label(art.transform, pack.Category, UIKit.Fs(34), Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            }

            // .pb
            var pb = UIKit.Rect("Body", inner.transform);
            UIKit.Vertical(pb.gameObject, UIKit.Pad(UIKit.Px(12), UIKit.Px(14)), UIKit.Px(8));

            UIKit.Label(pb, pack.Name, UIKit.Fs(16), Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold).Layout(UIKit.Px(26));

            MetaRow(pb, $"1회 {UIKit.Num(pack.Price)}pt", $"환원율 {GachaEngine.Rtp(pack, state):0}%");

            // .bar
            var bar = UIKit.Panel(pb, "Bar", Theme.Border, UIKit.Px(3));
            bar.Layout(UIKit.Px(6));
            var fill = UIKit.Panel(bar.transform, "Fill", Theme.Accent, UIKit.Px(3));
            float ratio = pack.Total > 0 ? (float)ps.left / pack.Total : 0f;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1);
            fill.enabled = ratio > 0f;

            MetaRow(pb, "남은 수량", $"{ps.left} / {pack.Total}");

            // .row 버튼 3개
            var row = UIKit.Rect("Row", pb);
            row.Layout(UIKit.Px(40));
            UIKit.Horizontal(row.gameObject, new RectOffset(0, 0, 0, 0), UIKit.Px(8));

            var one = UIKit.Button(row, "1회", () => app.TryDraw(pack, 1));
            one.SetEnabled(GachaEngine.CanDraw(pack, state, 1));
            var ten = UIKit.Button(row, "10회", () => app.TryDraw(pack, 10));
            ten.SetEnabled(GachaEngine.CanDraw(pack, state, 10));
            UIKit.GhostButton(row, "확률", Theme.Surface, () => app.ShowOdds(pack));

            if (ps.left <= 0)
            {
                var re = UIKit.GhostButton(pb, "재입고 (테스트)", Theme.Surface, () =>
                {
                    state.Restock(pack);
                    app.SaveAndRender();
                });
                re.Layout(UIKit.Px(40));
            }
        }

        static void MetaRow(Transform parent, string left, string right)
        {
            var row = UIKit.Rect("Meta", parent);
            row.Layout(UIKit.Px(20));
            UIKit.Label(row, left, UIKit.Fs(13), Theme.Muted, TextAnchor.MiddleLeft);
            UIKit.Label(row, right, UIKit.Fs(13), Theme.Muted, TextAnchor.MiddleRight);
        }
    }
}
