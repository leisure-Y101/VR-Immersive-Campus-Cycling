using UnityEngine;
using UnityEngine.UI;

public static class UiTheme
{
    // PicoCampusCyclingUnity design tokens. Keep one source for any legacy UI
    // path so fallback screens match the editable CampusRideUI prefab.
    public static readonly Color PanelBg = new Color(0.008f, 0.082f, 0.145f, 0.94f); // #021525
    public static readonly Color PanelBgSoft = new Color(0f, 0.141f, 0.227f, 0.72f); // rgba(0,36,58,.72)
    public static readonly Color Accent = new Color(0.031f, 0.749f, 0.91f, 1f); // #08BFE8
    public static readonly Color AccentBright = new Color(0.125f, 0.906f, 1f, 1f); // #20E7FF
    public static readonly Color TextMain = new Color(0.784f, 0.953f, 1f, 1f); // #C8F3FF
    public static readonly Color TextValue = new Color(0.949f, 0.988f, 1f, 1f); // #F2FCFF
    public static readonly Color TextDim = new Color(0.471f, 0.776f, 0.886f, 1f); // #78C6E2
    public static readonly Color TextDark = new Color(0.024f, 0.078f, 0.129f, 1f);

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
