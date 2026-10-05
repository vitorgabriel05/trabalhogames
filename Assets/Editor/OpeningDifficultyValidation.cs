using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Video;

[InitializeOnLoad]
public static class OpeningDifficultyValidation
{
    const string Request = "Temp/OpeningDifficulty.request";
    const string Result = "Builds/CoinClimb/opening-difficulty-result.txt";
    const string Key = "OpeningDifficulty.Stage";
    static double deadline;
    static OpeningDifficultyValidation() { EditorApplication.update += Tick; }
    [MenuItem("Tools/Torre/Validar abertura e dificuldade")]
    public static void Run() { File.WriteAllText(Request, "run"); }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); Debug.Log("OPENING CHECK PASS: " + message); }
    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        int stage = SessionState.GetInt(Key, 0);
        try
        {
            if (stage == 0)
            {
                if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete(Request);
                EditorSceneManager.SaveOpenScenes();
                UpperTowerSetup.Apply();
                Check(Resources.Load<VideoClip>("Video/Opening") != null, "Opening imported");
                var menuScene = EditorSceneManager.OpenScene(MainMenuScreen.MenuScene, OpenSceneMode.Additive);
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(menuScene);
                SessionState.SetInt(Key, 1);
                EditorApplication.EnterPlaymode();
            }
            else if (stage == 1 && EditorApplication.isPlaying)
            {
                var menu = UnityEngine.Object.FindAnyObjectByType<MainMenuScreen>();
                if (menu == null) return;
                Check(menu.CurrentPage == MainMenuScreen.Page.Intro, "Opening precedes menu");
                Check(MainMenuScreen.IsActive && Time.timeScale == 0 && AudioListener.pause, "Simulation and background audio blocked");
                foreach (var root in UnityEngine.SceneManagement.SceneManager.GetSceneByPath(MainMenuScreen.GameScene).GetRootGameObjects())
                    Check(!root.activeInHierarchy, "Gameplay root inactive: " + root.name);
                foreach (var source in menu.GetComponents<AudioSource>())
                    Check(source.clip == null || source.clip.name != "TitleTheme" || !source.isPlaying, "Title music waits for opening");
                deadline = EditorApplication.timeSinceStartup + 40;
                SessionState.SetInt(Key, 2);
            }
            else if (stage == 2)
            {
                var menu = UnityEngine.Object.FindAnyObjectByType<MainMenuScreen>();
                var video = menu.GetComponent<VideoPlayer>();
                if (video.texture == null || video.frame < 2)
                { if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Opening decode timeout"); return; }
                Check(video.isPlaying && !video.isLooping, "Opening decodes and plays once");
                ScreenCapture.CaptureScreenshot("Builds/CoinClimb/abertura.png");
                menu.FinishIntro();
                Check(menu.CurrentPage == MainMenuScreen.Page.Title && video.audioOutputMode == VideoAudioOutputMode.None, "Skip reaches title and removes embedded audio");
                menu.Show(MainMenuScreen.Page.Menu); menu.Confirm();
                deadline = EditorApplication.timeSinceStartup + 40;
                SessionState.SetInt(Key, 3);
            }
            else if (stage == 3)
            {
                if (UnityEngine.Object.FindAnyObjectByType<MainMenuScreen>() != null)
                { if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Start timeout"); return; }
                Check(!MainMenuScreen.IsActive && Time.timeScale == 1 && !AudioListener.pause, "Start restores gameplay");
                Check(UnityEngine.Object.FindAnyObjectByType<PlayerController>().isActiveAndEnabled, "Player enabled after Start");
                UpperTowerSetup.Validate();
                SessionState.SetInt(Key, 4); EditorApplication.ExitPlaymode();
            }
            else if (stage == 4 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetInt(Key, 0);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { MainMenuScreen.MenuScene, MainMenuScreen.GameScene },
                    locationPathName = "Builds/CoinClimb/Leproso.exe", target = BuildTarget.StandaloneWindows64 });
                Check(report.summary.result == BuildResult.Succeeded, "Windows build");
                File.WriteAllText(Result, "PASS: opening, decode, skip, gameplay/audio isolation, difficulty, Windows build");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }
        catch (Exception e)
        {
            SessionState.SetInt(Key, 0); Debug.LogException(e);
            Directory.CreateDirectory("Builds/CoinClimb"); File.WriteAllText(Result, "FAIL: " + e);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
