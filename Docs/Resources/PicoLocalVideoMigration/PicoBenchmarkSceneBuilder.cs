using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PicoBenchmarkSceneBuilder
{
    const string ScenePath = "Assets/Scenes/PicoVideoBenchmark.unity";

    [MenuItem("FreeBike/PICO/Create Local Video Benchmark Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<HeadPoseDriver>();

        var benchGo = new GameObject("PicoVideoBenchmark");
        benchGo.AddComponent<PicoVideoBenchmark>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        bool present = false;
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path == ScenePath) present = true;
        if (!present)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        Debug.Log("PicoBenchmarkSceneBuilder: created " + ScenePath);
    }

    public static void BuildForBatch()
    {
        Build();
        EditorApplication.Exit(0);
    }
}
