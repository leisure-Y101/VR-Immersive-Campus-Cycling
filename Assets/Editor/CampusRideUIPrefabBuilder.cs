using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-click authoring tool for the editable Campus Ride UI prefab.
/// Use Tools/Campus Ride/Create Editable UI Prefab after opening the project.
/// </summary>
public static class CampusRideUIPrefabBuilder
{
    const string PrefabPath = "Assets/Resources/Prefabs/CampusRideUI.prefab";
    static readonly Color Navy = Hex("061421");
    static readonly Color PanelTone = Hex("051421");
    static readonly Color Card = Hex("0A2230");
    static readonly Color Cyan = Hex("22D3EE");
    static readonly Color TextMain = Hex("EEF7FC");
    static readonly Color TextMuted = Hex("96BEC9");
    static readonly Color Amber = Hex("FFD166");
    static readonly Color Danger = Hex("FF4D4D");

    [MenuItem("Tools/Campus Ride/Create Editable UI Prefab")]
    public static void CreateEditablePrefab()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Prefabs");
        var root = new GameObject("CampusRideUI", typeof(RectTransform), typeof(Canvas), typeof(CampusRideUI));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        canvas.pixelPerfect = false;
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(1600, 900);

        var panel = Panel("MainPanel", root.transform, new Vector2(1090, 776), new Vector2(-250, 0), PanelColor(0.96f));
        Frame(panel.transform, "CutCornerFrame", new Color(Cyan.r, Cyan.g, Cyan.b, 0.90f), 30, 4);
        Line(panel.transform, "TopAccent", new Vector2(940, 5), new Vector2(0, 358), Cyan);
        Line(panel.transform, "BottomAccent", new Vector2(560, 3), new Vector2(0, -358), new Color(Cyan.r, Cyan.g, Cyan.b, 0.65f));
        Line(panel.transform, "LeftAccent", new Vector2(3, 210), new Vector2(-520, 225), new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f));
        Line(panel.transform, "RightAccent", new Vector2(3, 150), new Vector2(520, -190), new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f));

        var title = TextNode("Title", panel.transform, "沉浸式校园骑行", 48, new Vector2(0, 300), new Vector2(960, 64), TextAnchor.MiddleCenter, TextMain);
        title.fontStyle = FontStyle.Bold;
        Frame(title.transform, "TitleGlow", new Color(Cyan.r, Cyan.g, Cyan.b, 0.20f), 12, 2);
        TextNode("Subtitle", panel.transform, "PICO 4 Pro · 360° 校园骑行", 22, new Vector2(0, 255), new Vector2(960, 38), TextAnchor.MiddleCenter, TextMuted);
        var body = TextNode("Body", panel.transform, "", 24, new Vector2(0, -150), new Vector2(900, 190), TextAnchor.UpperCenter, TextMain);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Truncate;

        var rows = Empty("Rows", panel.transform, new Vector2(920, 560), Vector2.zero);
        var template = Panel("RowTemplate", rows.transform, new Vector2(900, 56), new Vector2(0, 0), new Color(Card.r, Card.g, Card.b, 0.88f));
        template.SetActive(false);
        Frame(template.transform, "Frame", new Color(Cyan.r, Cyan.g, Cyan.b, 0.38f), 16, 2);
        var badge = Panel("IconBadge", template.transform, new Vector2(42, 42), new Vector2(-390, 0), new Color(Cyan.r, Cyan.g, Cyan.b, 0.12f));
        Frame(badge.transform, "Frame", new Color(Cyan.r, Cyan.g, Cyan.b, 0.78f), 10, 2);
        var icon = TextNode("Icon", badge.transform, "01", 15, Vector2.zero, new Vector2(40, 36), TextAnchor.MiddleCenter, Cyan);
        icon.fontStyle = FontStyle.Bold;
        TextNode("Label", template.transform, "菜单项目", 27, new Vector2(-280, 0), new Vector2(550, 44), TextAnchor.MiddleLeft, TextMain);
        TextNode("Value", template.transform, "", 24, new Vector2(280, 0), new Vector2(260, 44), TextAnchor.MiddleRight, Amber);
        TextNode("Hint", template.transform, "触发", 14, new Vector2(390, -19), new Vector2(100, 20), TextAnchor.MiddleRight, TextMuted);

        var hud = CanvasNode("RideHUD", root.transform, new Vector2(880, 190));
        var hudBg = Panel("HudBg", hud.transform, new Vector2(860, 180), Vector2.zero, new Color(Navy.r, Navy.g, Navy.b, 0.88f));
        Frame(hudBg.transform, "CutCornerFrame", new Color(Cyan.r, Cyan.g, Cyan.b, 0.72f), 22, 3);
        Line(hud.transform, "HudAccent", new Vector2(730, 4), new Vector2(0, 78), new Color(Cyan.r, Cyan.g, Cyan.b, 0.85f));
        var hr = Panel("HrCard", hud.transform, new Vector2(300, 128), new Vector2(-260, -8), new Color(Card.r, Card.g, Card.b, 0.78f));
        Frame(hr.transform, "Frame", new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f), 16, 2);
        TextNode("HrLabel", hr.transform, "心率 · 模拟心率", 18, new Vector2(0, 34), new Vector2(270, 30), TextAnchor.MiddleCenter, TextMuted);
        TextNode("HrValue", hr.transform, "75 bpm", 34, new Vector2(0, -12), new Vector2(270, 50), TextAnchor.MiddleCenter, TextMain);
        var time = Panel("TimeCard", hud.transform, new Vector2(230, 128), new Vector2(15, -8), new Color(Card.r, Card.g, Card.b, 0.78f));
        Frame(time.transform, "Frame", new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f), 16, 2);
        TextNode("TimeLabel", time.transform, "时长", 18, new Vector2(0, 34), new Vector2(210, 30), TextAnchor.MiddleCenter, TextMuted);
        TextNode("TimeValue", time.transform, "00:00", 32, new Vector2(0, -12), new Vector2(210, 50), TextAnchor.MiddleCenter, TextMain);
        var dist = Panel("DistCard", hud.transform, new Vector2(230, 128), new Vector2(270, -8), new Color(Card.r, Card.g, Card.b, 0.78f));
        Frame(dist.transform, "Frame", new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f), 16, 2);
        TextNode("DistLabel", dist.transform, "距离", 18, new Vector2(0, 34), new Vector2(210, 30), TextAnchor.MiddleCenter, TextMuted);
        TextNode("DistValue", dist.transform, "0.00 km", 32, new Vector2(0, -12), new Vector2(210, 50), TextAnchor.MiddleCenter, TextMain);

        var dan = CanvasNode("Danmaku", root.transform, new Vector2(1200, 100));
        TextNode("Text", dan.transform, "", 34, Vector2.zero, new Vector2(1000, 80), TextAnchor.MiddleCenter, Amber);
        var alert = CanvasNode("HeartAlert", root.transform, new Vector2(900, 120));
        var alertBg = Panel("AlertBg", alert.transform, new Vector2(900, 120), Vector2.zero, new Color(0.28f, 0.02f, 0.03f, 0.78f));
        Frame(alertBg.transform, "Frame", new Color(Danger.r, Danger.g, Danger.b, 0.95f), 20, 3);
        TextNode("Text", alert.transform, "心率过高，请慢一点", 30, Vector2.zero, new Vector2(820, 90), TextAnchor.MiddleCenter, Danger);

        var ui = root.GetComponent<CampusRideUI>();
        ui.mainCanvas = canvas;
        ui.mainPanel = panel.GetComponent<RectTransform>();
        ui.titleText = title;
        ui.subtitleText = root.transform.Find("MainPanel/Subtitle").GetComponent<Text>();
        ui.bodyText = body;
        ui.rowRoot = rows.GetComponent<RectTransform>();
        ui.rowTemplate = template;
        ui.hudCanvas = hud.GetComponent<Canvas>();
        ui.hudHrLabel = hr.transform.Find("HrLabel").GetComponent<Text>();
        ui.hudHrValue = hr.transform.Find("HrValue").GetComponent<Text>();
        ui.hudTimeLabel = time.transform.Find("TimeLabel").GetComponent<Text>();
        ui.hudTimeValue = time.transform.Find("TimeValue").GetComponent<Text>();
        ui.hudDistLabel = dist.transform.Find("DistLabel").GetComponent<Text>();
        ui.hudDistValue = dist.transform.Find("DistValue").GetComponent<Text>();
        ui.danmakuCanvas = dan.GetComponent<Canvas>();
        ui.danmakuText = dan.transform.Find("Text").GetComponent<Text>();
        ui.alertCanvas = alert.GetComponent<Canvas>();
        ui.alertText = alert.transform.Find("Text").GetComponent<Text>();

        var old = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (old != null) Object.DestroyImmediate(old);
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Campus Ride editable UI prefab created at " + PrefabPath);
    }

    static GameObject Empty(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        return go;
    }

    static GameObject Panel(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        var go = Empty(name, parent, size, position);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    static Canvas CanvasNode(string name, Transform parent, Vector2 size)
    {
        var go = Empty(name, parent, size, Vector2.zero);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 110;
        return canvas;
    }

    static Text TextNode(string name, Transform parent, string value, int size, Vector2 position, Vector2 dimensions, TextAnchor anchor, Color color)
    {
        var go = Empty(name, parent, dimensions, position);
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.text = value;
        text.alignment = anchor;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static void Line(Transform parent, string name, Vector2 size, Vector2 position, Color color)
    {
        Panel(name, parent, size, position, color);
    }

    static void Frame(Transform parent, string name, Color color, float cut, float thickness)
    {
        var go = Empty(name, parent, Vector2.zero, Vector2.zero);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var frame = go.AddComponent<CampusRideCutFrame>();
        frame.color = color;
        frame.cornerCut = cut;
        frame.thickness = thickness;
        frame.raycastTarget = false;
    }

    static Color PanelColor(float alpha)
    {
        return new Color(PanelTone.r, PanelTone.g, PanelTone.b, alpha);
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
