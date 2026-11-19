#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Tools → Pânico no Display → Gerar instalador (Windows)
///
/// Em um clique:
///  1. gera o jogo em Build/Windows;
///  2. monta o instalador simples em Build/Instalador (pasta Jogo + INSTALAR.bat + DESINSTALAR.bat)
///     e compacta em Build/PanicoNoDisplay_Instalador.zip — funciona em qualquer Windows, sem programa extra;
///  3. se o Inno Setup estiver instalado, gera também Build/PanicoNoDisplay_Setup.exe
///     (instalador de verdade, com "Avançar / Instalar" e desinstalação pelo Windows).
/// </summary>
public static class PNDBuild
{
    const string Scene = "Assets/Scenes/SampleScene.unity";
    const string ExeName = "PanicoNoDisplay.exe";
    const string GameName = "Panico no Display";
    const string Version = "1.0";
    const string IconPng = "Assets/Standalone/icone_jogo.png"; // ícone do .exe e dos atalhos
    const string IconIco = "Assets/Standalone/icone_jogo.ico"; // ícone do Setup.exe (Inno Setup)

    [MenuItem("Tools/Pânico no Display/Gerar instalador (Windows)")]
    public static void BuildInstaller()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        string buildDir = Path.Combine(root, "Build");
        string gameDir = Path.Combine(buildDir, "Windows");

