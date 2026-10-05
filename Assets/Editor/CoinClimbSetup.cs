using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class CoinClimbSetup
{
    [InitializeOnLoadMethod]
    private static void ReturnFromExperiment()
    {
        if (Application.isBatchMode) return;
        EditorApplication.delayCall += () => {
            if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.EndsWith("AventuraExperimental.unity"))
                EditorSceneManager.OpenScene(MainMenuScreen.GameScene);
        };
    }
    [MenuItem("Tools/Moedas/Abrir torre com moedas")]
    public static void OpenTower() { EditorSceneManager.OpenScene(MainMenuScreen.GameScene); }
    [MenuItem("Tools/Moedas/Aplicar desafios na torre")]
    public static void Apply()
    {
        AssetDatabase.Refresh();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Coins" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 120;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        var scene = EditorSceneManager.OpenScene(MainMenuScreen.GameScene);
        var old = GameObject.Find("Moedas e desafios da subida");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        var root = new GameObject("Moedas e desafios da subida").transform;
        var maps = UnityEngine.Object.FindObjectsByType<Tilemap>();
        var candidates = maps.SelectMany(map => Cells(map)
            .Where(c => map.HasTile(c) && !map.HasTile(c + Vector3Int.up))
            .Select(c => new { map, cell = c, world = map.GetCellCenterWorld(c) }))
            .Where(c => c.world.y >= 7 && c.world.y <= 119 && Mathf.Abs(c.world.x) < 12)
            .OrderBy(c => c.world.y).ThenBy(c => c.world.x).ToArray();
        float lastY = -100;
        int count = 0, moving = 0;
        var platformSource = UnityEngine.Object.FindObjectsByType<SpaceFallingPlatform>().First();
        foreach (var candidate in candidates)
        {
            if (candidate.world.y - lastY < 4.5f) continue;
            int direction = count % 2 == 0 ? -1 : 1;
            if (candidate.map.HasTile(candidate.cell + new Vector3Int(direction, 0, 0))) continue;
            Vector3 reward = candidate.world + new Vector3(direction * .8f, 1.2f, 0);
            // Coins must be visible, outside solid terrain and away from lethal blades.
            if (!Clear(reward, maps, .45f)) continue;
            Transform parent = root;
            if (candidate.world.y > 24 && count % 3 == 2)
            {
                Vector3 ledge = candidate.world + new Vector3(direction * 1.8f, .45f, 0);
                if (Mathf.Abs(ledge.x) > 12.5f || !Clear(ledge, maps, .9f) || !Clear(ledge + Vector3.up * 1.5f, maps, .5f)) continue;
                var go = new GameObject("Apoio movel opcional " + moving++);
                go.transform.SetParent(root); go.transform.position = ledge;
                go.layer = platformSource.gameObject.layer;
                var art = go.AddComponent<SpriteRenderer>(); art.sprite = platformSource.idleSprite;
                art.sharedMaterial = platformSource.GetComponent<SpriteRenderer>().sharedMaterial;
                art.color = new Color(.45f, .85f, 1); art.sortingOrder = 5;
                go.transform.localScale = new Vector3(1.1f / art.sprite.bounds.size.x, .35f / art.sprite.bounds.size.y, 1);
                var box = go.AddComponent<BoxCollider2D>(); box.size = art.sprite.bounds.size;
                var motion = go.AddComponent<CoinMovingPlatform>(); motion.period = Mathf.Lerp(4.8f, 2.8f, candidate.world.y / 120f);
                parent = go.transform;
                reward = ledge + Vector3.up * .9f;
            }
            var coin = new GameObject("Moeda de risco " + count);
            coin.transform.SetParent(parent); coin.transform.position = reward;
            // Keep coin size independent of the moving support's visual scale.
            coin.transform.localScale = new Vector3(1 / parent.lossyScale.x, 1 / parent.lossyScale.y, 1);
            var renderer = coin.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Coins/coin_00.png");
            renderer.sharedMaterial = platformSource.GetComponent<SpriteRenderer>().sharedMaterial;
            renderer.sortingOrder = 15;
            var trigger = coin.AddComponent<CircleCollider2D>(); trigger.radius = .24f; trigger.isTrigger = true;
            coin.AddComponent<ClimbCoin>();
            lastY = candidate.world.y; count++;
        }
        int guards = 0;
        var sawSource = UnityEngine.Object.FindObjectsByType<UfoSawPatrol>().First();
        foreach (var coin in root.GetComponentsInChildren<ClimbCoin>().Where(c => c.transform.position.y > 35))
        {
            if (guards >= 5) break;
            Vector3 position = coin.transform.position + new Vector3(coin.transform.position.x < 0 ? 1.65f : -1.65f, .2f, 0);
            if (!Clear(position, maps, 1f)) continue;
            var guard = UnityEngine.Object.Instantiate(sawSource.gameObject, position, Quaternion.identity, root);
            guard.name = "OVNI guarda de moeda " + guards++;
            var patrol = guard.GetComponent<UfoSawPatrol>();
            patrol.travel = new Vector2(0, .45f); patrol.phase = 0;
        }
        foreach (var patrol in UnityEngine.Object.FindObjectsByType<UfoSawPatrol>())
            patrol.period = Mathf.Lerp(5.8f, 3.2f, Mathf.InverseLerp(55, 120, patrol.transform.position.y));
        foreach (var trap in UnityEngine.Object.FindObjectsByType<SpaceRetractingSpikes>())
        {
            float d = Mathf.InverseLerp(30, 120, trap.transform.position.y);
            trap.safeTime = Mathf.Lerp(2.8f, 1.25f, d);
            trap.warningTime = Mathf.Lerp(.75f, .5f, d);
            trap.phaseOffset = Mathf.Repeat(trap.transform.position.y * .37f, 2f);
        }
        foreach (var platform in UnityEngine.Object.FindObjectsByType<SpaceFallingPlatform>())
            platform.warningTime = Mathf.Lerp(1.1f, .55f, Mathf.InverseLerp(30, 120, platform.transform.position.y));
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        if (count < 10 || moving < 2) throw new Exception("Too few accessible rewards: " + count + ", moving " + moving);
        Debug.Log("COIN SETUP PASS: " + count + " coins, " + moving + " optional moving ledges, " + guards + " extra guards");
    }
    private static System.Collections.Generic.IEnumerable<Vector3Int> Cells(Tilemap map)
    {
        foreach (var cell in map.cellBounds.allPositionsWithin) yield return cell;
    }
    private static bool Clear(Vector3 position, Tilemap[] maps, float radius)
    {
        foreach (var map in maps)
        for (float x = -radius; x <= radius; x += radius)
        for (float y = -radius; y <= radius; y += radius)
            if (map.HasTile(map.WorldToCell(position + new Vector3(x,y,0)))) return false;
        foreach (var hazard in UnityEngine.Object.FindObjectsByType<SawHazard>())
            if (Vector2.Distance(hazard.transform.position, position) < radius + .8f) return false;
        return true;
    }
    public static void Run()
    {
        try { Apply(); CoinClimbValidation.Run(); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
