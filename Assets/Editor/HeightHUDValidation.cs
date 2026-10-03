using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class HeightHUDValidation
{
    private const string Stage = "HeightHUDValidation.stage";
    private const string ScenePath = "Assets/Project/Scenes/TorredasPlataformas.unity";
    private const string Key = "HeightRecord.v1." + ScenePath;
    static HeightHUDValidation() { EditorApplication.update += Tick; }
    public static void Run()
    {
        SessionState.SetBool("HeightHUDValidation.had", PlayerPrefs.HasKey(Key));
        SessionState.SetInt("HeightHUDValidation.old", PlayerPrefs.GetInt(Key));
        PlayerPrefs.DeleteKey(Key);
        EditorSceneManager.OpenScene(ScenePath);
        SessionState.SetInt(Stage, 1);
        EditorApplication.EnterPlaymode();
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Debug.Log("[HeightHUDValidation] PASS " + message);
    }
    private static void Tick()
    {
        int stage = SessionState.GetInt(Stage, 0);
        if (stage == 0 || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            var hud = UnityEngine.Object.FindAnyObjectByType<HeightRecordHUD>();
            if (hud == null) return;
            if (stage == 1)
            {
                Check(hud.Height == 0 && hud.Record == 0, "Fresh attempt starts at zero");
                var player = hud.GetComponent<PlayerController>();
                player.enabled = false;
                hud.GetComponent<Rigidbody2D>().simulated = false;
                float y = hud.transform.position.y;
                var data = new SerializedObject(hud);
                var clip = data.FindProperty("recordSound").objectReferenceValue as AudioClip;
                Check(clip != null && clip.name == "NewRecord8Bit" && clip.length > 0 && clip.length < 2, "8-bit record fanfare imported");
                hud.transform.position += Vector3.up * 10.1f;
                hud.SendMessage("LateUpdate");
                Check(hud.Height == 10 && hud.Record == 10, "Ascent updates height and record");
                var field = typeof(HeightRecordHUD).GetField("celebrationUntil", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                float until = (float)field.GetValue(hud);
                Check(until > Time.unscaledTime, "Record celebration starts");
                hud.transform.position = new Vector3(hud.transform.position.x, y + 4.1f, 0);
                hud.SendMessage("LateUpdate");
                Check(hud.Height == 4 && hud.Record == 10, "Fall decreases only current height");
                hud.transform.position = new Vector3(hud.transform.position.x, y - 2, 0);
                hud.SendMessage("LateUpdate");
                Check(hud.Height == 0 && hud.Record == 10, "Height is clamped below start");
                hud.transform.position = new Vector3(hud.transform.position.x, y + 12.1f, 0);
                hud.SendMessage("LateUpdate");
                Check(hud.Record == 12 && (float)field.GetValue(hud) == until, "Further records do not repeat celebration");
                hud.enabled = false;
                Check(PlayerPrefs.GetInt(Key) == 12, "Record saved at attempt end");
                SessionState.SetInt(Stage, 2);
                SceneManager.LoadScene(ScenePath);
            }
            else
            {
                Check(hud.Height == 0 && hud.Record == 12, "Scene restart resets height and retains record");
                Check(UnityEngine.Object.FindObjectsByType<HeightRecordHUD>(FindObjectsInactive.Exclude).Length == 1, "No duplicate HUD after restart");
                Finish(0);
            }
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }
    private static void Finish(int exitCode)
    {
        SessionState.SetInt(Stage, 0);
        if (SessionState.GetBool("HeightHUDValidation.had", false))
            PlayerPrefs.SetInt(Key, SessionState.GetInt("HeightHUDValidation.old", 0));
        else PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
        EditorApplication.Exit(exitCode);
    }
}