        try
        {
            // ---------- 1) Jogo ----------
            EditorUtility.DisplayProgressBar("Pânico no Display", "Gerando o jogo...", 0.1f);
            if (!BuildGame(gameDir)) return;

            // ---------- 2) Instalador simples (.bat + .zip) ----------
            EditorUtility.DisplayProgressBar("Pânico no Display", "Montando o instalador...", 0.6f);
            string simpleDir = Path.Combine(buildDir, "Instalador");
            if (Directory.Exists(simpleDir)) Directory.Delete(simpleDir, true);
            CopyGame(gameDir, Path.Combine(simpleDir, "Jogo"));
            WriteText(Path.Combine(simpleDir, "INSTALAR.bat"), InstallBat, false);
            WriteText(Path.Combine(simpleDir, "DESINSTALAR.bat"), UninstallBat, false);
            WriteText(Path.Combine(simpleDir, "LEIA-ME.txt"), ReadmeTxt, false);

            EditorUtility.DisplayProgressBar("Pânico no Display", "Compactando o .zip...", 0.75f);
            string zip = Path.Combine(buildDir, "PanicoNoDisplay_Instalador.zip");
            if (File.Exists(zip)) File.Delete(zip);
            bool zipOk = Run("powershell", "-NoProfile -ExecutionPolicy Bypass -Command \"Compress-Archive -Path '"
                + Path.Combine(simpleDir, "*") + "' -DestinationPath '" + zip + "' -Force\"", out string zipLog);
            if (!zipOk) Debug.LogWarning("[Pânico no Display] Não deu para criar o .zip (a pasta Build/Instalador está pronta mesmo assim):\n" + zipLog);

            // ---------- 3) Setup.exe (Inno Setup) ----------
            EditorUtility.DisplayProgressBar("Pânico no Display", "Gerando o Setup.exe...", 0.85f);
            string iss = Path.Combine(buildDir, "PanicoNoDisplay.iss");
            WriteText(iss, InnoScript(gameDir, buildDir, Path.Combine(root, IconIco).Replace('/', '\\')), true);
            string iscc = FindInnoSetup();
            bool setupOk = false;
            string issLog = "";
            if (iscc != null)
            {
                string setupExe = Path.Combine(buildDir, "PanicoNoDisplay_Setup.exe");
                if (File.Exists(setupExe)) File.Delete(setupExe); // nunca fica um Setup.exe velho para trás
                setupOk = Run(iscc, "/Q \"" + iss + "\"", out issLog);
                if (!setupOk) Debug.LogWarning("[Pânico no Display] O Inno Setup deu erro:\n" + issLog);
            }

            // ---------- Resumo ----------
            var msg = new StringBuilder();
            msg.AppendLine("Pronto! Arquivos na pasta Build:");
            msg.AppendLine();
            if (setupOk) msg.AppendLine("• PanicoNoDisplay_Setup.exe — instalador completo (recomendado)");
            if (zipOk) msg.AppendLine("• PanicoNoDisplay_Instalador.zip — descompacte e rode INSTALAR.bat");
            else msg.AppendLine("• Instalador\\ — rode INSTALAR.bat");
            if (iscc != null && !setupOk)
            {
                msg.AppendLine();
                msg.AppendLine("O Setup.exe NÃO foi gerado. Erro do Inno Setup:");
                msg.AppendLine(issLog.Length > 400 ? issLog.Substring(0, 400) : issLog);
            }
            if (iscc == null)
            {
                msg.AppendLine();
                msg.AppendLine("Para gerar também um Setup.exe, instale o Inno Setup (grátis, jrsoftware.org) e rode este menu de novo.");
            }
            Debug.Log("[Pânico no Display] " + msg);
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Pânico no Display", msg.ToString(), "OK");
            EditorUtility.RevealInFinder(setupOk ? Path.Combine(buildDir, "PanicoNoDisplay_Setup.exe") : zipOk ? zip : simpleDir);
        }
        catch (Exception e)
        {
            Debug.LogError("[Pânico no Display] Falha ao gerar o instalador: " + e);
            EditorUtility.DisplayDialog("Pânico no Display", "Falha ao gerar o instalador:\n" + e.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>Só o jogo (pasta Build/Windows), sem instalador.</summary>
    [MenuItem("Tools/Pânico no Display/Gerar só o jogo (Windows)")]
    public static void BuildWindowsOnly()
    {
        string gameDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Build", "Windows");
        if (BuildGame(gameDir))
            EditorUtility.RevealInFinder(Path.Combine(gameDir, ExeName));
    }

    static bool BuildGame(string gameDir)
    {
        PlayerSettings.companyName = "PanicoNoDisplay";
        PlayerSettings.productName = GameName;
        PlayerSettings.bundleVersion = Version;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = false;
        ApplyIcon();

        if (Directory.Exists(gameDir)) Directory.Delete(gameDir, true);
        var options = new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = Path.Combine(gameDir, ExeName),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[Pânico no Display] Jogo gerado em: " + gameDir);
            return true;
        }
        EditorUtility.ClearProgressBar();
        Debug.LogError("[Pânico no Display] A geração do jogo falhou (" + report.summary.result + "). Veja os erros acima no Console. " +
                       "Se falar em 'Windows Build Support', instale esse módulo pelo Unity Hub (Installs → engrenagem → Add modules).");
        EditorUtility.DisplayDialog("Pânico no Display", "A geração do jogo falhou. Veja o Console.", "OK");
        return false;
    }

    /// <summary>Usa o ícone do personagem no .exe (em vez do ícone padrão da Unity).</summary>
    static void ApplyIcon()
    {
        var ti = AssetImporter.GetAtPath(IconPng) as TextureImporter;
        if (ti == null)
        {
            Debug.LogWarning("[Pânico no Display] Ícone não encontrado em " + IconPng + " (o jogo vai usar o ícone da Unity).");
            return;
        }
        if (ti.textureCompression != TextureImporterCompression.Uncompressed || ti.maxTextureSize < 1024 || ti.mipmapEnabled)
        {
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 1024;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPng);
        if (tex != null)
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { tex }, IconKind.Any);
    }

    /// <summary>Copia o jogo sem as pastas de depuração que o Unity marca como "DoNotShip".</summary>
    static void CopyGame(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var dir in Directory.GetDirectories(from, "*", SearchOption.AllDirectories))
        {
            if (dir.Contains("DoNotShip")) continue;
            Directory.CreateDirectory(dir.Replace(from, to));
        }
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            if (file.Contains("DoNotShip")) continue;
            File.Copy(file, file.Replace(from, to), true);
        }
    }

    static void WriteText(string path, string text, bool utf8Bom)
    {
        // .bat precisa de quebras de linha do Windows e sem acentos
        text = text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        File.WriteAllText(path, text, utf8Bom ? new UTF8Encoding(true) : new UTF8Encoding(false));
    }

    static string FindInnoSetup()
    {
        string[] candidates =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Inno Setup 6", "ISCC.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Inno Setup 6", "ISCC.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Inno Setup 6", "ISCC.exe"),
        };
        foreach (var c in candidates)
            if (File.Exists(c)) return c;
        return null;
    }

    static bool Run(string exe, string args, out string log)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using (var p = Process.Start(psi))
            {
                string o = p.StandardOutput.ReadToEnd();
                string e = p.StandardError.ReadToEnd();
                p.WaitForExit();
                log = o + e;
                return p.ExitCode == 0;
            }
        }
        catch (Exception ex)
        {
            log = ex.Message;
            return false;
        }
    }

    // ==================== Textos do instalador ====================

    const string InstallBat = @"@echo off
