using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PicoBenchmarkBuilder
{
    const string ScenePath = "Assets/Scenes/PicoVideoBenchmark.unity";
    const string OutputDir = @"D:\freebikeapk\Builds";
    const string ApkPath = OutputDir + @"\PicoVideoBenchmark.apk";

    [MenuItem("FreeBike/PICO/Build Benchmark APK")]
    public static void Build()
    {
        Directory.CreateDirectory(OutputDir);
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = false;
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("PicoBenchmarkBuilder: " + report.summary.result + " -> " + ApkPath);
    }
}
