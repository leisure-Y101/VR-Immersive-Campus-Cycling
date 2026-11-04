using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildWindowsCampusRide
{
    const string ScenePath = "Assets/Scenes/panoTest.unity";
    const string OutputDir = @"D:\freebike\Releases\Windows\CampusRideDemo";
    const string ExePath = OutputDir + @"\freebike.exe";

    [MenuItem("FreeBike/Build Windows Campus Ride")]
    public static void Build()
    {
        Directory.CreateDirectory(OutputDir);
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = ExePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("BuildWindowsCampusRide: " + report.summary.result + " -> " + ExePath);
    }
}
