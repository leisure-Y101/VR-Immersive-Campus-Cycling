using UnityEngine;
using UnityEngine.UI;

public static class UiTheme
{
    public static readonly Color PanelBg = new Color(0.07f, 0.07f, 0.09f, 0.88f);
    public static readonly Color PanelBgSoft = new Color(0.13f, 0.13f, 0.16f, 0.85f);
    public static readonly Color Accent = new Color(1.00f, 0.63f, 0.20f, 1f);
    public static readonly Color TextMain = new Color(0.95f, 0.95f, 0.97f, 1f);
    public static readonly Color TextDim = new Color(0.72f, 0.72f, 0.78f, 1f);
    public static readonly Color TextDark = new Color(0.12f, 0.09f, 0.04f, 1f);

    static Font _font;
    public static Font Font
    {
        get
        {
            if (_font == null)
            {
                _font = Resources.Load<Font>("Fonts/simhei");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return _font;
        }
    }

    static Sprite _panelSprite;
    public static Sprite PanelSprite
    {
        get
        {
            if (_panelSprite == null) _panelSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            return _panelSprite;
        }
    }

    public static Image CreatePanel(Transform parent, string name, Vector2 size, Vector2 pos, Color? bg = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        if (PanelSprite != null)
        {
            img.sprite = PanelSprite;
            img.type = Image.Type.Sliced;
        }
        img.color = bg ?? PanelBg;
        img.raycastTarget = false;
        return img;
    }

    public static Text CreateText(Transform parent, string name, string content, int size, Color color, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = Font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.text = content;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
}
