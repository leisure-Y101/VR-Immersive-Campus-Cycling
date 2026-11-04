using System.IO;
using System.Xml;

#if UNITY_EDITOR
using UnityEditor.Android;
#endif

// 构建时向生成的 AndroidManifest.xml 注入 BLE 相关权限（不覆盖主 Manifest）
public class ThinkRiderManifestPostProcessor
#if UNITY_EDITOR
    : IPostGenerateGradleAndroidProject
#endif
{
#if UNITY_EDITOR
    public int callbackOrder => 1000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifestPath)) return;

        var doc = new XmlDocument();
        doc.Load(manifestPath);
        var manifest = doc.DocumentElement;
        const string androidNs = "http://schemas.android.com/apk/res/android";

        // Android 12+ 的 BLE 权限
        AddPermission(doc, manifest, "android.permission.BLUETOOTH_SCAN", "usesPermissionFlags", androidNs, "neverForLocation");
        AddPermission(doc, manifest, "android.permission.BLUETOOTH_CONNECT", null, null, null);

        // Kinect 转向 UDP 接收
        AddPermission(doc, manifest, "android.permission.INTERNET", null, null, null);

        // Android 11 及以下的旧权限
        AddPermission(doc, manifest, "android.permission.BLUETOOTH", "maxSdkVersion", androidNs, "30");
        AddPermission(doc, manifest, "android.permission.BLUETOOTH_ADMIN", "maxSdkVersion", androidNs, "30");
        AddPermission(doc, manifest, "android.permission.ACCESS_FINE_LOCATION", "maxSdkVersion", androidNs, "30");

        doc.Save(manifestPath);
        UnityEngine.Debug.Log("ThinkRiderManifestPostProcessor: BLE 权限已注入");
    }

    static void AddPermission(XmlDocument doc, XmlElement manifest, string name,
                              string extraAttr, string extraNs, string extraValue)
    {
        foreach (XmlNode node in manifest.ChildNodes)
        {
            if (node.Name != "uses-permission" || node.Attributes == null) continue;
            var a = node.Attributes["android:name"];
            if (a != null && a.Value == name) return;
        }

        var el = doc.CreateElement("uses-permission");
        el.SetAttribute("name", "http://schemas.android.com/apk/res/android", name);
        if (!string.IsNullOrEmpty(extraAttr) && !string.IsNullOrEmpty(extraNs))
            el.SetAttribute(extraAttr, extraNs, extraValue);
        manifest.AppendChild(el);
    }
#endif
}
