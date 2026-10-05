using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SpaceObstacleValidation
{
    private const string StageKey = "SpaceObstacles.ValidationStage";
    private static double deadline;
    private static Vector2 sawStart;
    private static float backgroundOffset;
    private static PlayerController originalPlayer;
    private static SpaceFallingPlatform platform;
    private static PlayerController player;
    private static Rigidbody2D body;
    private static bool heldReturn;
    static SpaceObstacleValidation() { EditorApplication.update += Tick; }

    public static void ReviewAndBuild()
    {
        IntegrationValidation.Build();
        Preview();
        Run();
    }

    public static void Run()
    {
        heldReturn = false;
        EditorSceneManager.OpenScene(IntegrationValidation.ScenePath);
        SpaceObstacleSetup.Validate();
        SessionState.SetInt(StageKey, 1);
        EditorApplication.EnterPlaymode();
    }

    private static void Check(bool passed, string message)
    {
        if (!passed) throw new InvalidOperationException(message);
        Debug.Log("[SpaceObstacleValidation] PASS " + message);
    }

    private static void PreparePlayer()
    {
        player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        body = player.GetComponent<Rigidbody2D>();
        player.enabled = false;
        player.GetComponent<HeightRecordHUD>().enabled = false;
        UnityEngine.Object.FindAnyObjectByType<CameraSeguidora>().enabled = false;
        body.gravityScale = 0f;
        body.linearVelocity = Vector2.zero;
        body.position = new Vector2(-11, -1);
        originalPlayer = player;
    }

    private static void SetStage(int stage, float seconds = 10f)
    {
        SessionState.SetInt(StageKey, stage);
        deadline = EditorApplication.timeSinceStartup + seconds;
    }

    private static void Tick()
    {
        int stage = SessionState.GetInt(StageKey, 0);
        if (stage == 0 || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (stage == 1)
            {
                if (UnityEngine.Object.FindAnyObjectByType<PlayerController>() == null) return;
                PreparePlayer();
                backgroundOffset = UnityEngine.Object.FindAnyObjectByType<Cenarioinfinito>()
                    .GetComponent<Renderer>().material.mainTextureOffset.x;
                platform = UnityEngine.Object.FindObjectsByType<SpaceFallingPlatform>()
                    .OrderBy(p => p.transform.position.y).First();
                var spikes = UnityEngine.Object.FindObjectsByType<SpaceRetractingSpikes>();
                foreach (var spike in spikes)
                {
                    spike.enabled = false;
                    spike.AnimateAt(0f);
                    Check(!spike.IsArmed && spike.GetComponents<Collider2D>().All(c => !c.enabled), "Retracted spikes have no lethal collision");
                    spike.AnimateAt(spike.safeTime + spike.warningTime * 0.5f);
                    Check(!spike.IsArmed, "Warning does not kill");
                    spike.AnimateAt(spike.safeTime + spike.warningTime + 0.3f);
                    Check(spike.IsArmed && spike.GetComponents<Collider2D>().All(c => c.enabled), "Extended spikes are lethal");
                    spike.AnimateAt(0f);
                }
                var saw = UnityEngine.Object.FindObjectsByType<UfoSawPatrol>().First();
                sawStart = saw.GetComponent<Rigidbody2D>().position;
                // Hit the underside first: it must not start the countdown.
                Bounds b = platform.GetComponent<BoxCollider2D>().bounds;
                body.position = new Vector2(b.center.x, b.min.y - player.GetComponent<Collider2D>().bounds.extents.y - 0.15f);
                body.linearVelocity = Vector2.up * 2f;
                SetStage(2, 3f);
            }
            else if (stage == 2 && EditorApplication.timeSinceStartup > deadline - 2.4f)
            {
                foreach (var background in UnityEngine.Object.FindObjectsByType<Cenarioinfinito>())
                {
                    var renderer = background.GetComponent<Renderer>();
                    Check(Mathf.Abs(renderer.material.mainTextureOffset.x - backgroundOffset) > 0.001f,
                        "Background keeps scrolling: " + background.name);
                    Check(renderer.bounds.size.x >= Camera.main.orthographicSize * 2f * Camera.main.aspect,
                        "Background covers viewport: " + background.name);
                }
                Check(platform.CurrentPhase == SpaceFallingPlatform.Phase.Ready, "Side/base contact does not collapse platform");
                var saw = UnityEngine.Object.FindObjectsByType<UfoSawPatrol>().First();
                Check(Vector2.Distance(sawStart, saw.GetComponent<Rigidbody2D>().position) > 0.015f, "UFO blade moves in physics");
                Bounds b = platform.GetComponent<BoxCollider2D>().bounds;
                body.position = new Vector2(b.center.x, b.max.y + player.GetComponent<Collider2D>().bounds.extents.y + 0.15f);
                body.linearVelocity = Vector2.down;
                body.gravityScale = 1f;
                SetStage(3);
            }
            else if (stage == 3 && platform.CurrentPhase == SpaceFallingPlatform.Phase.Warning)
            {
                Check(platform.GetComponent<BoxCollider2D>().enabled && platform.GetComponent<Animator>().enabled,
                    "Landing starts warning animation with solid support");
                body.gravityScale = 0f;
                body.position = new Vector2(-11, -1);
                body.linearVelocity = Vector2.zero;
                SetStage(4);
            }
            else if (stage == 4 && platform.CurrentPhase == SpaceFallingPlatform.Phase.Falling)
            {
                Check(!platform.GetComponent<BoxCollider2D>().enabled, "Falling support releases collision");
                SetStage(5);
            }
            else if (stage == 5)
            {
                if (platform.CurrentPhase == SpaceFallingPlatform.Phase.Returning && !heldReturn)
                {
                    body.position = platform.transform.position;
                    heldReturn = true;
                    SetStage(6, 2f);
                }
            }
            else if (stage == 6 && EditorApplication.timeSinceStartup > deadline - 0.8f)
            {
                Check(platform.CurrentPhase == SpaceFallingPlatform.Phase.Returning && !platform.GetComponent<BoxCollider2D>().enabled,
                    "Platform waits rather than materializing inside player");
                body.position = new Vector2(-11, -1);
                SetStage(7);
            }
            else if (stage == 7 && platform.CurrentPhase == SpaceFallingPlatform.Phase.Ready)
            {
                Check(platform.GetComponent<BoxCollider2D>().enabled && platform.GetComponent<SpriteRenderer>().color.a == 1f,
                    "Platform returns solid and visible");
                var spike = UnityEngine.Object.FindObjectsByType<SpaceRetractingSpikes>().First();
                spike.AnimateAt(0f);
                body.position = spike.GetComponent<SpriteRenderer>().bounds.center;
                body.linearVelocity = Vector2.zero;
                SetStage(8, 2f);
            }
            else if (stage == 8 && EditorApplication.timeSinceStartup > deadline - 1.5f)
            {
                Check(player != null && ReferenceEquals(player, originalPlayer), "Player can cross retracted blades");
                var spike = UnityEngine.Object.FindObjectsByType<SpaceRetractingSpikes>().First();
                spike.AnimateAt(spike.safeTime + spike.warningTime + 0.3f);
                body.position = spike.GetComponent<SpriteRenderer>().bounds.center;
                SetStage(9);
            }
            else if (stage == 9)
            {
                var current = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (current != null && !ReferenceEquals(current, originalPlayer))
                {
                    Check(true, "Extending spikes over overlapping player kills and reloads scene");
                    SpaceObstacleSetup.Validate();
                    Check(UnityEngine.Object.FindObjectsByType<SpaceFallingPlatform>()
                        .All(p => p.CurrentPhase == SpaceFallingPlatform.Phase.Ready), "Reload resets all falling platforms");
                    PreparePlayer();
                    var saw = UnityEngine.Object.FindObjectsByType<UfoSawPatrol>().First();
                    body.position = saw.GetComponent<Rigidbody2D>().position;
                    SetStage(10);
                }
            }
            else if (stage == 10)
            {
                var current = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (current != null && !ReferenceEquals(current, originalPlayer))
                {
                    Check(true, "Moving UFO blade kills and reloads scene");
                    Finish(0);
                }
            }
            if (stage != 1 && EditorApplication.timeSinceStartup > deadline)
                throw new TimeoutException("Obstacle validation timed out at stage " + stage);
        }
        catch (Exception error) { Debug.LogException(error); Finish(1); }
    }

    private static void Finish(int code)
    {
        SessionState.SetInt(StageKey, 0);
        Debug.Log("[SpaceObstacleValidation] FINISH " + code);
        EditorApplication.Exit(code);
    }

    public static void Preview()
    {
        EditorSceneManager.OpenScene(IntegrationValidation.ScenePath);
        Directory.CreateDirectory("Builds/SpaceObstacles");
        var camera = Camera.main;
        var follow = camera.GetComponent<CameraSeguidora>();
        follow.enabled = false;
        camera.orthographicSize = 6.8f;
        foreach (int height in new[] { 20, 40, 56, 60, 103 })
        {
            camera.transform.position = new Vector3(0, height, -10);
            var target = new RenderTexture(1280, 720, 24);
            camera.targetTexture = target;
            foreach (var background in UnityEngine.Object.FindObjectsByType<Cenarioinfinito>())
                background.RenderAt(camera, 0f);
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes("Builds/SpaceObstacles/trecho-" + height + ".png", image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target);
        }
        Debug.Log("[SpaceObstacleValidation] PREVIEW PASS");
    }
}
