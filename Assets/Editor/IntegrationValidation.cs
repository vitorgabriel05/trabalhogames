using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class IntegrationValidation
{
    public const string ScenePath = "Assets/Project/Scenes/TorredasPlataformas.unity";

    [MenuItem("Tools/Validar integracao do mapa")]
    public static void Validate()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var objects = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Select(t => t.gameObject).ToArray();
        Require(objects.All(go => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) == 0),
            "Nenhum script ausente na cena principal");
        var players = objects.Select(go => go.GetComponent<PlayerController>()).Where(p => p != null).ToArray();
        Require(players.Length == 1, "Um unico jogador na cena");
        var player = players[0];
        Require(player.GetComponent<Rigidbody2D>() != null && player.GetComponent<Collider2D>() != null,
            "Jogador com corpo e colisao");
        Require(player.GetComponent<PlayerLimite>() != null && player.GetComponent<HeightRecordHUD>() != null,
            "Limites laterais e HUD preservados");
        var playerData = new SerializedObject(player);
        Require(playerData.FindProperty("groundCheck").objectReferenceValue != null,
            "Detector de chao conectado");
        Require(playerData.FindProperty("audioSource").objectReferenceValue is AudioSource &&
            playerData.FindProperty("jumpSound").objectReferenceValue is AudioClip,
            "Audio do pulo conectado");
        var hudData = new SerializedObject(player.GetComponent<HeightRecordHUD>());
        Require(hudData.FindProperty("recordSound").objectReferenceValue is AudioClip &&
            hudData.FindProperty("pixelFont").objectReferenceValue is Font, "Som e fonte do recorde conectados");
        var camera = objects.Select(go => go.GetComponent<CameraSeguidora>()).Single(c => c != null);
        Require(new SerializedObject(camera).FindProperty("player").objectReferenceValue == player.transform,
            "Camera conectada ao jogador");
        var maps = objects.Select(go => go.GetComponent<Tilemap>()).Where(t => t != null).ToArray();
        Require(maps.Length >= 2 && maps.Any(t => t.cellBounds.size.y >= 123), "Mapa ampliado presente");
        int tiles = 0;
        foreach (var map in maps)
        {
            foreach (var position in map.cellBounds.allPositionsWithin)
            {
                if (!map.HasTile(position)) continue;
                tiles++;
                Require(map.GetSprite(position) != null, "Sprite do tile " + position + " em " + map.name);
            }
        }
        Require(tiles > 100, "Mapa possui tiles importados: " + tiles);
        Require(maps.Any(t => t.GetComponent<TilemapCollider2D>() != null), "Colisao do terreno presente");
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
        Require(scenes.Length > 0 && scenes[0].path == ScenePath, "Build inicia na cena do mapa ampliado");
        Debug.Log("[IntegrationValidation] PASS: mapa, jogador, camera, audio e HUD validados.");
    }

    public static void Build()
    {
        Validate();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = "Builds/Integration/Torre.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        Require(report.summary.result == BuildResult.Succeeded,
            "Build Windows: " + report.summary.result + ", erros: " + report.summary.totalErrors);
        Debug.Log("[IntegrationValidation] BUILD PASS");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[IntegrationValidation] " + message);
    }
}
