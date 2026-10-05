using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class UpperTowerSetup
{
    public const string RootName = "Continuacao da torre";
    private static Transform root;
    private static SpaceFallingPlatform fallingSource;
    private static UfoSawPatrol sawSource;
    private static GameObject spikeSource;
    private static Material material;
    private static Sprite pixel;

    [MenuItem("Tools/Torre/Aplicar continuacao ate o topo")]
    public static void Apply()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(MainMenuScreen.GameScene);
        var previous = GameObject.Find(RootName);
        if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
        fallingSource = UnityEngine.Object.FindObjectsByType<SpaceFallingPlatform>().First();
        sawSource = UnityEngine.Object.FindObjectsByType<UfoSawPatrol>().First();
        spikeSource = UnityEngine.Object.FindAnyObjectByType<SpaceRetractingSpikes>().transform.parent.gameObject;
        material = fallingSource.GetComponent<SpriteRenderer>().sharedMaterial;
        pixel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Sprites/SpaceObstacles/Pixel.png");
        root = new GameObject(RootName).transform;

        // 3.2-unit rises fit the existing 8-unit jump and double jump.
        // The first landing sits to the right of the original ledge at x=-3, y=123.
        float[] xs = { 0f, 3f, 0f, -3f };
        
        for (int i = 0; i < 54; i++)
        {
            float y = 125.8f + i * 3.2f;
            float x = xs[i % xs.Length];
            int section = Mathf.Min(3, i / 13);
            bool rest = i % 12 == 0 || i == 53;
            bool rhythm = !rest && i % 3 == 1;
            bool falling = !rest && i % 3 == 2;
            bool moving = !rest && !rhythm && !falling && i % 2 == 1;
            var position = new Vector3(x, y, 0);
            GameObject support;
            if (falling)
            {
                support = UnityEngine.Object.Instantiate(fallingSource.gameObject, position, Quaternion.identity, root);
                support.name = "Rota instavel " + i;
                support.transform.localScale = new Vector3(2.2f / fallingSource.idleSprite.bounds.size.x, .4f / fallingSource.idleSprite.bounds.size.y, 1);
                var trap = support.GetComponent<SpaceFallingPlatform>();
                trap.warningTime = section >= 2 ? .5f : .65f;
                trap.returnDelay = 3.2f;
            }
            else
            {
                support = Platform("Rota " + (rest ? "descanso " : moving ? "movel " : "segura ") + i,
                    position, rest ? 3.8f : rhythm ? 3.2f : 2.2f,
                    rest ? new Color(.55f, 1f, .7f) : moving ? new Color(.4f, .8f, 1f) : Color.white);
                if (moving)
                {
                    var motion = support.AddComponent<CoinMovingPlatform>();
                    motion.amplitude = .7f;
                    motion.period = section >= 2 ? 2.5f : 3.2f;
                    motion.phase = i * 1.7f;
                }
            }
            if (!rest && !falling)
            {
                Spikes(position + new Vector3(i % 2 == 0 ? .9f : -.9f, .2f, 0), section, i, support.transform);
                if (rhythm && section >= 2)
                    Spikes(position + new Vector3(i % 2 == 0 ? -.9f : .9f, .2f, 0), section, i + 100, support.transform);
            }
            if (rest)
            {
                Visual("Luz de descanso " + i, position + Vector3.down * .28f, new Vector2(3.8f, .055f), new Color(.25f, 1f, .65f));

            }
            // A separate side branch; no coin or guard is required for the main route.
            if (i % 4 == 2)
            {
                float side = (i / 4) % 2 == 0 ? 1f : -1f;
                var bonus = Platform("Desvio de moedas " + i, position + new Vector3(side * 5.2f, 1.25f, 0), 1.9f, new Color(1f, .8f, .35f));
                if (section != 1)
                {
                    var motion = bonus.AddComponent<CoinMovingPlatform>();
                    motion.amplitude = .35f; motion.period = 4.8f; motion.phase = i;
                }
                Coin(bonus.transform, new Vector3(-.35f, .8f, 0), "Moeda desvio " + i + " A");
                Coin(bonus.transform, new Vector3(.35f, .8f, 0), "Moeda desvio " + i + " B");
            }
            if (!rest && i % 2 == 1)
            {
                // Alternate a timing challenge across the route with a bonus-lane guard.
                float guardX = x * .5f;
                var saw = UnityEngine.Object.Instantiate(sawSource.gameObject, new Vector3(guardX, y + 1.8f, 0), Quaternion.identity, root);
                saw.name = "OVNI do trecho " + i;
                var patrol = saw.GetComponent<UfoSawPatrol>();
                patrol.travel = new Vector2(section >= 2 ? 2.4f : 1.9f, .35f);
                patrol.period = section >= 2 ? 2.6f : 3.3f;
                patrol.phase = i * .7f;
            }

        }
        // A permanent landing and a beacon mark the end of this climb.
        Visual("Mastro do topo", new Vector3(1.6f, 297f, 0), new Vector2(.1f, 2.4f), Color.white);
        Visual("Bandeira do topo", new Vector3(2.2f, 297.65f, 0), new Vector2(1.2f, .65f), new Color(.5f, 1f, .4f));
        var last = root.GetComponentsInChildren<Transform>().Single(t => t.name == "Rota descanso 53");
        Coin(last, new Vector3(-.8f, .9f, 0), "Moeda do topo A");
        Coin(last, new Vector3(0, .9f, 0), "Moeda do topo B");
        Coin(last, new Vector3(.8f, .9f, 0), "Moeda do topo C");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Validate();
        Debug.Log("UPPER SETUP PASS");
    }

    private static GameObject Platform(string name, Vector3 position, float width, Color tint)
    {
        // Keep the transform unscaled so rigidbody travel and child coins use world units.
        var go = new GameObject(name);
        go.transform.SetParent(root); go.transform.position = position;
        go.layer = LayerMask.NameToLayer("Ground");
        var art = new GameObject("Superficie").AddComponent<SpriteRenderer>();
        art.transform.SetParent(go.transform, false);
        art.sprite = fallingSource.idleSprite; art.sharedMaterial = material; art.sortingOrder = 5; art.color = tint;
        art.transform.localScale = new Vector3(width / art.sprite.bounds.size.x, .4f / art.sprite.bounds.size.y, 1);
        var shape = go.AddComponent<BoxCollider2D>(); shape.size = new Vector2(width, .4f);
        return go;
    }

    private static void Coin(Transform parent, Vector3 localPosition, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false); go.transform.localPosition = localPosition;
        var art = go.AddComponent<SpriteRenderer>();
        art.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Coins/coin_00.png");
        art.sharedMaterial = material; art.sortingOrder = 15;
        var trigger = go.AddComponent<CircleCollider2D>(); trigger.radius = .24f; trigger.isTrigger = true;
        go.AddComponent<ClimbCoin>();
    }

    private static void Spikes(Vector3 bottom, int section, int index, Transform support)
    {
        var go = UnityEngine.Object.Instantiate(spikeSource, bottom, Quaternion.identity, root);
        go.name = "Espetos no ritmo " + index;
        var trap = go.GetComponentInChildren<SpaceRetractingSpikes>();
        var art = trap.GetComponent<SpriteRenderer>();
        go.transform.localScale *= .8f / art.bounds.size.x;
        // Use the opaque sprite bounds; transparent borders must not float above the support.
        var bounds = MapPolishSetup.OpaqueBounds(art.sprite);
        float baseY = art.transform.TransformPoint(bounds.min).y;
        go.transform.position += Vector3.up * (bottom.y - baseY);
        trap.safeTime = section >= 2 ? 1.1f : 1.5f;
        trap.warningTime = .55f; trap.activeTime = 1.5f;
        trap.phaseOffset = index * .61f;
        go.transform.SetParent(support, true);
    }

    private static void Visual(string name, Vector3 position, Vector2 size, Color tint)
    {
        var go = new GameObject(name); go.transform.SetParent(root); go.transform.position = position;
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        var art = go.AddComponent<SpriteRenderer>(); art.sprite = pixel; art.sharedMaterial = material; art.color = tint; art.sortingOrder = 6;
    }

    public static void Validate()
    {
        var extension = GameObject.Find(RootName);
        if (extension == null) throw new InvalidOperationException("Continuacao ausente");
        var platforms = extension.GetComponentsInChildren<BoxCollider2D>().Where(b => b.gameObject.layer == LayerMask.NameToLayer("Ground")).ToArray();
        if (platforms.Length != 67 || extension.GetComponentsInChildren<ClimbCoin>().Length != 29 ||
            extension.GetComponentsInChildren<SpaceRetractingSpikes>().Length < 30 ||
            extension.GetComponentsInChildren<UfoSawPatrol>().Length < 25)
            throw new InvalidOperationException("Contagem de desafios/recompensas inesperada: " + platforms.Length);
        foreach (var trap in extension.GetComponentsInChildren<SpaceRetractingSpikes>())
            if (trap.transform.parent.parent == extension.transform) throw new InvalidOperationException("Espetos sem apoio");
        if (extension.GetComponentsInChildren<TextMesh>().Length != 0) throw new InvalidOperationException("Textos na torre");
        var route = platforms.Where(b => b.name.StartsWith("Rota ")).OrderBy(b => b.transform.position.y).ToArray();
        if (route.Length != 54) throw new InvalidOperationException("Rota principal incompleta");
        for (int i = 1; i < route.Length; i++)
            if (route[i].transform.position.y - route[i-1].transform.position.y > 3.3f ||
                Mathf.Abs(route[i].transform.position.x - route[i-1].transform.position.x) > 3.1f)
                throw new InvalidOperationException("Salto sem apoio: " + route[i].name);
        foreach (var component in extension.GetComponentsInChildren<Transform>())
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(component.gameObject) > 0)
                throw new InvalidOperationException("Script ausente: " + component.name);
        Debug.Log("UPPER STATIC PASS: 54 apoios de rota, 13 desvios, 29 moedas; topo " + route.Last().transform.position.y);
    }

    public static void Run() { Apply(); UpperTowerValidation.Run(); }

    public static void Inspect()
    {
        EditorSceneManager.OpenScene(MainMenuScreen.GameScene);
        foreach (var map in UnityEngine.Object.FindObjectsByType<Tilemap>())
        {
            foreach (int y in Enumerable.Range(map.cellBounds.yMin, map.cellBounds.size.y))
            {
                var cells = Enumerable.Range(map.cellBounds.xMin, map.cellBounds.size.x).Select(x => new Vector3Int(x, y, 0))
                    .Where(c => c.y == y && map.HasTile(c) && !map.HasTile(c + Vector3Int.up)).ToArray();
                if (cells.Length > 0) Debug.Log("SURFACE " + map.name + " y=" + y + " x=" + string.Join(",", cells.Select(c => c.x)) + " world=" + map.GetCellCenterWorld(cells[0]));
            }
        }
        foreach (var bg in UnityEngine.Object.FindObjectsByType<Cenarioinfinito>())
            Debug.Log("BACKGROUND " + bg.name + " " + bg.GetComponent<Renderer>().bounds);
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        Debug.Log("PLAYER " + player.transform.position + " " + new SerializedObject(player).FindProperty("jumpForce").floatValue + " gravity=" + player.GetComponent<Rigidbody2D>().gravityScale);
    }
}
