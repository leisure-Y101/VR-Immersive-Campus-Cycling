using System;

/// <summary>
/// Minimal playback contract so a future AVPro/OES backend can replace the
/// Unity VideoPlayer backend without changing the benchmark/app logic.
/// </summary>
public interface IVideoBackend
{
    bool IsPrepared { get; }
    bool IsPlaying { get; }
    float DurationSeconds { get; }
    float TimeSeconds { get; }
    int Width { get; }
    int Height { get; }
    string LastError { get; }

    event Action<string> Error;

    void Prepare(string url);
    void Play();
    void Pause();
    void Stop();
    void SetSpeed(float speed);
    void Seek(float seconds);
}
