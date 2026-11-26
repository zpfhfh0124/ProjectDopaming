using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Oripa.UI
{
    /// <summary>
    /// HTML 프로토타입의 CSS 변수(--bg, --sf, --tx, --mu, --ac, --bd)를 그대로 옮긴 색 테마.
    /// 라이트/다크 두 가지이며, 헤더의 버튼으로 바꾼다.
    /// </summary>
    public static class Theme
    {
        public static bool Dark { get; private set; }
        public static Color Bg, Surface, Text, Muted, Accent, Border;

        public static readonly Color Overlay = new Color(10 / 255f, 12 / 255f, 24 / 255f, 0.6f);
        public static readonly Color GradeS1 = Hex("#e8a200");
        public static readonly Color GradeS2 = Hex("#ff6b9d");
        public static readonly Color GradeS3 = Hex("#6b8cff");
        public static readonly Color GradeA = Hex("#d6336c");
        public static readonly Color GradeB = Hex("#2f7bff");
        public static readonly Color GradeCD = Hex("#7d869c");

        static Theme() => Apply(false);

        public static void Apply(bool dark)
        {
            Dark = dark;
            if (dark)
            {
                Bg = Hex("#0f1220"); Surface = Hex("#191d31"); Text = Hex("#eef0fa");
                Muted = Hex("#9aa3bf"); Accent = Hex("#7c9bff"); Border = Hex("#2a2f4a");
            }
            else
            {
                Bg = Hex("#eef1f7"); Surface = Hex("#ffffff"); Text = Hex("#14182b");
                Muted = Hex("#667089"); Accent = Hex("#2f5bff"); Border = Hex("#dbe0ee");
            }
        }

        public static Color GradeColor(Grade g) => g switch
        {
            Grade.S => GradeS1,
            Grade.A => GradeA,
            Grade.B => GradeB,
            _ => GradeCD,
        };

        public static Color Hex(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);
    }

    /// <summary>
    /// 코드로 uGUI를 만드는 도우미. 크기 단위는 캔버스 단위(기준 폭 1080)이고,
    /// HTML의 CSS px 값은 Px()로 변환한다 (폭 약 415px 휴대폰 화면 = 1080).
    /// </summary>
    public static class UIKit
    {
        public const float PxScale = 2.6f;
        public static float Px(float cssPx) => cssPx * PxScale;
        public static int Fs(float cssPx) => Mathf.RoundToInt(cssPx * PxScale);

        static Font font;
        static Sprite rounded;
        static Sprite glow;
        const int RoundedRadius = 32;

        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    // HTML과 같은 순서로 한글 폰트를 찾는다.
                    font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Apple SD Gothic Neo", "Malgun Gothic", "Noto Sans KR", "Noto Sans CJK KR", "NanumGothic", "Arial" }, 32);
                    if (font != null) font.hideFlags = HideFlags.DontSave;
                    else font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return font;
            }
        }

        public static Sprite Rounded => rounded != null ? rounded : rounded = MakeRounded(96, RoundedRadius, false);
        public static Sprite Glow => glow != null ? glow : glow = MakeRounded(128, 64, true);

        static Sprite MakeRounded(int size, int radius, bool soft)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = soft ? "OripaGlow" : "OripaRounded",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float dx = Mathf.Max(radius - fx, fx - (size - radius), 0f);
                    float dy = Mathf.Max(radius - fy, fy - (size - radius), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = soft ? Mathf.Pow(Mathf.Clamp01(1f - d / radius), 2f) : Mathf.Clamp01(radius - d);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave; // 에디터 미리보기에서 씬에 저장되거나 정리되지 않게
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        // ───────────── 기본 요소

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>둥근 모서리 이미지. radius는 캔버스 단위.</summary>
        public static Image Panel(Transform parent, string name, Color color, float radius = 0f)
        {
            var rt = Rect(name, parent).Stretch();
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            SetRadius(img, radius);
            return img;
        }

        public static void SetRadius(Image img, float radius)
        {
            if (radius <= 0f)
            {
                img.sprite = null;
                img.type = Image.Type.Simple;
                return;
            }
            img.sprite = Rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = RoundedRadius / radius;
        }

        /// <summary>테두리 있는 상자 (CSS의 border:1px solid). 반환값은 안쪽 면.</summary>
        public static Image BorderBox(Transform parent, string name, Color fill, Color border, float radius, float borderWidth = 3f)
        {
            var outer = Panel(parent, name, border, radius);
            var inner = Panel(outer.transform, "Fill", fill, Mathf.Max(1f, radius - borderWidth));
            inner.rectTransform.Stretch(borderWidth, borderWidth, borderWidth, borderWidth);
            inner.raycastTarget = false;
            return inner;
        }

        public static Image GlowImage(Transform parent, Color color, float spread)
        {
            var rt = Rect("Glow", parent).Stretch(-spread, -spread, -spread, -spread);
            rt.SetAsFirstSibling();
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Glow;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 64f / Mathf.Max(1f, spread);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect("Text", parent).Stretch();
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.lineSpacing = 1.1f;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        // ───────────── 버튼 (CSS button / button.g)

        /// <summary>기본 버튼: 강조색 배경, 흰 글씨, 둥근 모서리 8px.</summary>
        public static Button Button(Transform parent, string label, UnityAction onClick, int fontSize = 0)
        {
            return MakeButton(parent, label, Theme.Accent, Color.white, Color.clear, onClick, fontSize);
        }

        /// <summary>보조 버튼 (.g): 투명 배경, 테두리, 본문색 글씨. fill은 버튼 뒤 배경색.</summary>
        public static Button GhostButton(Transform parent, string label, Color fill, UnityAction onClick, int fontSize = 0)
        {
            return MakeButton(parent, label, fill, Theme.Text, Theme.Border, onClick, fontSize);
        }

        public static Button MakeButton(Transform parent, string label, Color bg, Color fg, Color border, UnityAction onClick, int fontSize)
        {
            float radius = Px(8);
            Image target;
            if (border.a > 0f)
            {
                var outer = Panel(parent, "Button", border, radius);
                var fillImg = Panel(outer.transform, "Fill", bg, radius - 3f);
                fillImg.rectTransform.Stretch(3, 3, 3, 3);
                fillImg.raycastTarget = false;
                target = outer;
            }
            else
            {
                target = Panel(parent, "Button", bg, radius);
            }

            var btn = target.gameObject.AddComponent<Button>();
            btn.targetGraphic = target;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white; // 비활성 표시는 CanvasGroup 투명도로
            colors.fadeDuration = 0.06f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var t = Label(target.transform, label, fontSize > 0 ? fontSize : Fs(15), fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.rectTransform.Stretch(Px(8), 0, Px(8), 0);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            target.gameObject.AddComponent<CanvasGroup>();
            return btn;
        }

        /// <summary>CSS button:disabled {opacity:.4}</summary>
        public static void SetEnabled(this Button b, bool enabled)
        {
            b.interactable = enabled;
            var g = b.GetComponent<CanvasGroup>();
            if (g != null) g.alpha = enabled ? 1f : 0.4f;
        }

        // ───────────── 레이아웃

        public static LayoutElement Layout(this Component c, float preferredHeight = -1, float preferredWidth = -1, float flexibleWidth = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.preferredWidth = preferredWidth;
            le.flexibleWidth = flexibleWidth;
            return le;
        }

        public static VerticalLayoutGroup Vertical(GameObject go, RectOffset padding, float spacing)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.padding = padding;
            v.spacing = spacing;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup Horizontal(GameObject go, RectOffset padding, float spacing)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.padding = padding;
            h.spacing = spacing;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
            return h;
        }

        public static RectOffset Pad(float all) => Pad(all, all);

        public static RectOffset Pad(float vertical, float horizontal)
        {
            int v = Mathf.RoundToInt(vertical), h = Mathf.RoundToInt(horizontal);
            return new RectOffset(h, h, v, v);
        }

        /// <summary>세로 스크롤 영역. content에 자식을 붙이면 된다.</summary>
        public static ScrollRect VerticalScroll(Transform parent, out RectTransform content)
        {
            var root = Rect("Scroll", parent).Stretch();
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;

            var viewport = Rect("Viewport", root).Stretch();
            viewport.gameObject.AddComponent<RectMask2D>();

            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 60f;
            return scroll;
        }

        public static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    child.SetActive(false);
                    Object.Destroy(child);
                }
                else Object.DestroyImmediate(child); // 에디터 미리보기
            }
        }

        public static string Num(long n) => n.ToString("N0");

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        public static float EaseOutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        // ───────────── 팩 이미지

        static readonly Dictionary<string, Texture2D> artCache = new Dictionary<string, Texture2D>();

        /// <summary>Resources/PackArt/{name} 이미지를 읽는다. 없으면 null.</summary>
        public static Texture2D LoadPackArt(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return LoadTexture("PackArt/" + name);
        }

        /// <summary>Resources 아래 경로(확장자 없이)의 이미지를 읽는다. 없으면 null.</summary>
        public static Texture2D LoadTexture(string path)
        {
            if (artCache.TryGetValue(path, out var tex) && tex != null) return tex;
            tex = Resources.Load<Texture2D>(path);
            artCache[path] = tex;
            return tex;
        }
    }

    /// <summary>
    /// CSS linear-gradient(135deg, a, b, c) 를 흉내 내는 정점 색 그라데이션.
    /// 9-slice 이미지의 정점에 색을 입혀서 둥근 모서리를 유지한다.
    /// </summary>
    public sealed class DiagonalGradient : BaseMeshEffect
    {
        public Color A = Color.white, B = Color.white, C = Color.white;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            var rect = graphic.rectTransform.rect;
            if (rect.width <= 0 || rect.height <= 0) return;
            var v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                float nx = (v.position.x - rect.xMin) / rect.width;
                float ny = (v.position.y - rect.yMin) / rect.height;
                float t = Mathf.Clamp01((nx + (1f - ny)) * 0.5f); // 왼쪽 위 0 → 오른쪽 아래 1
                Color c = t < 0.5f ? Color.Lerp(A, B, t * 2f) : Color.Lerp(B, C, (t - 0.5f) * 2f);
                c.a *= ((Color)v.color).a;
                v.color = c;
                vh.SetUIVertex(v, i);
            }
        }
    }
}
