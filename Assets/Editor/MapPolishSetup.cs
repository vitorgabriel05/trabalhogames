using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class MapPolishSetup
{
    [MenuItem("Tools/Corrigir terreno e animacao de morte")]
    public static void Apply()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(IntegrationValidation.ScenePath);
        var maps = UnityEngine.Object.FindObjectsByType<Tilemap>();
        var lower = maps.Single(m => m.name == "Tilemap");
        var upper = maps.Single(m => m.name == "Tilemap 2");
        // Extend the same four terrain rows, preserving grass, dirt and bottom edges.
        for (int y = -6; y <= -3; y++)
        {
            var left = lower.GetTile(new Vector3Int(lower.HasTile(new Vector3Int(-16, y, 0)) ? -16 : -9, y, 0));
            var middle = lower.GetTile(new Vector3Int(0, y, 0));
            var right = lower.GetTile(new Vector3Int(lower.HasTile(new Vector3Int(15, y, 0)) ? 15 : 8, y, 0));
            for (int x = -16; x <= 15; x++)
                lower.SetTile(new Vector3Int(x, y, 0), x == -16 ? left : x == 15 ? right : middle);
        }
        var spikes = UnityEngine.Object.FindObjectsByType<Espeto>();
        var early = spikes.Where(s => s.GetComponent<SpaceRetractingSpikes>() == null).ToArray();
        var oldHousings = early.Select(s => s.transform.parent).Where(p => p != null && p.name.StartsWith("Espetos mecanicos")).Distinct().ToArray();
        var first = early.Single(s => s.name == "Espinho inicial - 13" || (s.transform.parent != null && s.transform.parent.name.EndsWith("13")));
        var pair = early.Where(s => s != first).OrderBy(s => s.transform.position.x).ToArray();
        first.name = "Espinho inicial - 13";
        pair[0].name = "Espinho inicial - 19 esquerda";
        pair[1].name = "Espinho inicial - 19 direita";
        // Restore the original early hazards to their authored platforms.
        PlaceSpike(pair[0], lower, new Vector3Int(0, 18, 0));
        PlaceSpike(first, lower, new Vector3Int(1, 12, 0));
        PlaceSpike(pair[1], lower, new Vector3Int(1, 18, 0));
        foreach (var housing in oldHousings)
            if (housing.GetComponentsInChildren<Espeto>().Length == 0) UnityEngine.Object.DestroyImmediate(housing.gameObject);
        PlaceSpike(spikes.Single(s => s.GetComponent<SpaceRetractingSpikes>() != null), upper, new Vector3Int(3, 43, 0));
        var pads = UnityEngine.Object.FindObjectsByType<BouncePad>();
        var positions = new Dictionary<string, Vector3Int> {
            {"Jump1", new Vector3Int(-3, 44, 0)}, {"Jump2", new Vector3Int(-5, 68, 0)},
            {"Jump3", new Vector3Int(7, 79, 0)}, {"Jump4", new Vector3Int(-9, 65, 0)},
            {"Jump5", new Vector3Int(8, 100, 0)}, {"Jump6", new Vector3Int(5, 109, 0)},
            {"Jump7", new Vector3Int(-1, 111, 0)}, {"Jump8", new Vector3Int(-6, 114, 0)},
            {"Jump9", new Vector3Int(-4, 99, 0)}, {"Jump10", new Vector3Int(4, 86, 0)}
        };
        foreach (var pad in pads)
        {
            var renderer = pad.GetComponent<SpriteRenderer>();
            var data = new SerializedObject(pad);
            var idle = (Sprite)data.FindProperty("idleSprite").objectReferenceValue;
            renderer.sprite = idle;
            var local = OpaqueBounds(idle);
            var tile = TileBounds(upper, positions[pad.name]);
            Vector3 target = new Vector3(tile.center.x, tile.max.y - local.min.y * pad.transform.lossyScale.y, 0f);
            pad.transform.position = target;
            renderer.sortingOrder = 8;
            var collider = pad.GetComponent<BoxCollider2D>();
            if (collider == null) collider = pad.gameObject.AddComponent<BoxCollider2D>();
            collider.size = local.size;
            collider.offset = local.center;
            collider.isTrigger = false;
            pad.gameObject.layer = LayerMask.NameToLayer("Ground");
            data.FindProperty("surfaceSize").vector2Value = local.size;
            data.FindProperty("surfaceOffset").vector2Value = local.center;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        CreateDeathClip();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        MapPolishValidation.Validate();
        Debug.Log("[MapPolish] APPLY PASS: continuous grass, 4 supported spikes, 10 visible trampolines, death animation.");
    }

    private static void PlaceSpike(Espeto spike, Tilemap map, Vector3Int cell)
    {
        var renderer = spike.GetComponent<SpriteRenderer>();
        var tile = TileBounds(map, cell);
        var local = OpaqueBounds(renderer.sprite);
        Vector3 target = new Vector3(tile.center.x, tile.max.y - local.min.y * spike.transform.lossyScale.y, 0);
        var housing = spike.transform.parent;
        if (spike.GetComponent<SpaceRetractingSpikes>() == null)
        {
            spike.transform.SetParent(map.transform.parent, true);
            spike.transform.position = target;
            renderer.sortingOrder = 6;
            return;
        }
        // Move the whole mechanical assembly and clear inherited double offsets.
        if (housing != null && housing.name.StartsWith("Espetos mecanicos"))
        {
            Vector3 delta = target - spike.transform.position;
            housing.position += delta;
        }
        else spike.transform.position = target;
        var rail = housing != null ? housing.Find("Base de titanio") : null;
        if (rail != null)
        {
            Vector3 p = rail.position;
            p.y = tile.max.y - 0.03f;
            rail.position = p;
        }
        renderer.sortingOrder = 6;
    }

    private static void CreateDeathClip()
    {
        const string path = "Assets/Project/Animations/Animation.morte.anim";
        var hit = AssetDatabase.LoadAllAssetsAtPath("Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Hit (32x32).png")
            .OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
        var spin = AssetDatabase.LoadAllAssetsAtPath("Assets/Pixel Adventure 1/Assets/Main Characters/Ninja Frog/Double Jump (32x32).png")
            .OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
        if (hit.Length != 7 || spin.Length == 0) throw new InvalidOperationException("Missing Ninja Frog death frames");
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.name = "Animation.morte";
        clip.frameRate = 20;
        var frames = new List<ObjectReferenceKeyframe>();
        for (int i = 0; i < hit.Length; i++) frames.Add(new ObjectReferenceKeyframe { time = i * 0.05f, value = hit[i] });
        for (int i = 0; i < spin.Length; i++) frames.Add(new ObjectReferenceKeyframe { time = 0.35f + i * 0.04f, value = spin[i] });
        frames.Add(new ObjectReferenceKeyframe { time = 0.65f, value = hit[4] });
        frames.Add(new ObjectReferenceKeyframe { time = 0.8f, value = hit[5] });
        frames.Add(new ObjectReferenceKeyframe { time = PlayerController.DeathDuration, value = hit[6] });
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), frames.ToArray());
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Leproso.controller");
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Morte") ?? machine.AddState("Morte", new Vector3(600, 350));
        state.motion = clip;
        state.writeDefaultValues = false;
        state.tag = "Death";
        EditorUtility.SetDirty(clip);
        EditorUtility.SetDirty(controller);
    }

    internal static Bounds TileBounds(Tilemap map, Vector3Int cell)
    {
        var sprite = map.GetSprite(cell);
        var matrix = map.transform.localToWorldMatrix * Matrix4x4.Translate(map.CellToLocalInterpolated(cell + map.tileAnchor)) * map.GetTransformMatrix(cell);
        var opaque = OpaqueBounds(sprite);
        var result = new Bounds(matrix.MultiplyPoint3x4(opaque.min), Vector3.zero);
        result.Encapsulate(matrix.MultiplyPoint3x4(opaque.max));
        return result;
    }

    private static readonly Dictionary<Sprite, Bounds> opaqueCache = new Dictionary<Sprite, Bounds>();
    internal static Bounds OpaqueBounds(Sprite sprite)
    {
        if (opaqueCache.TryGetValue(sprite, out var cached)) return cached;
        var texture = new Texture2D(2, 2);
        texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
        var rect = sprite.rect;
        int minX = (int)rect.width, minY = (int)rect.height, maxX = 0, maxY = 0;
        for (int y = 0; y < rect.height; y++)
        for (int x = 0; x < rect.width; x++)
            if (texture.GetPixel((int)rect.x + x, (int)rect.y + y).a > 0.1f)
            { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x + 1); maxY = Mathf.Max(maxY, y + 1); }
        UnityEngine.Object.DestroyImmediate(texture);
        var min = (new Vector2(minX, minY) - sprite.pivot) / sprite.pixelsPerUnit;
        var max = (new Vector2(maxX, maxY) - sprite.pivot) / sprite.pixelsPerUnit;
        var result = new Bounds((min + max) * 0.5f, max - min);
        opaqueCache[sprite] = result;
        return result;
    }
}
