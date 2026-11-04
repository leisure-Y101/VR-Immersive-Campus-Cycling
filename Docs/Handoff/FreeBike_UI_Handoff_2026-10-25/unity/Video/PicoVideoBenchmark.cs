using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR;

/// <summary>
/// Standalone Android benchmark scene. It deliberately does not touch the ride
/// scene. It copies a StreamingAssets video to persistent storage, plays it on
/// an inward sphere, and shows the real decoded/XR dimensions.
/// </summary>
public class PicoVideoBenchmark : MonoBehaviour
{
    public string[] videoFiles = { "bench_4k60.mp4", "bench_57k30.mp4" };
    public Material videoMaterial;

    UnityVideoBackend backend;
    PackagedVideoInstaller installer;
    GameObject sphere;
    Renderer sphereRenderer;
    int currentIndex;
    string status = "starting";
    string error = "";
    bool started;
    float[] speeds = { 0.9f, 1.0f, 1.1f };
    int speedIndex = 1;
    bool prevTrigger;
    bool prevPrimary;
    bool prevSecondary;
    float lastLog;

    void Start()
    {
        Application.targetFrameRate = 90;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "BenchmarkVideoSphere";
        sphere.transform.position = Vector3.zero;
        sphere.transform.localScale = Vector3.one * 800f;
        var collider = sphere.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        Material mat = videoMaterial != null ? new Material(videoMaterial) : null;
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("UI/Default");
            if (shader == null)
            {
                error = "No usable shader after build";
                Debug.LogError("[Benchmark] " + error);
                return;
            }
            mat = new Material(shader);
        }
        mat.SetFloat("_Cull", 0f);
        mat.SetColor("_BaseColor", Color.white);
        sphereRenderer = sphere.GetComponent<Renderer>();
        sphereRenderer.sharedMaterial = mat;

        var player = sphere.AddComponent<VideoPlayer>();
        backend = sphere.AddComponent<UnityVideoBackend>();
        backend.Configure(sphereRenderer, "_BaseMap");
        backend.Error += message => error = message;

        installer = gameObject.AddComponent<PackagedVideoInstaller>();

        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
        }

        Load(0);
    }

    void Load(int index)
    {
        if (videoFiles.Length == 0) return;
        if (installer != null && installer.IsBusy) return;
        currentIndex = Mathf.Clamp(index, 0, videoFiles.Length - 1);
        started = false;
        error = "";
        status = "loading " + videoFiles[currentIndex];
        backend.Stop();
        sphere.SetActive(true);

        StartCoroutine(installer.Install(videoFiles[currentIndex],
            path =>
            {
                status = "preparing " + path;
                backend.Prepare(new Uri(path).AbsoluteUri);
            },
            message =>
            {
                error = message;
                status = "install failed";
            }));
    }

    void Update()
    {
        if (backend != null && backend.IsPrepared && !started)
        {
            started = true;
            backend.SetSpeed(speeds[speedIndex]);
            backend.Play();
            status = "playing " + videoFiles[currentIndex];
        }

        if (backend != null && backend.LastError.Length > 0)
            error = backend.LastError;

        ReadController();

        if (Time.time - lastLog > 2f)
        {
            lastLog = Time.time;
            Debug.Log($"[Benchmark] {videoFiles[currentIndex]} src={backend.Width}x{backend.Height} " +
                      $"frame={backend.TimeSeconds:F1}s speed={speeds[speedIndex]:F2} " +
                      $"eye={XRSettings.eyeTextureWidth}x{XRSettings.eyeTextureHeight} " +
                      $"gfx={SystemInfo.graphicsDeviceType} err={backend.LastError}");
        }
    }

    void ReadController()
    {
        bool trigger = false, primary = false, secondary = false;
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (right.isValid)
        {
            right.TryGetFeatureValue(CommonUsages.triggerButton, out trigger);
            right.TryGetFeatureValue(CommonUsages.primaryButton, out primary);
            right.TryGetFeatureValue(CommonUsages.secondaryButton, out secondary);
        }

        if (trigger && !prevTrigger)
            Load((currentIndex + 1) % videoFiles.Length);

        if (secondary && !prevSecondary)
        {
            if (backend.IsPlaying) backend.Pause();
            else backend.Play();
        }

        if (primary && !prevPrimary)
        {
            speedIndex = (speedIndex + 1) % speeds.Length;
            backend.SetSpeed(speeds[speedIndex]);
        }

        prevTrigger = trigger;
        prevPrimary = primary;
        prevSecondary = secondary;
    }

    void OnGUI()
    {
        int fs = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 70f), 18, 38);
        GUI.skin.label.fontSize = fs;
        GUI.skin.box.fontSize = fs + 2;
        GUI.skin.button.fontSize = fs;

        float w = Mathf.Min(1120f, Screen.width - 60f);
        float line = fs + 14f;
        float h = line * 8f + 60f;
        float x = (Screen.width - w) * 0.5f;
        float y = Mathf.Clamp(Screen.height * 0.50f - h * 0.5f, 40f, Screen.height - h - 40f);

        GUI.Box(new Rect(x, y, w, h), "FreeBike Pico local video benchmark");
        float ty = y + 34f;
        GUI.Label(new Rect(x + 24f, ty, w - 48f, line), "Source: " + videoFiles[currentIndex]); ty += line;
        GUI.Label(new Rect(x + 24f, ty, w - 48f, line), $"Install: {installer.CurrentStatus}  ({installer.CurrentProgress:P0})"); ty += line;
        GUI.Label(new Rect(x + 24f, ty, w - 48f, line), $"Video: {backend.Width}x{backend.Height}  prepared={backend.IsPrepared} playing={backend.IsPlaying}"); ty += line;
        GUI.Label(new Rect(x + 24f, ty, w - 48f, line), $"Time: {backend.TimeSeconds:F1}s / {backend.DurationSeconds:F1}s  speed={speeds[speedIndex]:F2}"); ty += line;
        GUI.Label(new Rect(x + 24f, ty, w - 48f, line), $"XR eye texture: {XRSettings.eyeTextureWidth}x{XRSettings.eyeTextureHeight}"); ty += line;
        GUI.Label(new Rect(x + 24f, ty, w - 48f, line), $"GFX: {SystemInfo.graphicsDeviceType}  CPU: {SystemInfo.processorType}"); ty += line;
        GUI.Label(new Rect(x + 24f, ty, w - 48f, line), "Trigger/A: next sample   B: pause   Primary: speed");
        if (!string.IsNullOrEmpty(error))
        {
            ty += line;
            GUI.Label(new Rect(x + 24f, ty, w - 48f, line), "ERROR: " + error);
        }

        float by = y + h - 44f;
        float bw = (w - 48f - 4f * 12f) / 5f;
        if (GUI.Button(new Rect(x + 24f, by, bw, 32f), "4K60")) Load(0);
        if (GUI.Button(new Rect(x + 24f + (bw + 12f), by, bw, 32f), "5.7K30")) Load(1);
        if (GUI.Button(new Rect(x + 24f + (bw + 12f) * 2f, by, bw, 32f), "Pause")) { if (backend.IsPlaying) backend.Pause(); else backend.Play(); }
        if (GUI.Button(new Rect(x + 24f + (bw + 12f) * 3f, by, bw, 32f), "0.9x")) { speedIndex = 0; backend.SetSpeed(speeds[0]); }
        if (GUI.Button(new Rect(x + 24f + (bw + 12f) * 4f, by, bw, 32f), "1.1x")) { speedIndex = 2; backend.SetSpeed(speeds[2]); }
    }
}
