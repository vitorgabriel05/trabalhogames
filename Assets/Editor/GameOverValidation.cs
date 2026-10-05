using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class GameOverValidation
{
    private const string Key = "GameOverValidation.Stage";
    private static double deadline;
    static GameOverValidation() { EditorApplication.update += Tick; }

    public static void Run()
    {
        EditorSceneManager.OpenScene(IntegrationValidation.ScenePath);
        Require(Resources.Load<Texture2D>("UI/GameOver") != null, "Game over image imported");
        Require(Resources.Load<Font>("UI/GameOverFont") != null, "Pixel font imported");
        foreach (var background in UnityEngine.Object.FindObjectsByType<Cenarioinfinito>())
        {
            Vector3 position = background.transform.position, scale = background.transform.localScale;
            background.RenderAt(Camera.main, 3f);
            Require(background.transform.position == position && background.transform.localScale == scale,
                "Background preserves authored position and scale");
        }
        Require(Resources.Load<AudioClip>("Audio/GameOverMusic") != null, "Game over music imported");
        for (int i = 1; i <= 6; i++)
            Require(Resources.Load<AudioClip>("Audio/GameOverVoice" + i) != null, "Voice " + i + " imported");
        Require(Resources.Load<Texture2D>("UI/GameOver").isReadable, "Title can be animated");
        SessionState.SetInt(Key, 1);
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        int stage = SessionState.GetInt(Key, 0);
        if (stage == 0 || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (stage == 1)
            {
                if (player == null) return;
                player.Die();
                Require(player.GetComponent<GameOverScreen>() == null, "Menu waits for death animation");
                deadline = EditorApplication.timeSinceStartup + PlayerController.DeathDuration + .5;
                SessionState.SetInt(Key, 2);
            }
            else if (stage == 2 && EditorApplication.timeSinceStartup >= deadline)
            {
                var screen = player.GetComponent<GameOverScreen>();
                Require(screen != null && screen.IsVisible, "Game over visible after death");
                Require(player.IsDead && Time.timeScale == 0f, "Gameplay stays paused at menu");
                Require(screen.ContinueSelected, "Sim selected by default");
                Require(GameOverScreen.DeathCount == 1 && screen.SelectedVoiceIndex >= 0 && screen.SelectedVoiceIndex < 6,
                    "Death counted and random voice selected");
                var sources = player.GetComponents<AudioSource>();
                bool musicPlaying = false, voiceAssigned = false;
                foreach (var source in sources)
                {
                    if (source.clip == null) continue;
                    if (source.clip.name == "GameOverMusic") musicPlaying = source.isPlaying && !source.loop;
                    if (source.clip.name.StartsWith("GameOverVoice")) voiceAssigned = !source.loop;
                }
                Require(musicPlaying && voiceAssigned, "One-shot intro and separate voice source during pause");
                Require(Resources.Load<AudioClip>("Audio/GameOverAmbient") != null, "Ambient continuation imported");
                Require(player.GetComponent<PlayerDeathEffect>().CurrentPhase == PlayerDeathEffect.Phase.Finished,
                    "Death effect finished before menu");
                System.IO.Directory.CreateDirectory("Builds/GameOver");
                ScreenCapture.CaptureScreenshot("Builds/GameOver/game-over.png");
                deadline = EditorApplication.timeSinceStartup + 6;
                SessionState.SetInt(Key, 3);
            }
            else if (stage == 3 && EditorApplication.timeSinceStartup >= deadline)
            {
                bool introStopped = false, ambiencePlaying = false;
                foreach (var source in player.GetComponents<AudioSource>())
                {
                    if (source.clip == null) continue;
                    if (source.clip.name == "GameOverMusic") introStopped = !source.isPlaying && !source.loop;
                    if (source.clip.name == "GameOverAmbient") ambiencePlaying = source.isPlaying && source.loop;
                }
                Require(introStopped && ambiencePlaying, "Intro finishes once and ambient continuation takes over while time is frozen");
                player.GetComponent<GameOverScreen>().GetType().GetMethod("Confirm", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(player.GetComponent<GameOverScreen>(), null);
                deadline = EditorApplication.timeSinceStartup + .8;
                SessionState.SetInt(Key, 4);
            }
            else if (stage == 4 && EditorApplication.timeSinceStartup >= deadline)
            {
                Require(player != null && !player.IsDead && player.GetComponent<GameOverScreen>() == null,
                    "Sim reloads scene with a living player");
                Require(Time.timeScale == 1f, "Restart restores gameplay time");
                Require(GameOverScreen.DeathCount == 1, "Death counter survives restart");
                int previousVoice = (int)typeof(GameOverScreen).GetField("previousVoiceIndex", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                var nextScreen = player.gameObject.AddComponent<GameOverScreen>();
                nextScreen.Show();
                Require(GameOverScreen.DeathCount == 2 && nextScreen.SelectedVoiceIndex != previousVoice,
                    "Second death chooses a different random voice");
                SessionState.SetInt(Key, 0);
                Debug.Log("[GameOverValidation] PASS");
                EditorApplication.Exit(0);
            }
        }
        catch (Exception error)
        {
            SessionState.SetInt(Key, 0);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log("[GameOverValidation] " + message);
    }
}
