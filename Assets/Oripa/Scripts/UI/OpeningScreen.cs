using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Oripa.UI
{
    /// <summary>
    /// 뽑기 결과 모달 (HTML의 "뽑기 결과" + .res 그리드).
    /// 카드는 HTML의 @keyframes flip (rotateY 90° → 0°, 투명 → 불투명, 0.5초) 처럼 뒤집히며 나타난다.
    /// Unity 버전에서는 한 장씩 순서대로 나오고, S상은 빛·흔들림·섬광이 더해진다.
    /// </summary>
    public sealed class OpeningScreen : MonoBehaviour
    {
        const int Columns = 3;           // minmax(96px,1fr) → 모달 폭에서 3열
        const float CardHeight = 250f;   // min-height 96px
        const float FlipTime = 0.5f;
        const float Stagger = 0.12f;

        RectTransform panel;
        Image flash;

        public static void Show(GachaApp app, List<PrizeCard> results)
        {
            var root = app.OpenModal(out var panel, "뽑기 결과");
            var view = root.gameObject.AddComponent<OpeningScreen>();
            view.panel = panel;
            view.Build(app, root, results);
        }

        void Build(GachaApp app, RectTransform root, List<PrizeCard> results)
        {
            float innerWidth = GachaApp.ModalWidth - UIKit.Px(18) * 2;
            float gap = UIKit.Px(8);

            var gridRt = UIKit.Rect("Results", panel);
            var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2((innerWidth - gap * (Columns - 1)) / Columns, CardHeight);
            grid.spacing = new Vector2(gap, gap);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;

            var views = new List<CardView>();
            foreach (var card in results)
            {
                // 그리드가 셀 위치를 잡고, 안쪽 Holder를 회전·투명도로 연출한다.
                var cell = UIKit.Rect("Cell", gridRt);
                var cv = CardView.Create(cell, card, false);
                cv.Rect.Stretch();
                var group = cv.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                cv.Rect.localRotation = Quaternion.Euler(0, 90, 0);
                views.Add(cv);
            }

            app.ModalButton(panel, root, "닫기", null);

            flash = UIKit.Panel(root, "Flash", Color.clear);
            flash.raycastTarget = false;

            StartCoroutine(RevealAll(views));
        }

        IEnumerator RevealAll(List<CardView> views)
        {
            yield return null; // 레이아웃이 한 번 계산된 뒤 시작
            for (int i = 0; i < views.Count; i++)
            {
                var cv = views[i];
                if (cv.Card.grade == Grade.S)
                {
                    // S상은 앞 카드가 다 나온 뒤 잠깐 뜸을 들이고 등장
                    yield return Wait(FlipTime * 0.6f);
                    yield return Flip(cv);
                    cv.EnableGlow();
                    StartCoroutine(Shake(UIKit.Px(6), 0.35f));
                    StartCoroutine(Flash(Theme.GradeS2, 0.06f, 0.45f));
                    yield return Wait(0.25f);
                }
                else
                {
                    StartCoroutine(Flip(cv));
                    yield return Wait(Stagger);
                }
            }
        }

        static IEnumerator Flip(CardView cv)
        {
            var group = cv.GetComponent<CanvasGroup>();
            for (float e = 0; e < FlipTime; e += Time.unscaledDeltaTime)
            {
                float k = UIKit.EaseOutCubic(e / FlipTime);
                cv.Rect.localRotation = Quaternion.Euler(0, 90f * (1f - k), 0);
                group.alpha = k;
                yield return null;
            }
            cv.Rect.localRotation = Quaternion.identity;
            group.alpha = 1f;
        }

        IEnumerator Shake(float amplitude, float duration)
        {
            Vector2 basePos = panel.anchoredPosition;
            for (float e = 0; e < duration; e += Time.unscaledDeltaTime)
            {
                panel.anchoredPosition = basePos + Random.insideUnitCircle * amplitude * (1f - e / duration);
                yield return null;
            }
            panel.anchoredPosition = basePos;
        }

        IEnumerator Flash(Color color, float fadeIn, float fadeOut)
        {
            for (float e = 0; e < fadeIn; e += Time.unscaledDeltaTime)
            {
                flash.color = color.WithAlpha(e / fadeIn * 0.45f);
                yield return null;
            }
            for (float e = 0; e < fadeOut; e += Time.unscaledDeltaTime)
            {
                flash.color = color.WithAlpha(0.45f * (1f - e / fadeOut));
                yield return null;
            }
            flash.color = Color.clear;
        }

        static IEnumerator Wait(float seconds)
        {
            for (float e = 0; e < seconds; e += Time.unscaledDeltaTime) yield return null;
        }
    }
}
