using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public static class CoinClimbValidation
{
    private const string Key = "CoinClimb.Validation";
    private static double deadline;
    private static int frame;
    private static ClimbCoin animated;
    static CoinClimbValidation() { EditorApplication.update += Tick; }
    public static void Run() { SessionState.SetInt(Key, 1); EditorApplication.EnterPlaymode(); }
    public static void Rebuild()
    {
        try
        {
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { MainMenuScreen.MenuScene, MainMenuScreen.GameScene }, locationPathName = "Builds/CoinClimb/Leproso.exe", target = BuildTarget.StandaloneWindows64 });
            Require(result.summary.result == BuildResult.Succeeded, "Final Windows build with transparent coin frames");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); Debug.Log("COIN CHECK PASS: " + message); }
    private static void Tick()
    {
        int stage = SessionState.GetInt(Key, 0);
        if (stage == 0 || EditorApplication.isCompiling) return;
        try
        {
            if (stage == 4 && !EditorApplication.isPlaying)
            {
                SessionState.SetInt(Key, 0);
                Directory.CreateDirectory("Builds/CoinClimb");
                var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { MainMenuScreen.MenuScene, MainMenuScreen.GameScene }, locationPathName = "Builds/CoinClimb/Leproso.exe", target = BuildTarget.StandaloneWindows64 });
                Require(result.summary.result == BuildResult.Succeeded, "Windows build");
                Debug.Log("COIN VALIDATION FINISH 0"); EditorApplication.Exit(0); return;
            }
            if (!EditorApplication.isPlaying) return;
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            var hud = player.GetComponent<HeightRecordHUD>();
            var coins = UnityEngine.Object.FindObjectsByType<ClimbCoin>();
            if (stage == 1)
            {
                Require(coins.Length >= 10, "Coins across original tower");
                Require(hud.Coins == 0, "New run starts with zero coins");
                Require(Resources.LoadAll<Sprite>("Coins").Length == 16, "All 16 supplied frames imported");
                Require(GameObject.Find("Coin icon").GetComponent<UnityEngine.UI.Image>().sprite != null, "HUD coin icon");
                Require(UnityEngine.Object.FindObjectsByType<CoinMovingPlatform>().Length >= 2, "Optional moving supports");
                Require(coins[0].TryCollect(player.GetComponent<Collider2D>()), "Collection by player");
                Require(!coins[0].TryCollect(player.GetComponent<Collider2D>()), "Duplicate collection ignored");
                Require(hud.Coins == 1, "HUD increments once");
                Require(!coins[1].TryCollect(coins[2].GetComponent<Collider2D>()), "Other colliders cannot collect");
                animated = coins[1]; frame = animated.FrameIndex; deadline = EditorApplication.timeSinceStartup + 15; SessionState.SetInt(Key, 2);
            }
            else if (stage == 2 && (animated.FrameIndex != frame || EditorApplication.timeSinceStartup > deadline))
            {
                Require(animated.FrameIndex != frame, "Frame animation advances");
                Time.timeScale = 0; frame = animated.FrameIndex; deadline = EditorApplication.timeSinceStartup + .22; SessionState.SetInt(Key, 3);
            }
            else if (stage == 3 && EditorApplication.timeSinceStartup > deadline)
            {
                frame = animated.FrameIndex; deadline = EditorApplication.timeSinceStartup + .22; SessionState.SetInt(Key, 5);
            }
            else if (stage == 5 && EditorApplication.timeSinceStartup > deadline)
            {
                Require(animated.FrameIndex == frame, "Animation freezes during pause");
                Time.timeScale = 1; SessionState.SetInt(Key, 4); EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception e) { SessionState.SetInt(Key, 0); Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
