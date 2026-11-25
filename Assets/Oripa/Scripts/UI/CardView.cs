using System;
using UnityEngine;
using UnityEngine.UI;

namespace Oripa.UI
{
    /// <summary>
    /// 카드 한 장 (HTML의 .card). 등급색 배경 위에 팩 이름, "S상", 포인트, 전환 버튼.
    /// S상은 3색 그라데이션 (linear-gradient 135deg #e8a200 → #ff6b9d → #6b8cff).
    /// </summary>
    public sealed class CardView : MonoBehaviour
    {
        public PrizeCard Card { get; private set; }
        public RectTransform Rect { get; private set; }
        Image glow;

        /// <summary>
        /// withValue: 포인트 가치와 "포인트로 전환" 버튼까지 보여 준다 (보유 카드 목록용).
        /// </summary>
        public static CardView Create(Transform parent, PrizeCard card, bool withValue, Action<PrizeCard> onSell = null)
        {
            var rt = UIKit.Rect("Card_" + card.uid, parent);
            var view = rt.gameObject.AddComponent<CardView>();
            view.Rect = rt;
            view.Card = card;
            view.Build(withValue, onSell);
            return view;
        }

        void Build(bool withValue, Action<PrizeCard> onSell)
        {
            float pad = UIKit.Px(10);
            var bg = UIKit.Panel(transform, "Bg", Theme.GradeColor(Card.grade), UIKit.Px(10));
            if (Card.grade == Grade.S)
            {
                var grad = bg.gameObject.AddComponent<DiagonalGradient>();
                grad.A = Theme.GradeS1;
                grad.B = Theme.GradeS2;
                grad.C = Theme.GradeS3;
            }

            // 위: 팩 이름
            var pack = UIKit.Label(bg.transform, Card.packName, UIKit.Fs(13), Color.white, TextAnchor.UpperLeft);
            pack.rectTransform.Stretch(pad, 0, pad, pad);
            pack.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 등급 (b, 22px)
            var grade = UIKit.Label(bg.transform, GradeInfo.Label(Card.grade), UIKit.Fs(22), Color.white,
                withValue ? TextAnchor.UpperLeft : TextAnchor.LowerLeft, FontStyle.Bold);
            if (withValue) grade.rectTransform.Stretch(pad, 0, pad, pad + UIKit.Px(22));
            else grade.rectTransform.Stretch(pad, pad, pad, 0);

            if (!withValue) return;

            var value = UIKit.Label(bg.transform, UIKit.Num(Card.value) + "pt", UIKit.Fs(13), Color.white, TextAnchor.UpperLeft);
            value.rectTransform.Stretch(pad, 0, pad, pad + UIKit.Px(56));

            var sell = UIKit.MakeButton(bg.transform, "포인트로 전환", new Color(0, 0, 0, 0.28f), Color.white, Color.clear,
                () => onSell?.Invoke(Card), UIKit.Fs(12));
            var srt = (RectTransform)sell.transform;
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(1, 0);
            srt.pivot = new Vector2(0.5f, 0);
            srt.offsetMin = new Vector2(pad, pad);
            srt.offsetMax = new Vector2(-pad, pad + UIKit.Px(28));
        }

        /// <summary>S상 카드 뒤에 은은하게 빛나는 테두리.</summary>
        public void EnableGlow()
        {
            if (glow != null) return;
            glow = UIKit.GlowImage(transform, Theme.GradeS2.WithAlpha(0.8f), UIKit.Px(16));
        }

        void Update()
        {
            if (glow == null) return;
            float t = Time.unscaledTime;
            var c = Color.HSVToRGB(Mathf.Repeat(t * 0.3f, 1f), 0.55f, 1f);
            glow.color = c.WithAlpha(0.55f + 0.25f * Mathf.Sin(t * 4f));
        }
    }
}
