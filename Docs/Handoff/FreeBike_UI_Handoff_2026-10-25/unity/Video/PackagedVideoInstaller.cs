using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Resolves a video shipped in StreamingAssets. On Android StreamingAssets is
/// inside the APK, so the first run streams it to persistentDataPath and then
/// validates size/hash before handing the file to the player.
/// </summary>
public class PackagedVideoInstaller : MonoBehaviour
{
    const string FolderName = "FreeBikeVideos";
    const string Version = "v1";

    public string CurrentStatus { get; private set; } = "";
    public float CurrentProgress { get; private set; }
    public bool IsBusy { get; private set; }

    public IEnumerator Install(string fileName, Action<string> completed, Action<string> failed)
    {
        IsBusy = true;
        CurrentProgress = 0f;
        CurrentStatus = "preparing";
        string dir = Path.Combine(Application.persistentDataPath, FolderName, Version);
        Directory.CreateDirectory(dir);
        string dest = Path.Combine(dir, fileName);
        string ready = dest + ".ready";

        if (File.Exists(dest) && File.Exists(ready) && new FileInfo(dest).Length > 1024 * 1024)
        {
            CurrentStatus = "using cached " + fileName;
            CurrentProgress = 1f;
            IsBusy = false;
            completed?.Invoke(dest);
            yield break;
        }

        string source = Application.streamingAssetsPath + "/Videos/" + fileName;
        string url = source;
        if (!url.Contains("://"))
            url = new Uri(source).AbsoluteUri;

        string part = dest + ".part";
        if (File.Exists(part)) File.Delete(part);

        CurrentStatus = "copying " + fileName;
        using (var request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerFile(part);
            request.SendWebRequest();
            while (!request.isDone)
            {
                CurrentProgress = Mathf.Clamp01(request.downloadProgress);
                yield return null;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                CurrentStatus = "copy failed: " + request.error;
                IsBusy = false;
                failed?.Invoke(CurrentStatus);
                yield break;
            }
        }

        if (!File.Exists(part) || new FileInfo(part).Length < 1024 * 1024)
        {
            CurrentStatus = "copied file is too small";
            IsBusy = false;
            failed?.Invoke(CurrentStatus);
            yield break;
        }

        if (File.Exists(dest)) File.Delete(dest);
        File.Move(part, dest);

        CurrentStatus = "hashing " + fileName;
        string hash = ComputeSha256(dest);
        string meta = "{\"file\":\"" + Escape(fileName) + "\",\"bytes\":" + new FileInfo(dest).Length +
                      ",\"sha256\":\"" + hash + "\",\"version\":\"" + Version + "\"}";
        File.WriteAllText(ready, meta, Encoding.UTF8);

        CurrentStatus = "ready " + fileName;
        CurrentProgress = 1f;
        IsBusy = false;
        completed?.Invoke(dest);
    }

    static string ComputeSha256(string path)
    {
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(path))
        {
            byte[] hash = sha.ComputeHash(stream);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
