#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Gera a versão para Windows (pasta Build/Windows) com um clique:
/// Tools → Pânico no Display → Gerar instalável (Windows).
/// </summary>
public static class PNDBuild
{
    const string Scene = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Pânico no Display/Gerar instalável (Windows)")]
    public static void BuildWindows()
    {
        PlayerSettings.companyName = "PanicoNoDisplay";
        PlayerSettings.productName = "Panico no Display";
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = false;

        string root = Directory.GetParent(Application.dataPath).FullName;
        string exe = Path.Combine(root, "Build", "Windows", "PanicoNoDisplay.exe");

        var options = new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = exe,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[Pânico no Display] Instalável gerado em: " + exe);
            EditorUtility.RevealInFinder(exe);
        }
        else
        {
            Debug.LogError("[Pânico no Display] A geração falhou (" + report.summary.result + "). Veja os erros acima no Console.");
        }
    }
}
#endif
