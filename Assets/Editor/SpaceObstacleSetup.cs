using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class SpaceObstacleSetup
{
    private const string ArtPath = "Assets/Project/Sprites/SpaceObstacles";
    private const string PrefabPath = "Assets/Project/Prefabs/SpaceObstacles";
    private static Material spriteMaterial;

    [MenuItem("Tools/Obstaculos espaciais/Aplicar na cena principal")]
    public static void Apply()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(IntegrationValidation.ScenePath);
        if (scene.GetRootGameObjects().Any(g => g.name == "Obstaculos espaciais"))
            throw new InvalidOperationException("Os obstaculos ja foram aplicados; edite os componentes existentes.");
        Directory.CreateDirectory(ArtPath);
        Directory.CreateDirectory(PrefabPath);
        var shipSprite = CreateShip();
        var beamSprite = CreateBeam();
        var pixel = SaveSprite("Pixel", 1, 1, new[] { new Color32(255, 255, 255, 255) }, 1f);
        var root = new GameObject("Obstaculos espaciais").transform;
        var saws = UnityEngine.Object.FindObjectsByType<SpriteRenderer>()
            .Where(s => s.name.StartsWith("Serra", StringComparison.Ordinal) && s.transform.position.y >= 60f).OrderBy(s => s.transform.position.y).ToArray();
        spriteMaterial = saws.First().sharedMaterial;
        foreach (var saw in saws)
        {
            if (saw.GetComponent<SawHazard>() == null) saw.gameObject.AddComponent<SawHazard>();
            var blade = saw.GetComponent<CircleCollider2D>();
            if (blade == null) blade = saw.gameObject.AddComponent<CircleCollider2D>();
            blade.isTrigger = true;
            blade.radius = Mathf.Min(saw.sprite.bounds.extents.x, saw.sprite.bounds.extents.y) * 0.88f;
            blade.offset = saw.sprite.bounds.center;
            var body = saw.GetComponent<Rigidbody2D>();
            if (body == null) body = saw.gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var patrol = saw.gameObject.AddComponent<UfoSawPatrol>();
            float difficulty = Mathf.InverseLerp(60f, 120f, saw.transform.position.y);
            patrol.period = Mathf.Lerp(5.8f, 3.8f, difficulty);
            // Keep the author's blade height and restrict travel to its local gap.
            float amplitude = Mathf.Lerp(1.05f, 1.65f, difficulty);
            float clearance = ClearTravel(saw, amplitude);
            if (clearance < 0.35f)
            {
                // Two original blades sit against the underside of a platform.
                // Lower them slightly so the new patrol has an actual flight gap.
                saw.transform.position += Vector3.down * 0.45f;
                clearance = ClearTravel(saw, amplitude);
            }
            patrol.travel = Vector2.right * clearance;
            patrol.phase = 0f;
            saw.sortingOrder = 5;
            var ship = Visual("Mini OVNI - piloto alien", saw.transform, shipSprite, new Vector3(0, 0.46f, 0));
            patrol.ship = ship.transform;
            var beam = Visual("Campo trator", saw.transform, beamSprite, new Vector3(0, 0.23f, 0));
            beam.sortingOrder = 3;
            patrol.engine = beam;
            if (saw == saws.First()) PrefabUtility.SaveAsPrefabAsset(saw.gameObject, PrefabPath + "/SerraOVNI.prefab");
        }

        var spikes = UnityEngine.Object.FindObjectsByType<Espeto>().Where(s => s.transform.position.y >= 35f).OrderBy(s => s.transform.position.y).ToArray();
        foreach (var spike in spikes)
        {
            // A stationary parent lets the blades retract without moving the base.
            var housing = new GameObject("Espetos mecanicos - " + spike.transform.position.y.ToString("F0")).transform;
            housing.SetParent(root);
            housing.position = spike.transform.position;
            spike.transform.SetParent(housing, true);
            var renderer = spike.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = 6;
            foreach (var collider in spike.GetComponents<Collider2D>()) collider.isTrigger = true;
            var trap = spike.gameObject.AddComponent<SpaceRetractingSpikes>();
            trap.blades = spike.transform;
            trap.phaseOffset = 0f;
            trap.safeTime = spike.transform.position.y < 15f ? 2.8f : 2.2f;
            float width = renderer.bounds.size.x;
            float bottom = renderer.bounds.min.y - housing.position.y;
            var rail = Visual("Base de titanio", housing, pixel, new Vector3(0, bottom - 0.05f, 0));
            rail.transform.localScale = new Vector3(width + 0.12f, 0.12f, 1);
            rail.color = new Color(0.32f, 0.45f, 0.62f);
            var light = Visual("Aviso de ativacao", housing, pixel, new Vector3(0, bottom - 0.015f, 0));
            light.transform.localScale = new Vector3(width * 0.65f, 0.035f, 1);
            light.sortingOrder = 7;
            light.color = Color.cyan;
            trap.warningLight = light;
            if (spike == spikes.First()) PrefabUtility.SaveAsPrefabAsset(housing.gameObject, PrefabPath + "/EspetosRetrateis.prefab");
        }

        var maps = UnityEngine.Object.FindObjectsByType<Tilemap>();
        var lower = maps.Single(m => m.name == "Tilemap");
        var upper = maps.Single(m => m.name == "Tilemap 2");
        ReplacePlatform(upper, root, pixel, 37, 4, 5, 1f);
        ReplacePlatform(upper, root, pixel, 52, -5, -3, 0.9f);
        ReplacePlatform(upper, root, pixel, 69, 4, 5, 0.85f);
        ReplacePlatform(upper, root, pixel, 87, -6, -5, 0.8f);
        ReplacePlatform(upper, root, pixel, 107, -3, -2, 0.75f);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
        Debug.Log("[SpaceObstacleSetup] APPLY PASS: " + saws.Length + " OVNIs, " + spikes.Length + " armadilhas, 5 plataformas.");
    }

    private static float ClearTravel(SpriteRenderer saw, float desired)
    {
        float radius = saw.bounds.extents.x;
        var maps = UnityEngine.Object.FindObjectsByType<Tilemap>();
        // Sweep the entire blade, not just its center, against occupied tiles.
        for (float distance = 0.1f; distance <= desired; distance += 0.1f)
        foreach (float sign in new[] { -1f, 1f })
        {
            Bounds swept = new Bounds(saw.transform.position + Vector3.right * distance * sign,
                new Vector3(radius * 2f + 0.12f, radius * 2f + 0.12f, 1));
            foreach (var map in maps)
            foreach (var cell in map.cellBounds.allPositionsWithin)
                if (map.HasTile(cell) && swept.Intersects(TileBounds(map, cell))) return Mathf.Max(0.1f, distance - 0.2f);
        }
        return desired;
    }

    private static Bounds TileBounds(Tilemap map, Vector3Int cell)
    {
        var sprite = map.GetSprite(cell);
        var bounds = sprite.bounds;
        var matrix = map.transform.localToWorldMatrix * Matrix4x4.Translate(map.CellToLocalInterpolated(cell + map.tileAnchor)) * map.GetTransformMatrix(cell);
        var result = new Bounds(matrix.MultiplyPoint3x4(bounds.min), Vector3.zero);
        result.Encapsulate(matrix.MultiplyPoint3x4(bounds.max));
        return result;
    }

    private static void ReplacePlatform(Tilemap map, Transform parent, Sprite pixel, int y, int firstX, int lastX, float warning)
    {
        Bounds bounds = new Bounds();
        for (int x = firstX; x <= lastX; x++)
        {
            var cell = new Vector3Int(x, y, 0);
            if (!map.HasTile(cell)) throw new InvalidOperationException("Plataforma ausente: " + map.name + " " + cell);
            if (x == firstX) bounds = TileBounds(map, cell); else bounds.Encapsulate(TileBounds(map, cell));
        }
        var idle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Pixel Adventure 1/Assets/Traps/Falling Platforms/Off.png");
        var go = new GameObject("Plataforma instavel - " + y);
        go.layer = LayerMask.NameToLayer("Ground");
        go.transform.SetParent(parent);
        float height = 0.4f;
        go.transform.position = new Vector3(bounds.center.x, bounds.max.y - height * 0.5f, 0);
        go.transform.localScale = new Vector3(bounds.size.x / idle.bounds.size.x, height / idle.bounds.size.y, 1);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = idle;
        renderer.sharedMaterial = spriteMaterial;
        renderer.sortingOrder = 4;
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(idle.bounds.size.x, idle.bounds.size.y * 0.55f);
        collider.offset = new Vector2(0, idle.bounds.size.y * 0.225f);
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Pixel Adventure 1/Assets/Traps/Falling Platforms/On (32x10)_0.controller");
        animator.enabled = false;
        var platform = go.AddComponent<SpaceFallingPlatform>();
        platform.idleSprite = idle;
        platform.warningTime = warning;
        var light = Visual("Estabilizador de energia", go.transform, pixel, new Vector3(0, -0.03f, 0));
        light.transform.localScale = new Vector3(0.14f, 0.012f, 1);
        light.color = Color.cyan;
        platform.warningLight = light;
        for (int x = firstX; x <= lastX; x++) map.SetTile(new Vector3Int(x, y, 0), null);
        if (y == 37) PrefabUtility.SaveAsPrefabAsset(go, PrefabPath + "/PlataformaInstavel.prefab");
    }

    private static SpriteRenderer Visual(string name, Transform parent, Sprite sprite, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = spriteMaterial;
        renderer.sortingOrder = 6;
        return renderer;
    }

    // Original small pixel drawing, following the project's code-native PixelHUDArt approach.
    private static Sprite CreateShip()
    {
        const int w = 48, h = 32;
        var pixels = new Color32[w * h];
        Action<int, int, int, int, string> rect = (x, y, width, height, hex) => {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            for (int yy = y; yy < y + height; yy++) for (int xx = x; xx < x + width; xx++)
                if (xx >= 0 && xx < w && yy >= 0 && yy < h) pixels[yy * w + xx] = color;
        };
        // Glass dome with a green pilot and dark almond eyes.
        for (int y = 13; y <= 29; y++) for (int x = 11; x <= 36; x++)
        {
            float ellipse = (x - 23.5f) * (x - 23.5f) / 169f + (y - 15f) * (y - 15f) / 225f;
            if (ellipse <= 1f) rect(x, y, 1, 1, ellipse > 0.8f ? "79DFF6" : "183E66");
        }
        rect(18, 17, 12, 9, "48BF71"); rect(20, 25, 8, 2, "8EF3A0");
        rect(16, 20, 2, 4, "48BF71"); rect(30, 20, 2, 4, "48BF71");
        rect(19, 21, 4, 3, "071326"); rect(26, 21, 4, 3, "071326");
        rect(20, 23, 1, 1, "CDFBFF"); rect(27, 23, 1, 1, "CDFBFF");
        rect(23, 18, 3, 1, "183E43"); rect(17, 14, 15, 3, "20324B");
        rect(13, 24, 2, 3, "D4F8FF"); rect(15, 27, 5, 1, "D4F8FF");
        // Layered saucer, metal bevels, rivets and cyan propulsion.
        rect(12, 12, 24, 3, "B3C8E8"); rect(7, 10, 34, 3, "526C9B");
        rect(3, 7, 42, 4, "111E37"); rect(5, 8, 38, 3, "92ABC9");
        rect(9, 5, 30, 3, "344B78"); rect(14, 3, 20, 2, "111E37");
        rect(7, 9, 34, 1, "E5F4FF");
        foreach (int x in new[] { 10, 19, 28, 37 }) { rect(x, 7, 3, 2, "102743"); rect(x, 8, 2, 1, "5FF3E9"); }
        rect(18, 2, 12, 2, "54E4FF"); rect(21, 0, 6, 2, "A4F5FF");
        return SaveSprite("MiniOVNIAlien", w, h, pixels, 100f);
    }

    private static Sprite CreateBeam()
    {
        const int w = 20, h = 30;
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            int half = 3 + (h - 1 - y) / 5;
            for (int x = 10 - half; x < 10 + half; x++)
                pixels[y * w + x] = new Color32(77, 216, 255, (byte)(x == 10 - half || x == 9 + half ? 110 : 32));
        }
        return SaveSprite("CampoTrator", w, h, pixels, 100f);
    }

    private static Sprite SaveSprite(string name, int width, int height, Color32[] pixels, float ppu)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        string path = ArtPath + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("Tools/Obstaculos espaciais/Validar cena")]
    public static void Validate()
    {
        var saws = UnityEngine.Object.FindObjectsByType<UfoSawPatrol>();
        var platforms = UnityEngine.Object.FindObjectsByType<SpaceFallingPlatform>();
        var spikes = UnityEngine.Object.FindObjectsByType<SpaceRetractingSpikes>();
        // Coin detours and the upper continuation may add hazards to the original seven.
        if (saws.Length < 7 || platforms.Length < 5 || spikes.Length < 1)
            throw new InvalidOperationException("Contagem inesperada: " + saws.Length + "/" + platforms.Length + "/" + spikes.Length);
        if (saws.Any(s => s.transform.position.y < 60f && !s.name.StartsWith("OVNI guarda de moeda")) || platforms.Any(p => p.transform.position.y < 30f) ||
            spikes.Any(s => s.transform.position.y < 35f))
            throw new InvalidOperationException("Novas mecanicas invadiram o trecho inicial.");
        foreach (var background in UnityEngine.Object.FindObjectsByType<Cenarioinfinito>())
            if (background.GetComponent<Renderer>().sharedMaterial.shader.name != "Game/ScrollingBackground")
                throw new InvalidOperationException("Fundo sem animacao UV: " + background.name);
        if (Camera.main.clearFlags != CameraClearFlags.SolidColor)
            throw new InvalidOperationException("Camera deve limpar as bordas a cada quadro.");
        foreach (var saw in saws)
            if (saw.ship == null || saw.engine == null || !saw.GetComponent<CircleCollider2D>().isTrigger || saw.travel.magnitude <= 0)
                throw new InvalidOperationException("OVNI incompleto: " + saw.name);
        foreach (var platform in platforms)
            if (platform.idleSprite == null || platform.warningLight == null || platform.gameObject.layer != LayerMask.NameToLayer("Ground") ||
                platform.GetComponent<Animator>().runtimeAnimatorController == null)
                throw new InvalidOperationException("Plataforma incompleta: " + platform.name);
        foreach (var spike in spikes)
            if (spike.blades == null || spike.warningLight == null || spike.GetComponents<Collider2D>().Length == 0)
                throw new InvalidOperationException("Armadilha incompleta: " + spike.name);
        Debug.Log("[SpaceObstacleSetup] VALIDATE PASS");
    }
}
