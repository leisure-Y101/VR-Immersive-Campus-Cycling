using System;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Unity VideoPlayer backend. Works in Editor and is the compatibility path
/// for Android until an AVPro/OES backend is licensed and verified.
/// </summary>
public class UnityVideoBackend : MonoBehaviour, IVideoBackend
{
    public Renderer targetRenderer;
    public string materialProperty = "_BaseMap";

    VideoPlayer player;
    bool prepared;

    public bool IsPrepared => prepared;
    public bool IsPlaying => player != null && player.isPlaying;
    public float DurationSeconds => player != null ? (float)player.length : 0f;
    public float TimeSeconds => player != null ? (float)player.time : 0f;
    public int Width => player != null ? (int)player.width : 0;
    public int Height => player != null ? (int)player.height : 0;
    public string LastError { get; private set; } = "";

    public event Action<string> Error;

    void Awake()
    {
        player = GetComponent<VideoPlayer>();
        if (player == null) player = gameObject.AddComponent<VideoPlayer>();

        player.playOnAwake = false;
        player.waitForFirstFrame = false;
        player.skipOnDrop = true;
        player.isLooping = true;
        player.audioOutputMode = VideoAudioOutputMode.Direct;
        player.source = VideoSource.Url;
        player.errorReceived += OnPlayerError;
        player.prepareCompleted += OnPrepared;
    }

    public void Configure(Renderer renderer, string property)
    {
        targetRenderer = renderer;
        materialProperty = string.IsNullOrEmpty(property) ? "_BaseMap" : property;
    }

    public void Prepare(string url)
    {
        if (player == null) return;
        prepared = false;
        LastError = "";
        player.Stop();
        player.url = url;
        player.renderMode = VideoRenderMode.MaterialOverride;
        player.targetMaterialRenderer = targetRenderer;
        player.targetMaterialProperty = materialProperty;
        player.Prepare();
    }

    void OnPrepared(VideoPlayer source)
    {
        prepared = true;
        Debug.Log($"[Video] prepared {source.width}x{source.height} @ {source.frameRate:F2}fps");
    }

    void OnPlayerError(VideoPlayer source, string message)
    {
        LastError = message;
        prepared = false;
        Debug.LogError("[Video] " + message);
        Error?.Invoke(message);
    }

    public void Play()
    {
        if (player == null) return;
        if (!prepared)
        {
            player.prepareCompleted += PlayWhenPrepared;
            player.Prepare();
            return;
        }
        player.Play();
    }

    void PlayWhenPrepared(VideoPlayer source)
    {
        player.prepareCompleted -= PlayWhenPrepared;
        player.Play();
    }

    public void Pause() { if (player != null) player.Pause(); }
    public void Stop() { if (player != null) player.Stop(); prepared = false; }

    public void SetSpeed(float speed)
    {
        if (player != null) player.playbackSpeed = Mathf.Clamp(speed, 0.25f, 2f);
    }

    public void Seek(float seconds)
    {
        if (player != null && prepared) player.time = Mathf.Clamp(seconds, 0f, DurationSeconds);
    }

    void OnDestroy()
    {
        if (player != null)
        {
            player.errorReceived -= OnPlayerError;
            player.prepareCompleted -= OnPrepared;
        }
    }
}
