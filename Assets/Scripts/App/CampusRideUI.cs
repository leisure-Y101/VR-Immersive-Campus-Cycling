using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual-only binding surface for the Campus Ride UI prefab.
/// Designers can edit the referenced objects in CampusRideUI.prefab without changing CampusRideApp.
/// </summary>
public class CampusRideUI : MonoBehaviour
{
    [Header("World-space main panel")]
    public Canvas mainCanvas;
    public RectTransform mainPanel;
    public Text titleText;
    public Text subtitleText;
    public Text bodyText;
    public RectTransform rowRoot;
    public GameObject rowTemplate;

    [Header("Head-relative ride HUD")]
    public Canvas hudCanvas;
    public Text hudHrLabel;
    public Text hudHrValue;
    public Text hudTimeLabel;
    public Text hudTimeValue;
    public Text hudDistLabel;
    public Text hudDistValue;

    [Header("Head-relative feedback")]
    public Canvas danmakuCanvas;
    public Text danmakuText;
    public Canvas alertCanvas;
    public Text alertText;

    public static CampusRideUI FindOrNull(GameObject root)
    {
        if (root == null) return null;
        var ui = root.GetComponent<CampusRideUI>();
        if (ui == null) ui = root.AddComponent<CampusRideUI>();
        ui.AutoBind();
        return ui;
    }

    public void AutoBind()
    {
        if (mainCanvas == null) mainCanvas = GetComponent<Canvas>();
        if (mainPanel == null) mainPanel = FindRect("MainPanel");
        if (titleText == null) titleText = FindText("MainPanel/Title");
        if (subtitleText == null) subtitleText = FindText("MainPanel/Subtitle");
        if (bodyText == null) bodyText = FindText("MainPanel/Body");
        if (rowRoot == null) rowRoot = FindRect("MainPanel/Rows");
        if (rowTemplate == null)
        {
            var t = FindTransform("MainPanel/Rows/RowTemplate");
            if (t != null) rowTemplate = t.gameObject;
        }

        if (hudCanvas == null) hudCanvas = FindCanvas("RideHUD");
        if (hudHrLabel == null) hudHrLabel = FindText("RideHUD/HrCard/HrLabel");
        if (hudHrValue == null) hudHrValue = FindText("RideHUD/HrCard/HrValue");
        if (hudTimeLabel == null) hudTimeLabel = FindText("RideHUD/TimeCard/TimeLabel");
        if (hudTimeValue == null) hudTimeValue = FindText("RideHUD/TimeCard/TimeValue");
        if (hudDistLabel == null) hudDistLabel = FindText("RideHUD/DistCard/DistLabel");
        if (hudDistValue == null) hudDistValue = FindText("RideHUD/DistCard/DistValue");

        if (danmakuCanvas == null) danmakuCanvas = FindCanvas("Danmaku");
        if (danmakuText == null) danmakuText = FindText("Danmaku/Text");
        if (alertCanvas == null) alertCanvas = FindCanvas("HeartAlert");
        if (alertText == null) alertText = FindText("HeartAlert/Text");
    }

    public void SetRuntimeFont(Font font)
    {
        if (font == null) return;
        foreach (var text in GetComponentsInChildren<Text>(true)) text.font = font;
    }

    Transform FindTransform(string path) { return transform.Find(path); }
    RectTransform FindRect(string path) { return FindTransform(path) as RectTransform; }
    Text FindText(string path)
    {
        var t = FindTransform(path);
        return t != null ? t.GetComponent<Text>() : null;
    }
    Canvas FindCanvas(string path)
    {
        var t = FindTransform(path);
        return t != null ? t.GetComponent<Canvas>() : null;
    }
}
