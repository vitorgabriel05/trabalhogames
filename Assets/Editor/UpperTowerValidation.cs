using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class UpperTowerValidation
{
    private const string Key = "UpperTower.Validation";
    private const string TemporaryRecord = "UpperTower.Validation.Record";
    private static double deadline;
    private static PlayerController player;
    private static Rigidbody2D body;
    private static float cameraBefore;
    private static int jumpIndex;
    private static SimulationMode2D previousMode;
    static UpperTowerValidation() { EditorApplication.update += Tick; }

    public static void Run()
    {
        EditorSceneManager.OpenScene(MainMenuScreen.GameScene);
        UpperTowerSetup.Validate();
        Preview();
        SessionState.SetInt(Key, 1);
        deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        int stage = SessionState.GetInt(Key, 0);
        if (stage == 0 || EditorApplication.isCompiling) return;
        try
        {
            if (stage == 10 && !EditorApplication.isPlaying)
            {
                SessionState.SetInt(Key, 0);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { MainMenuScreen.MenuScene, MainMenuScreen.GameScene },
                    locationPathName = "Builds/CoinClimb/Leproso.exe", target = BuildTarget.StandaloneWindows64 });
                Check(report.summary.result == BuildResult.Succeeded, "Build Windows atualizado");
                PlayerPrefs.DeleteKey(TemporaryRecord);
                Debug.Log("UPPER VALIDATION FINISH 0");
                EditorApplication.Exit(0); return;
            }
            if (!EditorApplication.isPlaying) return;
            if (stage != 1 && EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Stage " + stage);
            if (stage == 1)
            {
                player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (player == null) return;
                // Prevent validation teleports from changing the user's height record.
                var hud = player.GetComponent<HeightRecordHUD>();
                typeof(HeightRecordHUD).GetField("recordKey", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, TemporaryRecord);
                hud.enabled = false;
                body = player.GetComponent<Rigidbody2D>();
                player.enabled = false;
                player.GetComponent<PlayerLimite>().enabled = false;
                foreach (var hazard in UnityEngine.Object.FindObjectsByType<SawHazard>()) hazard.gameObject.SetActive(false);
                foreach (var spike in UnityEngine.Object.FindObjectsByType<Espeto>()) spike.gameObject.SetActive(false);
                foreach (var motion in UnityEngine.Object.FindObjectsByType<CoinMovingPlatform>())
                {
                    motion.enabled = false;
                    motion.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                }
                previousMode = Physics2D.simulationMode; Physics2D.simulationMode = SimulationMode2D.Script;
                jumpIndex = 0; SetStage(2);
            }
            else if (stage == 2)
            {
                var route = GameObject.Find(UpperTowerSetup.RootName).GetComponentsInChildren<BoxCollider2D>()
                    .Where(b => b.name.StartsWith("Rota ")).OrderBy(b => b.transform.position.y).ToArray();
                // Include the entry jump from the original last ledge.
                Check(JumpTo(jumpIndex == 0 ? null : route[jumpIndex - 1], route[jumpIndex], true) ||
                    JumpTo(jumpIndex == 0 ? null : route[jumpIndex - 1], route[jumpIndex], false), "Salto alcancavel " + jumpIndex);
                if (++jumpIndex == route.Length)
                {
                    var landing = GameObject.Find(UpperTowerSetup.RootName).GetComponentsInChildren<BoxCollider2D>()
                        .Where(b => b.name.StartsWith("Desvio de moedas ")).OrderBy(b => b.transform.position.y).First();
                    Place(landing.bounds.center.x, landing.bounds.max.y + 12f);
                    body.linearVelocity = Vector2.down * 60f;
                    bool landed = false;
                    for (int frame = 0; frame < 30; frame++)
                    {
                        Physics2D.Simulate(.02f);
                        if (player.GetComponent<Collider2D>().IsTouching(landing) && Mathf.Abs(body.linearVelocity.y) < .1f)
                        { landed = true; break; }
                    }
                    Check(landed, "Queda rapida de 60 unidades por segundo aterrissa sem atravessar o apoio");
                    Physics2D.simulationMode = previousMode;
                    body.simulated = false; body.gravityScale = 0;
                    player.enabled = true;
                    Place(12f, 270f);
                    SetStage(3, .5);
                }
            }
            else if (stage == 3 && Ready())
            {
                cameraBefore = Camera.main.transform.position.y;
                Place(12f, 180f); body.linearVelocity = Vector2.down * 30f;
                SetStage(4, .5);
            }
            else if (stage == 4 && Ready())
            {
                Check(!player.IsDead && Camera.main.transform.position.y < cameraBefore - 70f, "Descida rapida de 90 unidades sem morte pela camera");
                Check(Camera.main.WorldToViewportPoint(player.transform.position).y >= .19f, "Jogador continua visivel na descida");
                var coin = GameObject.Find(UpperTowerSetup.RootName).GetComponentsInChildren<ClimbCoin>()
                    .First(c => c.transform.position.y < 160f && c.GetComponent<SpriteRenderer>().enabled);
                Place(coin.transform.position.x, coin.transform.position.y);
                Check(coin.TryCollect(player.GetComponent<Collider2D>()), "Moeda abaixo do pico pode ser coletada");
                Check(player.GetComponent<HeightRecordHUD>().Coins >= 1, "Contador registra coleta na volta");
                var pause = player.GetComponent<PauseScreen>();
                pause.Open(); cameraBefore = Camera.main.transform.position.y;
                SetStage(5, .3);
            }
            else if (stage == 5 && Ready())
            {
                Check(PauseScreen.IsPaused && Mathf.Approximately(Camera.main.transform.position.y, cameraBefore), "Pausa congela camera");
                player.GetComponent<PauseScreen>().Resume();
                Place(12f, player.FallDeathY - 2f); body.linearVelocity = Vector2.down * 2;
                SetStage(6, .3);
            }
            else if (stage == 6 && Ready())
            {
                Check(player.IsDead, "Queda abaixo da base continua fatal");
                SceneManager.LoadScene(MainMenuScreen.GameScene); SetStage(7, .5);
            }
            else if (stage == 7 && Ready())
            {
                player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                Check(player != null && !player.IsDead && Time.timeScale == 1 && !PauseScreen.IsPaused, "Reinicio restaura jogo e camera");
                var pause = player.GetComponent<PauseScreen>(); pause.Open();
                typeof(PauseScreen).GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pause, new object[] { 2 });
                pause.Confirm(); SetStage(8, 1);
            }
            else if (stage == 8 && Ready())
            {
                Check(SceneManager.GetActiveScene().path == MainMenuScreen.MenuScene && MainMenuScreen.IsActive && !PauseScreen.IsPaused,
                    "Dar o fora retorna a tela inicial sem sair do Play Mode");
                var menu = UnityEngine.Object.FindAnyObjectByType<MainMenuScreen>();
                Check(menu != null, "Menu restaurado");
                if (menu.CurrentPage == MainMenuScreen.Page.Intro) menu.FinishIntro();
                Check(menu.CurrentPage == MainMenuScreen.Page.Title, "Tela de titulo e audio do menu restaurados");
                SceneManager.LoadScene(MainMenuScreen.GameScene); SetStage(9, .5);
            }
            else if (stage == 9 && Ready())
            {
                player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                var saw = GameObject.Find(UpperTowerSetup.RootName).GetComponentInChildren<UfoSawPatrol>();
                player.GetComponent<Rigidbody2D>().position = saw.GetComponent<Rigidbody2D>().position;
                Physics2D.SyncTransforms(); SetStage(11, .3);
            }
            else if (stage == 11 && Ready())
            {
                Check(player.IsDead, "OVNI novo continua letal");
                SessionState.SetInt(Key, 10); EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception e)
        {
            Physics2D.simulationMode = previousMode;
            SessionState.SetInt(Key, 0); PlayerPrefs.DeleteKey(TemporaryRecord);
            Debug.LogException(e); Debug.Log("UPPER VALIDATION FINISH 1"); EditorApplication.Exit(1);
        }
    }

    private static bool JumpTo(BoxCollider2D from, BoxCollider2D to, bool delayedHorizontal)
    {
        float feetOffset = body.position.y - player.GetComponent<Collider2D>().bounds.min.y;
        Vector2 origin = from != null ? new Vector2(from.bounds.center.x, from.bounds.max.y) : new Vector2(-3f, 123f);
        origin.x += Mathf.Sign(origin.x - to.bounds.center.x) * (from != null ? Mathf.Min(.65f, from.bounds.extents.x * .5f) : .6f);
        Place(origin.x, origin.y + feetOffset + .04f);
        body.linearVelocity = Vector2.up * 8f;
        bool secondJump = false;
        for (int step = 0; step < 180; step++)
        {
            var shape = player.GetComponent<Collider2D>();
            if (!secondJump && body.linearVelocity.y < 1f)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, 8f); secondJump = true;
            }
            float direction = Mathf.Sign(to.bounds.center.x - body.position.x);
            bool aboveTarget = shape.bounds.min.y > to.bounds.max.y + .1f;
            float horizontal = (!delayedHorizontal || aboveTarget) && Mathf.Abs(to.bounds.center.x - body.position.x) > .08f ? direction * 6f : 0;
            body.linearVelocity = new Vector2(horizontal, body.linearVelocity.y);
            Physics2D.Simulate(.02f);
            if (secondJump && step > 45 && shape.IsTouching(to) && Mathf.Abs(body.linearVelocity.y) < .1f &&
                shape.bounds.min.y >= to.bounds.max.y - .12f) return true;
        }
        Debug.Log("Jump strategy retry " + to.name + " body " + body.position + " target " + to.bounds);
        return false;
    }

    private static void Place(float x, float y)
    {
        body.position = new Vector2(x, y); player.transform.position = new Vector3(x, y, 0);
        body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
    }
    private static void SetStage(int stage, double delay = 0)
    {
        SessionState.SetInt(Key, stage); deadline = EditorApplication.timeSinceStartup + delay + 60;
        SessionState.SetFloat(Key + ".Ready", (float)(EditorApplication.timeSinceStartup + delay));
    }
    private static bool Ready() { return EditorApplication.timeSinceStartup >= SessionState.GetFloat(Key + ".Ready", 0); }
    private static void Check(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); Debug.Log("UPPER CHECK PASS: " + message); }

    public static void Preview()
    {
        Directory.CreateDirectory("Builds/CoinClimb");
        var camera = Camera.main;
        var follow = camera.GetComponent<CameraSeguidora>(); follow.enabled = false;
        foreach (int height in new[] { 127, 170, 212, 256, 295 })
        {
            camera.transform.position = new Vector3(0, height, -10);
            var target = new RenderTexture(1280, 720, 24); camera.targetTexture = target;
            camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
            File.WriteAllBytes("Builds/CoinClimb/altura-" + height + ".png", image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
        }
        // Do not carry preview camera positions/settings into Play Mode.
        EditorSceneManager.OpenScene(MainMenuScreen.GameScene);
    }
}
