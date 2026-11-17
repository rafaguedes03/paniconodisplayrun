using PanicoNoDisplay;
using UnityEngine;

/// <summary>
/// Versão independente (este projeto): abre o minijogo sozinho assim que o jogo inicia
/// e fecha o programa quando o jogador escolhe SAIR.
///
/// Este arquivo fica FORA da pasta PanicoNoDisplay de propósito: ao copiar a pasta
/// PanicoNoDisplay para dentro do jogo principal, ele não vai junto e o minijogo
/// continua abrindo só pela Central de Minigames / gatilho.
/// </summary>
public static class PNDStandaloneBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        PanicoNoDisplayLauncher.OnClosed -= OnClosed;
        PanicoNoDisplayLauncher.OnClosed += OnClosed;

        if (!PanicoNoDisplayLauncher.IsRunning)
            PanicoNoDisplayLauncher.Launch();
    }

    static void OnClosed(int money)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
