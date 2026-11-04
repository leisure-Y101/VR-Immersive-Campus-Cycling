using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 通用焦点式菜单面板：竖排选项，焦点高亮，支持 动作/数值/开关 三种条目
// 交互规范（game-ui-ux）：上下移动焦点、左右调值、确认触发、每屏默认选中第一项
public class MenuPanel
{
    enum Kind { Action, Value, Toggle }

    class Item
    {
        public GameObject go;
        public Image bg;
        public Text label;
        public Text value;
        public Kind kind;
        public Func<string> get;
        public Action<int> adjust;
        public Action confirm;
    }

    public readonly RectTransform Root;
    public int Index { get; private set; }

    readonly List<Item> items = new List<Item>();
    readonly Vector2 itemSize;
    readonly float step = 72f;
    float listTopY;
    Text body;
    Text titleText;
    Text subtitle;

    public MenuPanel(Transform parent, string title, string hint, Vector2 size, Vector2 pos, bool withBody = false,
                     float itemWidth = 420f, float itemHeight = 58f)
    {
        itemSize = new Vector2(itemWidth, itemHeight);
        var img = UiTheme.CreatePanel(parent, "Panel_" + title, size, pos);
        Root = (RectTransform)img.transform;

        var t = UiTheme.CreateText(Root, "Title", title, 40, UiTheme.TextMain, TextAnchor.MiddleCenter);
        titleText = t;
        var tr = (RectTransform)t.transform;
        tr.sizeDelta = new Vector2(size.x - 40, 54);
        tr.anchoredPosition = new Vector2(0, size.y / 2 - 46);

        subtitle = UiTheme.CreateText(Root, "Subtitle", "", 22, UiTheme.TextDim, TextAnchor.MiddleCenter);
        var sr = (RectTransform)subtitle.transform;
        sr.sizeDelta = new Vector2(size.x - 40, 28);
        sr.anchoredPosition = new Vector2(0, -size.y / 2 + 58);
        subtitle.gameObject.SetActive(false);

        listTopY = size.y / 2 - 104;
        if (withBody)
        {
            body = UiTheme.CreateText(Root, "Body", "", 28, UiTheme.TextMain, TextAnchor.MiddleCenter);
            var br = (RectTransform)body.transform;
            br.sizeDelta = new Vector2(size.x - 60, 140);
            br.anchoredPosition = new Vector2(0, listTopY - 70);
            listTopY -= 150;
        }

        if (!string.IsNullOrEmpty(hint))
        {
            var h = UiTheme.CreateText(Root, "Hint", hint, 20, UiTheme.TextDim, TextAnchor.MiddleCenter);
            var hr = (RectTransform)h.transform;
            hr.sizeDelta = new Vector2(size.x - 30, 30);
            hr.anchoredPosition = new Vector2(0, -size.y / 2 + 26);
        }

        Root.gameObject.SetActive(false);
    }

    public void SetBody(string text) { if (body != null) body.text = text; }

    public void SetTitle(string text)
    {
        if (titleText != null) titleText.text = text;
    }

    public void SetSubtitle(string text)
    {
        if (subtitle == null) return;
        bool show = !string.IsNullOrEmpty(text);
        subtitle.gameObject.SetActive(show);
        if (show) subtitle.text = text;
    }

    public void AddAction(string label, Action onConfirm)
    {
        var it = new Item { kind = Kind.Action, confirm = onConfirm };
        Build(it, label);
    }

    public void AddValue(string label, Func<string> get, Action<int> adjust)
    {
        var it = new Item { kind = Kind.Value, get = get, adjust = adjust };
        Build(it, label);
    }

    public void AddToggle(string label, Func<string> get, Action onToggle)
    {
        var it = new Item { kind = Kind.Toggle, get = get, confirm = onToggle, adjust = _ => onToggle() };
        Build(it, label);
    }

    void Build(Item it, string label)
    {
        var img = UiTheme.CreatePanel(Root, "Item_" + label, itemSize, Vector2.zero, UiTheme.PanelBgSoft);
        it.go = img.gameObject;
        it.bg = img;

        it.label = UiTheme.CreateText(img.transform, "Label", label, 30, UiTheme.TextMain, TextAnchor.MiddleLeft);
        var lr = (RectTransform)it.label.transform;
        lr.anchorMin = new Vector2(0, 0);
        lr.anchorMax = new Vector2(1, 1);
        lr.offsetMin = new Vector2(24, 0);
        lr.offsetMax = new Vector2(-150, 0);

        if (it.kind != Kind.Action)
        {
            it.value = UiTheme.CreateText(img.transform, "Value", "", 30, UiTheme.TextDim, TextAnchor.MiddleRight);
            var vr = (RectTransform)it.value.transform;
            vr.anchorMin = new Vector2(1, 0);
            vr.anchorMax = new Vector2(1, 1);
            vr.pivot = new Vector2(1f, 0.5f);
            vr.sizeDelta = new Vector2(140, 0);
            vr.anchoredPosition = new Vector2(-20, 0);
        }

        items.Add(it);
        LayoutItems();
    }

    void LayoutItems()
    {
        for (int i = 0; i < items.Count; i++)
            ((RectTransform)items[i].go.transform).anchoredPosition = new Vector2(0, listTopY - step * i);
    }

    public void Show() { Root.gameObject.SetActive(true); Index = 0; Refresh(); }
    public void Hide() { Root.gameObject.SetActive(false); }

    public void Move(int dir)
    {
        if (items.Count == 0) return;
        Index = (Index + dir + items.Count) % items.Count;
        Refresh();
    }

    public void Adjust(int dir)
    {
        if (items.Count == 0) return;
        var it = items[Index];
        if (it.adjust != null) { it.adjust(dir); Refresh(); }
    }

    public void Confirm()
    {
        if (items.Count == 0) return;
        var it = items[Index];
        if (it.kind == Kind.Action) it.confirm?.Invoke();
        else if (it.kind == Kind.Value) { it.adjust?.Invoke(1); Refresh(); }
        else { it.confirm?.Invoke(); Refresh(); }
    }

    public void Refresh()
    {
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            bool f = i == Index;
            if (it.bg != null) it.bg.color = f ? UiTheme.Accent : UiTheme.PanelBgSoft;
            if (it.label != null) it.label.color = f ? UiTheme.TextDark : UiTheme.TextMain;
            if (it.value != null)
            {
                it.value.text = it.get != null ? it.get() : "";
                it.value.color = f ? UiTheme.TextDark : UiTheme.TextDim;
            }
            if (it.go != null) it.go.transform.localScale = f ? Vector3.one * 1.03f : Vector3.one;
        }
    }
}