setlocal
title Instalar Panico no Display
set ""DEST=%LOCALAPPDATA%\PanicoNoDisplay""
echo.
echo  Instalando Panico no Display em:
echo  %DEST%
echo.
if exist ""%DEST%"" rmdir /s /q ""%DEST%""
xcopy ""%~dp0Jogo"" ""%DEST%\"" /e /i /q /y >nul
if errorlevel 1 (
  echo  ERRO ao copiar os arquivos. Feche o jogo se ele estiver aberto e tente de novo.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -Command ""$w = New-Object -ComObject WScript.Shell; $exe = Join-Path $env:LOCALAPPDATA 'PanicoNoDisplay\PanicoNoDisplay.exe'; foreach ($d in @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs'))) { $l = $w.CreateShortcut((Join-Path $d 'Panico no Display.lnk')); $l.TargetPath = $exe; $l.WorkingDirectory = (Split-Path $exe); $l.IconLocation = $exe; $l.Save() }""
echo  Pronto! Use o atalho ""Panico no Display"" na Area de Trabalho.
echo.
pause
";

    const string UninstallBat = @"@echo off
setlocal
title Desinstalar Panico no Display
set ""DEST=%LOCALAPPDATA%\PanicoNoDisplay""
if exist ""%DEST%"" rmdir /s /q ""%DEST%""
powershell -NoProfile -ExecutionPolicy Bypass -Command ""foreach ($d in @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs'))) { Remove-Item (Join-Path $d 'Panico no Display.lnk') -ErrorAction SilentlyContinue }""
echo.
echo  Panico no Display foi removido. (O ranking TOP 5 fica guardado; para zerar,
echo  apague a pasta %USERPROFILE%\AppData\LocalLow\PanicoNoDisplay)
echo.
pause
";

    const string ReadmeTxt = @"PANICO NO DISPLAY - CORRIDA INFINITA

COMO INSTALAR
  1. Descompacte esta pasta (se veio em .zip).
  2. De dois cliques em INSTALAR.bat.
  3. Abra pelo atalho ""Panico no Display"" na Area de Trabalho.

Nao precisa de administrador nem de internet.
Para remover: DESINSTALAR.bat.

Se aparecer o aviso azul ""O Windows protegeu o computador"",
clique em ""Mais informacoes"" e depois em ""Executar assim mesmo"".

Para jogar sem instalar: abra Jogo\PanicoNoDisplay.exe.
";

    static string InnoScript(string gameDir, string outDir, string iconIco)
    {
        return @"; Gerado por Tools -> Panico no Display -> Gerar instalador (Windows)
; Para recompilar sozinho: abra este arquivo no Inno Setup e aperte Compile.

[Setup]
AppId=PanicoNoDisplay
AppName=" + GameName + @"
AppVersion=" + Version + @"
AppPublisher=Rafael Lima Guedes
DefaultDirName={localappdata}\PanicoNoDisplay
DefaultGroupName=" + GameName + @"
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=" + outDir + @"
OutputBaseFilename=PanicoNoDisplay_Setup
" + (File.Exists(iconIco) ? "SetupIconFile=" + iconIco + "\n" : "") + @"UninstallDisplayIcon={app}\" + ExeName + @"
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: ""ptbr""; MessagesFile: ""compiler:Languages\BrazilianPortuguese.isl""

[Tasks]
Name: ""desktopicon""; Description: ""Criar atalho na Área de Trabalho""; GroupDescription: ""Atalhos:""

[Files]
Source: """ + gameDir + @"\*""; DestDir: ""{app}""; Excludes: ""*DoNotShip*""; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: ""{group}\" + GameName + @"""; Filename: ""{app}\" + ExeName + @"""
Name: ""{autodesktop}\" + GameName + @"""; Filename: ""{app}\" + ExeName + @"""; Tasks: desktopicon

[Run]
Filename: ""{app}\" + ExeName + @"""; Description: ""Jogar agora""; Flags: nowait postinstall skipifsilent
";
    }
}
#endif
