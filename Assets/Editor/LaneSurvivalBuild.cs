using System.IO;
using UnityEditor;
using UnityEngine;

// Menu: Tools > Lane Survival. Sets the build scene list and builds a Windows player.
[InitializeOnLoad]
public static class LaneSurvivalBuild
{
    const string ScenePath = "Assets/Scenes/LaneSurvival.unity";
    const string OutputPath = "Builds/LaneSurvival/LaneSurvival.exe";

    // Keeps the build list on the game scene so every platform build starts the game.
    static LaneSurvivalBuild()
    {
        EditorApplication.delayCall += () =>
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 1 && scenes[0].path == ScenePath && scenes[0].enabled) return;
            SetBuildScene();
        };
    }

    [MenuItem("Tools/Lane Survival/Set Build Scene")]
    public static void SetBuildScene()
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "Lane Survival";
        Debug.Log("Build scene list set to " + ScenePath);
    }

    [MenuItem("Tools/Lane Survival/Build Windows")]
    public static void BuildWindows()
    {
        SetBuildScene();
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log("Build finished: " + report.summary.result + " -> " + OutputPath);
    }
}
