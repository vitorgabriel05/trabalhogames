using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class MapPolishValidation
{
    private const string Key = "MapPolish.Validation";
    private static PlayerController player;
    private static Rigidbody2D body;
    private static BouncePad[] pads;
    private static int padIndex;
    private static double deadline;
    private static float deathTime;
    private static Quaternion rotation;
    private static float cameraY;
    private static int captureIndex;
    static MapPolishValidation() { EditorApplication.update += Tick; }

    public static void Run()
    {
        EditorSceneManager.OpenScene(IntegrationValidation.ScenePath);
        Validate();
        Preview();
        SessionState.SetInt(Key, 1);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Tools/Validar correcoes do mapa e morte")]
    public static void Validate()
    {
        var maps = UnityEngine.Object.FindObjectsByType<Tilemap>();
        var lower = maps.Single(m => m.name == "Tilemap");
        for (int y = -6; y <= -3; y++)
        for (int x = -16; x <= 15; x++)
            if (!lower.HasTile(new Vector3Int(x, y, 0))) throw new InvalidOperationException("Initial terrain hole: " + x + "," + y);
        Check(true, "Continuous initial grass and soil across both screen edges");
        foreach (var spike in UnityEngine.Object.FindObjectsByType<Espeto>())
        {
            var sprite = spike.GetComponent<SpriteRenderer>();
            var opaque = MapPolishSetup.OpaqueBounds(sprite.sprite);
            float bottom = spike.transform.TransformPoint(opaque.min).y;
            Check(HasSupport(maps, spike.transform.position.x, bottom), "Supported spike at " + spike.transform.position);
        }
        foreach (var pad in UnityEngine.Object.FindObjectsByType<BouncePad>())
        {
            var box = pad.GetComponent<BoxCollider2D>();
            Check(Mathf.Abs(pad.transform.position.x) < 12f && pad.GetComponent<SpriteRenderer>().sortingOrder >= 8,
                "Visible pad " + pad.name);
            Check(HasSupport(maps, box.bounds.center.x, box.bounds.min.y), "Pad on surface " + pad.name);
        }
        var deathSound = Resources.Load<AudioClip>("Audio/UndertaleDeath");
        Check(deathSound != null && deathSound.length >= 2.2f, "Death audio from supplied Undertale reference is imported");
        SpaceObstacleSetup.Validate();
        Debug.Log("[MapPolishValidation] STATIC PASS");
    }

    private static bool HasSupport(Tilemap[] maps, float x, float bottom)
    {
        foreach (var support in UnityEngine.Object.FindObjectsByType<BoxCollider2D>())
            if (!support.isTrigger && support.gameObject.layer == LayerMask.NameToLayer("Ground") &&
                Mathf.Abs(support.bounds.max.y - bottom) < .015f && support.bounds.min.x <= x && support.bounds.max.x >= x)
                return true;
        foreach (var map in maps)
        foreach (var cell in map.cellBounds.allPositionsWithin)
        {
            if (!map.HasTile(cell)) continue;
            var bounds = MapPolishSetup.TileBounds(map, cell);
            if (Mathf.Abs(bounds.max.y - bottom) < 0.015f && bounds.min.x <= x && bounds.max.x >= x) return true;
        }
        return false;
    }

    private static void Prepare()
    {
        player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        player.enabled = false;
        player.GetComponent<HeightRecordHUD>().enabled = false;
        player.GetComponent<PlayerLimite>().enabled = false;
        Camera.main.GetComponent<CameraSeguidora>().enabled = false;
        body = player.GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        pads = UnityEngine.Object.FindObjectsByType<BouncePad>().OrderBy(p => p.name).ToArray();
    }

    private static void PlaceOnPad()
    {
        var box = pads[padIndex].GetComponent<BoxCollider2D>();
        float bottom = player.GetComponent<Collider2D>().bounds.min.y - body.position.y;
        body.position = new Vector2(box.bounds.center.x, box.bounds.max.y - bottom + 0.08f);
        body.linearVelocity = Vector2.down * 2f;
        Camera.main.transform.position = new Vector3(0f, body.position.y + 1f, -10f);
        Physics2D.SyncTransforms();
        deadline = EditorApplication.timeSinceStartup + 4f;
    }

    private static void Tick()
    {
        int stage = SessionState.GetInt(Key, 0);
        if (stage == 0 || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (stage == 1)
            {
                if (UnityEngine.Object.FindAnyObjectByType<PlayerController>() == null) return;
                Prepare(); padIndex = 0; PlaceOnPad(); SessionState.SetInt(Key, 2);
            }
            else if (stage == 2 && body.linearVelocity.y > 10f)
            {
                Check(true, "Top collision bounces " + pads[padIndex].name);
                if (++padIndex < pads.Length) PlaceOnPad();
                else
                {
                    var spike = UnityEngine.Object.FindObjectsByType<Espeto>().First(s => s.GetComponent<SpaceRetractingSpikes>() == null);
                    body.position = spike.GetComponent<SpriteRenderer>().bounds.center;
                    body.linearVelocity = Vector2.zero;
                    Camera.main.transform.position = new Vector3(0, body.position.y + 1f, -10);
                    Physics2D.SyncTransforms();
                    deadline = EditorApplication.timeSinceStartup + 6f;
                    SessionState.SetInt(Key, 3);
                }
            }
            else if (stage == 3 && player.IsDead)
            {
                deathTime = Time.unscaledTime;
                rotation = player.transform.rotation;
                cameraY = Camera.main.transform.position.y;
                captureIndex = 0;
                Check(!body.simulated && !player.GetComponent<Collider2D>().enabled, "Death disables physics and collision");
                player.Die(); player.Bounce(18f);
                Check(body.linearVelocity == Vector2.zero, "Repeated hazard and bounce do not interrupt death");
                SessionState.SetInt(Key, 4);
            }
            else if (stage == 4)
            {
                float elapsed = Time.unscaledTime - deathTime;
                var current = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if (current != player)
                {
                    Check(elapsed >= PlayerDeathEffect.Duration - 0.2f, "Scene waits for soul and fragments before restarting");
                    Prepare();
                    Check(!player.IsDead && body.simulated && player.GetComponent<SpriteRenderer>().color.a == 1f, "Reload restores live character");
                    Camera.main.transform.position = new Vector3(0, 20, -10);
                    body.position = new Vector2(0, 10);
                    body.linearVelocity = Vector2.down * 2f;
                    player.enabled = true;
                    deadline = EditorApplication.timeSinceStartup + 6f;
                    SessionState.SetInt(Key, 5);
                }
                else if (captureIndex < 6 && elapsed >= new[] {0.08f, 0.22f, 0.50f, 1.75f, 2.75f, 3.40f}[captureIndex])
                {
                    Capture("morte-" + captureIndex);
                    var effect = player.GetComponent<PlayerDeathEffect>();
                    if (captureIndex >= 2)
                    {
                        Check(Quaternion.Angle(rotation, player.transform.rotation) < 0.01f, "Soul death keeps character pose stable");
                        Check(Mathf.Abs(Camera.main.transform.position.y - cameraY) < 0.001f, "Camera stays fixed during death");
                        Check(player.GetComponent<SpriteRenderer>().color.a < 0.01f, "Original pose dissolves into soul");
                    }
                    if (captureIndex == 1) Check(effect.SoundStarted && effect.DeathSound != null, "Reference sound starts before crack");
                    if (captureIndex == 2) Check(effect.CurrentPhase == PlayerDeathEffect.Phase.Broken && effect.VisibleFragments == 0, "Cracked soul holds before shattering");
                    if (captureIndex == 3) Check(effect.CurrentPhase == PlayerDeathEffect.Phase.Shattered && effect.VisibleFragments == 6, "Six red fragments scatter with second sound");
                    if (captureIndex == 4) Check(effect.FragmentAlpha < 1f && effect.FragmentAlpha > 0f, "Soul fragments fade gradually");
                    if (captureIndex == 5) Check(effect.FragmentAlpha < 0.2f, "Fragments finish fading before restart");
                    captureIndex++;
                }
            }
            else if (stage == 5 && player.IsDead)
            {
                Check(player.GetComponent<SpriteRenderer>().bounds.max.y > Camera.main.transform.position.y - Camera.main.orthographicSize,
                    "Fall death remains visible inside viewport");
                deadline = EditorApplication.timeSinceStartup + 6f;
                SessionState.SetInt(Key, 6);
            }
            else if (stage == 6 && UnityEngine.Object.FindAnyObjectByType<PlayerController>() != player)
            {
                Check(true, "Fall death also restarts after animation");
                Finish(0);
            }
            if (stage != 1 && EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Stage " + stage + ", pad " + padIndex);
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }

    private static void Check(bool pass, string message)
    {
        if (!pass) throw new InvalidOperationException("[MapPolishValidation] " + message);
        Debug.Log("[MapPolishValidation] PASS " + message);
    }
    private static void Finish(int result)
    {
        SessionState.SetInt(Key, 0);
        Debug.Log("[MapPolishValidation] FINISH " + result);
        EditorApplication.Exit(result);
    }

    public static void Preview()
    {
        Camera.main.GetComponent<CameraSeguidora>().enabled = false;
        Camera.main.orthographicSize = 6.8f;
        foreach (int height in new[] {0, 12, 18, 26, 38, 44, 100, 111})
        {
            Camera.main.transform.position = new Vector3(0, height, -10);
            Capture("mapa-" + height);
        }
        EditorSceneManager.OpenScene(IntegrationValidation.ScenePath);
    }
    private static void Capture(string name)
    {
        Directory.CreateDirectory("Builds/MapPolish");
        var camera = Camera.main;
        var target = new RenderTexture(1440, 720, 24);
        camera.targetTexture = target;
        var death = UnityEngine.Object.FindAnyObjectByType<PlayerDeathEffect>();
        if (death != null) death.FitBackdrop(camera);
        foreach (var background in UnityEngine.Object.FindObjectsByType<Cenarioinfinito>()) background.RenderAt(camera, 0f);
        camera.Render();
        RenderTexture.active = target;
        var texture = new Texture2D(1440, 720, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, 1440, 720), 0, 0); texture.Apply();
        if (death != null && death.CurrentPhase >= PlayerDeathEffect.Phase.Broken)
        {
            foreach (var corner in new[] {new Vector2Int(1, 1), new Vector2Int(1438, 1), new Vector2Int(1, 718), new Vector2Int(1438, 718)})
            {
                var color = texture.GetPixel(corner.x, corner.y);
                Check(color.r + color.g + color.b < 0.01f, "Death backdrop covers wide viewport corner " + corner);
            }
        }
        File.WriteAllBytes("Builds/MapPolish/" + name + ".png", texture.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(target);
    }
}
