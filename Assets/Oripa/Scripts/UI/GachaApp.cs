using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Oripa.UI
{
    /// <summary>
    /// 앱 진입점. 화면 구성은 HTML 프로토타입(카드 뽑기 프로토타입.html)을 따른다:
    /// 헤더(로고, 포인트, 충전) → 안내 → 팩 목록 → 보유 카드 → 푸터, 그리고 결과/확률 모달.
    ///
    /// 두 가지 방식으로 동작한다.
    /// 1) 코드 생성 (기본): 화면 전체를 코드로 만든다. 에디터에서는 저장되지 않는 미리보기(DontSave)로 보여 준다.
    /// 2) 구운 UI (메뉴 Oripa > UI를 씬에 굽기): 고정된 화면(헤더, 포인트 줄, 안내, 제목, 푸터, 배경)을
    ///    씬에 저장되는 진짜 오브젝트로 만든다. Scene 뷰에서 위치, 크기, 색, 글자를 직접 고칠 수 있다.
    ///    팩 목록과 보유 카드는 데이터에 따라 바뀌므로 PackListSlot / CollectionSlot 안에 실행할 때 코드로 채운다.
    /// </summary>
    [ExecuteAlways]
    public sealed class GachaApp : MonoBehaviour
    {
        /// <summary>.w { max-width:960px; padding:16px } 를 1080 폭 세로 화면에 맞춘 값.</summary>
        public const float PageWidth = 1080f;
        public static float PagePadding => UIKit.Px(16);
        public static float ContentWidth => PageWidth - PagePadding * 2;
        /// <summary>.md { max-width:560px; width:100% } → 화면 폭에서 좌우 16px 뺀 값.</summary>
        public static float ModalWidth => PageWidth - UIKit.Px(16) * 2;

        const string ThemePrefKey = "oripa.theme.dark";
        const string PreviewName = "GachaCanvas (미리보기)";
        const string BakedName = "GachaCanvas";

        public GachaCatalog Catalog { get; private set; }
        public PlayerState State { get; private set; }
        public GachaEngine Engine { get; private set; }

        [Tooltip("Play 전에도 Scene/Game 뷰에 UI를 미리 보여 준다.")]
        [SerializeField] bool previewInEditMode = true;

        [Header("구운 UI (Oripa > UI를 씬에 굽기 로 자동 연결)")]
        [Tooltip("켜져 있으면 씬에 저장된 UI를 쓰고, 꺼져 있으면 화면 전체를 코드로 만든다.")]
        [SerializeField] bool baked;
        [SerializeField] RectTransform canvasRoot;
        [SerializeField] RectTransform modalLayer;
        [Tooltip("팩 카드가 실행 중에 이 안에 만들어진다. 직접 넣은 자식은 Play 때 지워진다.")]
        [SerializeField] RectTransform packListSlot;
        [Tooltip("보유 카드가 실행 중에 이 안에 만들어진다. 직접 넣은 자식은 Play 때 지워진다.")]
        [SerializeField] RectTransform collectionSlot;
        [SerializeField] Text pointsText;
        [SerializeField] Text collectionTitle;
        [SerializeField] Button chargeButton;
        [SerializeField] Button resetButton;
        [SerializeField] Button themeButton;

        public bool IsBaked => baked;
        bool LayoutReady => canvasRoot != null && packListSlot != null && collectionSlot != null;

        Coroutine toastRoutine;

        void Awake()
        {
            if (!Application.isPlaying) return; // 에디터 미리보기는 OnEnable에서 따로 만든다.

            Application.targetFrameRate = 60;
            LoadData();
            EnsureEventSystem();

            if (baked)
            {
                if (!LayoutReady)
                {
                    Debug.LogError("[Oripa] 구운 UI의 연결이 끊겼습니다. 메뉴 Oripa > UI를 씬에 굽기 를 다시 실행하세요. 이번에는 코드로 화면을 만듭니다.");
                    baked = false;
                }
                else
                {
                    UIKit.ApplyMissingFonts(canvasRoot);
                    WireButtons();
                    Render();
                    return;
                }
            }

            DestroyPreview();
            BuildCanvas(false);
            WireButtons();
            Render();
        }

        void LoadData()
        {
            Theme.Apply(PlayerPrefs.GetInt(ThemePrefKey, 0) == 1);
            Catalog = GachaCatalog.CreateDefault();
            State = SaveSystem.Load();
            Engine = new GachaEngine();
        }

        void WireButtons()
        {
            if (chargeButton != null) chargeButton.onClick.AddListener(Charge);
            if (resetButton != null) resetButton.onClick.AddListener(ConfirmReset);
            if (themeButton != null) themeButton.onClick.AddListener(ToggleTheme);
        }

        // ───────────────────────── 에디터 미리보기 (Play 전)

        void OnEnable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;
            // OnEnable 도중(스크립트 리로드, 씬 열기)에는 에셋 로드가 불안정할 수 있어 한 프레임 뒤에 만든다.
            UnityEditor.EditorApplication.delayCall += DelayedPreview;
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;
            UnityEditor.EditorApplication.delayCall -= DelayedPreview;
            DestroyPreview();
#endif
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // 인스펙터에서 미리보기 체크를 바꿨을 때. OnValidate 안에서는 오브젝트를 지울 수 없어서 미룬다.
            if (Application.isPlaying) return;
            UnityEditor.EditorApplication.delayCall -= DelayedPreview;
            UnityEditor.EditorApplication.delayCall += DelayedPreview;
        }

        void DelayedPreview()
        {
            if (this == null || Application.isPlaying) return; // 그 사이 삭제되었거나 Play에 들어간 경우
            RefreshPreview();
        }

        /// <summary>
        /// 에디터 전용: 고정 화면을 씬에 저장되는 오브젝트로 만들고 이 컴포넌트에 연결한다.
        /// 이미 구운 UI가 있으면 호출하는 쪽(OripaEditorMenu)이 먼저 지운다.
        /// 반환값은 새로 만든 캔버스 (Undo 등록용).
        /// </summary>
        public GameObject BakeLayout()
        {
            DestroyPreview();
            LoadData();
            baked = true;
            BuildCanvas(true);
            UIKit.ApplyMissingFonts(canvasRoot);
            return canvasRoot.gameObject;
        }

        /// <summary>에디터 전용: 구운 UI 연결을 끊는다 (캔버스 삭제는 호출하는 쪽이 Undo로 처리).</summary>
        public void ClearBakedReferences()
        {
            baked = false;
            canvasRoot = modalLayer = packListSlot = collectionSlot = null;
            pointsText = collectionTitle = null;
            chargeButton = resetButton = themeButton = null;
        }

        public RectTransform BakedCanvas => baked ? canvasRoot : null;
#endif

        /// <summary>에디터에서 미리보기를 지금 상태(저장 데이터, 테마)로 다시 만든다.</summary>
        [ContextMenu("미리보기 새로고침")]
        public void RefreshPreview()
        {
            if (Application.isPlaying) return;
            DestroyPreview();
            if (!previewInEditMode || !isActiveAndEnabled) return;

            LoadData();
            if (baked)
            {
                if (!LayoutReady) return;
                // 구운 UI: 저장된 오브젝트는 그대로 두고, 팩/보유 카드만 저장되지 않는 미리보기로 채운다.
                UIKit.ApplyMissingFonts(canvasRoot);
                // 슬롯에 직접 넣어 둔 오브젝트가 있으면 그것은 건드리지 않고, 새로 만든 미리보기만 저장 제외로 표시한다.
                int packBefore = packListSlot.childCount, collectionBefore = collectionSlot.childCount;
                Render();
                MarkNewChildrenDontSave(packListSlot, packBefore);
                MarkNewChildrenDontSave(collectionSlot, collectionBefore);
                return;
            }

            // EventSystem은 만들지 않는다 (씬에 저장되면 안 되고, 에디터에서는 클릭할 일이 없다).
            BuildCanvas(false);
            canvasRoot.name = PreviewName;
            Render();
            MarkDontSave(canvasRoot);
        }

        static void MarkDontSave(Transform t)
        {
            t.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            for (int i = 0; i < t.childCount; i++) MarkDontSave(t.GetChild(i));
        }

        static void MarkNewChildrenDontSave(Transform t, int from)
        {
            for (int i = from; i < t.childCount; i++) MarkDontSave(t.GetChild(i));
        }

        void DestroyPreview()
        {
            if (baked)
            {
                // 구운 UI에서는 슬롯 안의 미리보기 카드만 지운다.
                if (!Application.isPlaying)
                {
                    ClearSlot(packListSlot);
                    ClearSlot(collectionSlot);
                }
                return;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (child.name != PreviewName) continue;
                if (Application.isPlaying) { child.SetActive(false); Destroy(child); }
                else DestroyImmediate(child);
            }
            if (!Application.isPlaying)
            {
                canvasRoot = modalLayer = packListSlot = collectionSlot = null;
                pointsText = collectionTitle = null;
                chargeButton = resetButton = themeButton = null;
            }
        }

        /// <summary>
        /// 슬롯 비우기. Play 중에는 전부 지운다.
        /// 에디터에서는 미리보기(DontSave)로 만든 것만 지워서, 실수로 넣은 오브젝트가 사라지지 않게 한다.
        /// </summary>
        static void ClearSlot(Transform slot)
        {
            if (slot == null) return;
            if (Application.isPlaying)
            {
                UIKit.ClearChildren(slot);
                return;
            }
            for (int i = slot.childCount - 1; i >= 0; i--)
            {
                var child = slot.GetChild(i).gameObject;
                if ((child.hideFlags & HideFlags.DontSave) != 0) DestroyImmediate(child);
            }
        }

        // ───────────────────────── 동작

        public void TryDraw(PackDefinition pack, int count)
        {
            var status = Engine.TryDraw(pack, State, count, out var results);
            if (status == DrawStatus.NotEnoughPoints)
            {
                Toast("포인트가 부족해요. 위의 충전 버튼을 눌러 주세요.");
                return;
            }
            if (status == DrawStatus.SoldOut)
            {
                Toast("남은 수량이 부족해요.");
                return;
            }
            // 결과를 먼저 저장한 뒤 연출을 보여 준다.
            SaveAndRender();
            OpeningScreen.Show(this, results);
        }

        public void SellCard(PrizeCard card)
        {
            if (GachaEngine.Sell(State, card.uid)) SaveAndRender();
        }

        public void Charge()
        {
            State.points += 1_000_000;
            SaveAndRender();
        }

        public void SaveAndRender()
        {
            SaveSystem.Save(State);
            Render();
        }

        void ToggleTheme()
        {
            // 테마 버튼은 코드 생성 방식에만 있다 (구운 UI의 색은 Scene에서 직접 정한다).
            if (baked) return;
            Theme.Apply(!Theme.Dark);
            PlayerPrefs.SetInt(ThemePrefKey, Theme.Dark ? 1 : 0);
            PlayerPrefs.Save();
            var old = canvasRoot.gameObject;
            old.SetActive(false);
            Destroy(old);
            BuildCanvas(false);
            WireButtons();
            Render();
        }

        void ConfirmReset()
        {
            var root = OpenModal(out var panel, "처음부터 다시 시작할까요?");
            ModalText(panel, "포인트, 보유 카드, 남은 수량이 모두 처음 상태로 돌아가요.");
            var row = UIKit.Rect("Row", panel);
            row.Layout(UIKit.Px(40));
            UIKit.Horizontal(row.gameObject, new RectOffset(0, 0, 0, 0), UIKit.Px(8));
            UIKit.Button(row, "초기화", () =>
            {
                Destroy(root.gameObject);
                State = new PlayerState();
                SaveAndRender();
                Toast("초기화했어요.");
            });
            UIKit.GhostButton(row, "취소", Theme.Surface, () => Destroy(root.gameObject));
        }

        // ───────────────────────── 화면

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>
        /// 캔버스와 고정 화면(헤더, 포인트 줄, 안내, 제목, 푸터)을 만들고 참조를 연결한다.
        /// 버튼 동작은 WireButtons()에서 연결한다 (씬에 저장된 UI도 같은 방식).
        /// forBake: 씬에 굽는 경우. 테마 전환 버튼은 넣지 않는다.
        /// </summary>
        void BuildCanvas(bool forBake)
        {
            var canvasGo = new GameObject(BakedName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(PageWidth, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasRoot = (RectTransform)canvasGo.transform;

            UIKit.Panel(canvasRoot, "Background", Theme.Bg);

            // 넓은 화면에서도 가운데 1080 폭 기둥 (.w { margin:0 auto })
            var column = UIKit.Rect("Column", canvasRoot);
            column.anchorMin = new Vector2(0.5f, 0);
            column.anchorMax = new Vector2(0.5f, 1);
            column.sizeDelta = new Vector2(PageWidth, 0);
            column.anchoredPosition = Vector2.zero;

            UIKit.VerticalScroll(column, out var page);
            UIKit.Vertical(page.gameObject, UIKit.Pad(PagePadding), UIKit.Px(14));

            modalLayer = UIKit.Rect("Modals", canvasRoot).Stretch();

            BuildHeader(page, !forBake);

            // .note
            var note = UIKit.BorderBox(page, "Note", Theme.Bg, Theme.Border, UIKit.Px(8));
            var noteOuter = note.transform.parent.GetComponent<Image>();
            UIKit.Vertical(noteOuter.gameObject, UIKit.Pad(3), 0);
            UIKit.Vertical(note.gameObject, UIKit.Pad(UIKit.Px(10), UIKit.Px(14)), 0);
            UIKit.Label(note.transform,
                "UI 프로토타입이에요. 실제 결제, 뽑기, 배송은 없고 포인트와 카드는 모두 가짜 데이터예요.",
                UIKit.Fs(13), Theme.Muted);

            // 팩 목록 자리 (내용은 Render에서 채운다)
            packListSlot = UIKit.Rect("PackListSlot", page);
            UIKit.Vertical(packListSlot.gameObject, new RectOffset(0, 0, 0, 0), UIKit.Px(14));

            // h2 보유 카드 (margin-top 28px)
            Spacer(page, UIKit.Px(14));
            collectionTitle = UIKit.Label(page, "보유 카드", UIKit.Fs(17), Theme.Text, TextAnchor.LowerLeft, FontStyle.Bold);
            collectionTitle.name = "CollectionTitle";
            collectionTitle.Layout(UIKit.Px(30));

            // 보유 카드 자리
            collectionSlot = UIKit.Rect("CollectionSlot", page);
            UIKit.Vertical(collectionSlot.gameObject, new RectOffset(0, 0, 0, 0), 0);

            // footer
            Spacer(page, UIKit.Px(14));
            UIKit.Label(page,
                "환원율은 서비스 내 포인트 기준 계산값이에요. 실제 서비스로 만들기 전에 사행성 관련 법률 검토가 필요해요. " +
                "만 19세 미만 이용 제한과 확률 공개 문구도 이 화면에 추가해야 해요.",
                UIKit.Fs(12), Theme.Muted).name = "Footer";

            resetButton = UIKit.GhostButton(page, "데이터 초기화 (테스트)", Theme.Bg, null, UIKit.Fs(13));
            resetButton.name = "ResetButton";
            resetButton.Layout(UIKit.Px(36));
            Spacer(page, UIKit.Px(24));
        }

        /// <summary>HTML의 render(): 데이터에 따라 바뀌는 부분(포인트, 팩 목록, 보유 카드)만 다시 그린다.</summary>
        public void Render()
        {
            if (!LayoutReady) return;

            if (pointsText != null) pointsText.text = UIKit.Num(State.points) + " pt";
            if (collectionTitle != null) collectionTitle.text = $"보유 카드  ({State.prizes.Count}장)";

            ClearSlot(packListSlot);
            PackListScreen.Build(packListSlot, this, ContentWidth);

            ClearSlot(collectionSlot);
            CollectionScreen.Build(collectionSlot, this, ContentWidth);
        }

        void BuildHeader(Transform page, bool withThemeButton)
        {
            // 로고 + 테마 전환
            var top = UIKit.Rect("Header", page);
            top.Layout(UIKit.Px(56));
            var logoTex = UIKit.LoadTexture("Branding/Logo");
            if (logoTex != null)
            {
                var logo = UIKit.Rect("Logo", top);
                logo.anchorMin = logo.anchorMax = new Vector2(0, 0.5f);
                logo.pivot = new Vector2(0, 0.5f);
                logo.sizeDelta = new Vector2(UIKit.Px(56), UIKit.Px(56));
                logo.anchoredPosition = Vector2.zero;
                var raw = logo.gameObject.AddComponent<RawImage>();
                raw.texture = logoTex;
                raw.raycastTarget = false;
            }

            // 로고 오른쪽에 "도파냥" 글자 로고 (원본 비율 유지: 높이를 바꾸면 폭이 따라간다)
            var wordTex = UIKit.LoadTexture("Branding/Wordmark");
            if (wordTex != null)
            {
                float h = UIKit.Px(48);
                float aspect = (float)wordTex.width / wordTex.height;
                var word = UIKit.Rect("Wordmark", top);
                word.anchorMin = word.anchorMax = new Vector2(0, 0.5f);
                word.pivot = new Vector2(0, 0.5f);
                word.sizeDelta = new Vector2(h * aspect, h);
                word.anchoredPosition = new Vector2(logoTex != null ? UIKit.Px(56 + 8) : 0, 0);
                var wraw = word.gameObject.AddComponent<RawImage>();
                wraw.texture = wordTex;
                wraw.raycastTarget = false;
                var fitter = word.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                fitter.aspectRatio = aspect;
            }

            if (logoTex == null && wordTex == null)
            {
                // 로고 이미지가 둘 다 없을 때만 글자로 대신한다.
                UIKit.Label(top, "카드 뽑기", UIKit.Fs(22), Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            }

            themeButton = null;
            if (withThemeButton)
            {
                themeButton = UIKit.GhostButton(top, Theme.Dark ? "라이트" : "다크", Theme.Bg, null, UIKit.Fs(13));
                themeButton.name = "ThemeButton";
                var trt = (RectTransform)themeButton.transform;
                trt.anchorMin = trt.anchorMax = new Vector2(1, 0.5f);
                trt.pivot = new Vector2(1, 0.5f);
                trt.sizeDelta = new Vector2(UIKit.Px(64), UIKit.Px(34));
                trt.anchoredPosition = Vector2.zero;
            }

            // .bal : 둥근 알약 모양, 포인트 + 충전 버튼
            var balRow = UIKit.Rect("BalanceRow", page);
            balRow.Layout(UIKit.Px(52));
            var bal = UIKit.BorderBox(balRow, "Balance", Theme.Surface, Theme.Border, UIKit.Px(26));
            var balOuter = (RectTransform)bal.transform.parent;
            balOuter.anchorMin = new Vector2(0, 0);
            balOuter.anchorMax = new Vector2(0, 1);
            balOuter.pivot = new Vector2(0, 0.5f);
            balOuter.offsetMin = Vector2.zero;
            balOuter.offsetMax = new Vector2(UIKit.Px(370), 0);

            pointsText = UIKit.Label(bal.transform, "0 pt", UIKit.Fs(15), Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            pointsText.name = "PointsText";
            pointsText.rectTransform.Stretch(UIKit.Px(14), 0, UIKit.Px(210), 0);

            chargeButton = UIKit.Button(bal.transform, "+1,000,000pt 충전(데모)", null, UIKit.Fs(14));
            chargeButton.name = "ChargeButton";
            var crt = (RectTransform)chargeButton.transform;
            crt.anchorMin = new Vector2(1, 0);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(1, 0.5f);
            crt.offsetMin = new Vector2(-UIKit.Px(204), UIKit.Px(6));
            crt.offsetMax = new Vector2(-UIKit.Px(6), -UIKit.Px(6));
        }

        static void Spacer(Transform parent, float height) => UIKit.Rect("Spacer", parent).Layout(height);

        // ───────────────────────── 모달 (.ov / .md)

        /// <summary>모달을 연다. 바깥(어두운 영역)을 누르면 닫힌다. panel에 내용을 세로로 쌓는다.</summary>
        public RectTransform OpenModal(out RectTransform panel, string title)
        {
            var root = UIKit.Rect("Modal", modalLayer).Stretch();
            var ov = UIKit.Panel(root, "Overlay", Theme.Overlay);
            var close = ov.gameObject.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(() => Destroy(root.gameObject));

            var md = UIKit.Panel(root, "Panel", Theme.Surface, UIKit.Px(14));
            panel = md.rectTransform;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(ModalWidth, 0);
            UIKit.Vertical(md.gameObject, UIKit.Pad(UIKit.Px(18)), UIKit.Px(10));
            md.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (!string.IsNullOrEmpty(title))
                UIKit.Label(panel, title, UIKit.Fs(18), Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold).Layout(UIKit.Px(32));
            return root;
        }

        public Text ModalText(RectTransform panel, string text, Color? color = null, int size = 0)
        {
            return UIKit.Label(panel, text, size > 0 ? size : UIKit.Fs(14), color ?? Theme.Text);
        }

        /// <summary>왼쪽 정렬 버튼 하나 (HTML의 &lt;button data-a="close"&gt;닫기&lt;/button&gt;).</summary>
        public Button ModalButton(RectTransform panel, RectTransform root, string label, System.Action action)
        {
            var row = UIKit.Rect("ButtonRow", panel);
            row.Layout(UIKit.Px(40));
            var b = UIKit.Button(row, label, () =>
            {
                Destroy(root.gameObject);
                action?.Invoke();
            });
            var rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(UIKit.Px(64), 0);
            return b;
        }

        /// <summary>확률 표 (HTML의 odds()).</summary>
        public void ShowOdds(PackDefinition pack)
        {
            var root = OpenModal(out var panel, pack.Name + " 확률");
            var odds = GachaEngine.OddsOf(pack, State);
            var ps = State.GetPack(pack);

            OddsRow(panel, "등급", "확률", "포인트 가치", true);
            foreach (var g in GradeInfo.DisplayOrder)
            {
                if (!pack.HasGrade(g)) continue;
                string p = odds[(int)g].ToString("0.#") + "%";
                if (ps.stock != null) p += $" ({ps.stock[(int)g]}개)";
                OddsRow(panel, GradeInfo.Label(g), p, UIKit.Num(pack.Value[(int)g]) + "pt", false);
            }

            ModalText(panel, $"총 환원율 {GachaEngine.Rtp(pack, State):0.0}% (서비스 내 포인트 기준)", Theme.Muted, UIKit.Fs(13))
                .Layout(UIKit.Px(32));
            ModalButton(panel, root, "닫기", null);
        }

        void OddsRow(RectTransform panel, string a, string b, string c, bool header)
        {
            var row = UIKit.Rect("Row", panel);
            row.Layout(UIKit.Px(34));
            var style = header ? FontStyle.Bold : FontStyle.Normal;
            int fs = UIKit.Fs(14);
            var t1 = UIKit.Label(row, a, fs, Theme.Text, TextAnchor.MiddleLeft, style);
            t1.rectTransform.anchorMax = new Vector2(0.25f, 1);
            var t2 = UIKit.Label(row, b, fs, Theme.Text, TextAnchor.MiddleLeft, style);
            t2.rectTransform.anchorMin = new Vector2(0.25f, 0);
            t2.rectTransform.anchorMax = new Vector2(0.62f, 1);
            var t3 = UIKit.Label(row, c, fs, Theme.Text, TextAnchor.MiddleLeft, style);
            t3.rectTransform.anchorMin = new Vector2(0.62f, 0);
            // td { border-bottom:1px solid var(--bd) }
            var line = UIKit.Panel(row, "Line", Theme.Border);
            line.rectTransform.anchorMin = new Vector2(0, 0);
            line.rectTransform.anchorMax = new Vector2(1, 0);
            line.rectTransform.pivot = new Vector2(0.5f, 0);
            line.rectTransform.sizeDelta = new Vector2(0, 3);
            line.raycastTarget = false;
        }

        // ───────────────────────── 토스트

        public void Toast(string message)
        {
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine(message));
        }

        IEnumerator ToastRoutine(string message)
        {
            var existing = canvasRoot.Find("Toast");
            if (existing != null) Destroy(existing.gameObject);

            var bg = UIKit.Panel(canvasRoot, "Toast", Theme.Text.WithAlpha(0.92f), UIKit.Px(8));
            bg.raycastTarget = false;
            bg.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, UIKit.Px(60)), new Vector2(UIKit.Px(360), UIKit.Px(44)));
            UIKit.Label(bg.transform, message, UIKit.Fs(14), Theme.Bg, TextAnchor.MiddleCenter);
            var group = bg.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            for (float e = 0; e < 2.2f; e += Time.unscaledDeltaTime)
            {
                group.alpha = e < 0.15f ? e / 0.15f : e > 1.8f ? 1f - (e - 1.8f) / 0.4f : 1f;
                yield return null;
            }
            Destroy(bg.gameObject);
            toastRoutine = null;
        }
    }

    /// <summary>PlayerPrefs에 JSON으로 저장. 출시 버전에서는 서버 저장으로 바꾼다.</summary>
    public static class SaveSystem
    {
        const string Key = "oripa.save.v2";

        public static PlayerState Load()
        {
            var json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new PlayerState();
            try
            {
                return JsonUtility.FromJson<PlayerState>(json) ?? new PlayerState();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("저장 데이터를 읽지 못해 새로 시작합니다: " + e.Message);
                return new PlayerState();
            }
        }

        public static void Save(PlayerState state)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(state));
            PlayerPrefs.Save();
        }

        public static void Delete() => PlayerPrefs.DeleteKey(Key);
    }
}
