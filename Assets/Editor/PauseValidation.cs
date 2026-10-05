using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PauseValidation
{
    private const string Stage = "PauseValidation.Stage";
    private static double deadline;
    private const string TestRecord = "PauseValidation.TemporaryRecord";
    static PauseValidation() { EditorApplication.update += Tick; }
    public static void Run()
    {
        EditorSceneManager.OpenScene(IntegrationValidation.ScenePath);
        Require(Resources.Load<Texture2D>("UI/Pause") != null, "Supplied pause artwork imported");
        Require(Resources.Load<AudioClip>("Audio/MenuSelect") != null && Resources.Load<AudioClip>("Audio/MenuConfirm") != null, "Menu sounds imported");
        SessionState.SetInt(Stage, 1);
        EditorApplication.EnterPlaymode();
    }
    private static void Tick()
    {
        int stage = SessionState.GetInt(Stage, 0);
        if (stage == 0 || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            var pause = player.GetComponent<PauseScreen>();
            if (stage == 1)
            {
                Require(pause != null, "Pause attached automatically to player");
                pause.Open();
                Require(PauseScreen.IsPaused && Time.timeScale == 0 && AudioListener.pause, "Pause freezes gameplay and audio");
                Require(pause.SelectedOption == 3, "Resume selected by default");
                bool uiBypassesPause = false;
                foreach (var source in player.GetComponents<AudioSource>()) if (source.ignoreListenerPause) uiBypassesPause = true;
                Require(uiBypassesPause, "UI audio remains available during pause");
                pause.Resume();
                Require(!PauseScreen.IsPaused && Time.timeScale == 1 && !AudioListener.pause, "Resume restores time and audio");
                Time.timeScale = .75f;
                AudioListener.pause = true;
                pause.Open(); pause.Resume();
                Require(Time.timeScale == .75f && AudioListener.pause, "Original time and audio states preserved");
                Time.timeScale = 1; AudioListener.pause = false;
                pause.Open();
                pause.Confirm();
                deadline = EditorApplication.timeSinceStartup + .4;
                SessionState.SetInt(Stage, 2);
            }
            else if (stage == 2 && EditorApplication.timeSinceStartup >= deadline)
            {
                Require(!PauseScreen.IsPaused && Time.timeScale == 1 && !AudioListener.pause, "BORA resumes after confirmation sound");
                pause.Open();
                Select(pause, 0); pause.Confirm();
                deadline = EditorApplication.timeSinceStartup + 1;
                SessionState.SetInt(Stage, 3);
            }
            else if (stage == 3 && EditorApplication.timeSinceStartup >= deadline)
            {
                Require(!PauseScreen.IsPaused && Time.timeScale == 1 && !AudioListener.pause && !player.IsDead, "Restart loads living player and restores audio");
                var hud = player.GetComponent<HeightRecordHUD>();
                typeof(HeightRecordHUD).GetField("recordKey", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, TestRecord);
                PlayerPrefs.SetInt(TestRecord, 123);
                pause.Open(); Select(pause, 1); pause.Confirm();
                deadline = EditorApplication.timeSinceStartup + 1;
                SessionState.SetInt(Stage, 4);
            }
            else if (stage == 4 && EditorApplication.timeSinceStartup >= deadline)
            {
                Require(!PlayerPrefs.HasKey(TestRecord), "Trophy clears only the selected record key");
                Require(!PauseScreen.IsPaused && Time.timeScale == 1 && !AudioListener.pause, "Record reset restarts cleanly");
                SessionState.SetInt(Stage, 0);
                Debug.Log("[PauseValidation] PASS");
                EditorApplication.Exit(0);
            }
        }
        catch (Exception error)
        {
            PlayerPrefs.DeleteKey(TestRecord);
            SessionState.SetInt(Stage, 0);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
    private static void Select(PauseScreen pause, int option)
    {
        typeof(PauseScreen).GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pause, new object[] { option });
    }
    private static void Require(bool value, string description)
    {
        if (!value) throw new InvalidOperationException(description);
        Debug.Log("[PauseValidation] " + description);
    }
}
