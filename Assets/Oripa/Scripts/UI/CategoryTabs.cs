using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Oripa.UI
{
    /// <summary>
    /// 카테고리 탭 바. tabs 아래 켜져 있는 자식 하나가 탭 하나다 (Label 글자 + Underline 밑줄).
    /// 선택된 탭은 진한 굵은 글자 + 빨간 밑줄, 나머지는 회색 글자.
    /// 탭을 늘리려면 Scene에서 Tab을 복제(Ctrl+D)하고 Label 글자를 바꾸면 된다.
    /// 선택이 바뀌면 onSelect(번호)가 불린다 (팩 거르기 등은 여기에 연결).
    /// </summary>
    [ExecuteAlways]
    public class CategoryTabs : MonoBehaviour
    {
        public RectTransform tabs;
        public int selected;
        public Color activeColor = new Color(0.08f, 0.09f, 0.17f);
        public Color inactiveColor = new Color(0.6f, 0.62f, 0.68f);
        public Color underlineColor = new Color(0.86f, 0.04f, 0.17f);
        public IntEvent onSelect = new IntEvent();

        [System.Serializable] public class IntEvent : UnityEvent<int> { }

        public int SelectedIndex => selected;

        public string SelectedName
        {
            get
            {
                var t = TabAt(selected);
                var label = t != null ? t.GetComponentInChildren<Text>() : null;
                return label != null ? label.text : "";
            }
        }

        void Start()
        {
            if (!Application.isPlaying) { Apply(); return; }
            int index = 0;
            for (int i = 0; i < tabs.childCount; i++)
            {
                var c = tabs.GetChild(i);
                if (!c.gameObject.activeSelf) continue;
                var b = c.GetComponent<Button>();
                if (b == null) b = c.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                int my = index++;
                b.onClick.AddListener(() => Select(my));
            }
            Apply();
        }

        public void Select(int index)
        {
            if (index == selected) return;
            selected = index;
            Apply();
            onSelect.Invoke(selected);
        }

        RectTransform TabAt(int index)
        {
            if (tabs == null) return null;
            int n = 0;
            for (int i = 0; i < tabs.childCount; i++)
            {
                var c = tabs.GetChild(i);
                if (!c.gameObject.activeSelf) continue;
                if (n++ == index) return (RectTransform)c;
            }
            return null;
        }

        /// <summary>선택 상태에 맞게 글자색, 굵기, 밑줄을 맞춘다.</summary>
        public void Apply()
        {
            if (tabs == null) return;
            int n = 0;
            for (int i = 0; i < tabs.childCount; i++)
            {
                var c = tabs.GetChild(i);
                if (!c.gameObject.activeSelf) continue;
                bool on = n++ == selected;
                var label = c.Find("Label");
                var text = label != null ? label.GetComponent<Text>() : null;
                if (text != null)
                {
                    text.color = on ? activeColor : inactiveColor;
                    text.fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
                }
                var line = c.Find("Underline");
                if (line != null)
                {
                    var img = line.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = underlineColor;
                        img.enabled = on;
                    }
                }
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () => { if (this != null) Apply(); };
        }
#endif
    }
}
