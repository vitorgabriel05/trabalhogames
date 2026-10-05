using System.Collections.Generic;
using UnityEngine;

// The player pose becomes a red soul, cracks, then scatters into pixel shards.
// Timings match the two sound transients in the user-provided reference recording.
public class PlayerDeathEffect : MonoBehaviour
{
    public const float SoundStart = 0.18f;
    public const float SplitTime = SoundStart + 0.10f;
    public const float ShatterTime = SoundStart + 1.40f;
    public const float Duration = 3.8f;
    public enum Phase { Impact, Soul, Broken, Shattered, Finished }
    public Phase CurrentPhase { get; private set; }
    public bool SoundStarted { get; private set; }
    public AudioClip DeathSound { get; private set; }
    public float FragmentAlpha { get; private set; }
    public int VisibleFragments { get; private set; }

    private GameObject root;
    private SpriteRenderer character;
    private Color characterColor;
    private SpriteRenderer veil, heart, leftHalf, rightHalf;
    private readonly List<SpriteRenderer> fragments = new List<SpriteRenderer>();
    private readonly List<Sprite> sprites = new List<Sprite>();
    private readonly List<Texture2D> textures = new List<Texture2D>();
    private AudioSource deathAudio;
    private Vector3 origin;
    private Camera view;
    private static readonly Color SoulRed = new Color(1f, 0f, 0f, 1f);
    private static readonly Vector2[] Velocities = {
        new Vector2(-1.05f, 0.7f), new Vector2(-0.7f, -0.25f),
        new Vector2(-0.3f, 1.15f), new Vector2(0.4f, 1.05f),
        new Vector2(0.95f, 0.4f), new Vector2(0.7f, -0.55f)
    };

    public void Begin(SpriteRenderer source, Camera camera)
    {
        if (root != null) return;
        character = source;
        characterColor = source.color;
        origin = source.bounds.center;
        view = camera;
        root = new GameObject("Death - soul and fragments");
        var pixel = MakeSprite(new[] { "#" }, 1f);
        veil = Visual("Black backdrop", pixel, Color.black, 32760);
        FitBackdrop(camera);

        string[] shape = {
            "................", "................", "...####..####...", "..############..",
            "..############..", "..############..", "...##########...", "....########....",
            ".....######.....", "......####......", ".......##.......", "................",
            "................", "................", "................", "................"
        };
        heart = Visual("Red soul", MakeSprite(shape, 40f), SoulRed, 32761);
        leftHalf = Visual("Soul - left crack", MakeSprite(shape, 40f, -1), SoulRed, 32761);
        rightHalf = Visual("Soul - right crack", MakeSprite(shape, 40f, 1), SoulRed, 32761);
        for (int i = 0; i < Velocities.Length; i++)
        {
            var shard = MakeSprite(i % 2 == 0 ? new[] { ".#.", "###", "#.." } : new[] { "##.", ".##", "..#" }, 40f);
            fragments.Add(Visual("Soul fragment " + i, shard, SoulRed, 32761));
        }
        DeathSound = Resources.Load<AudioClip>("Audio/UndertaleDeath");
        deathAudio = gameObject.AddComponent<AudioSource>();
        deathAudio.playOnAwake = false;
        deathAudio.spatialBlend = 0f;
        deathAudio.volume = 0.85f;
        if (DeathSound == null) Debug.LogError("Death reference sound missing: Resources/Audio/UndertaleDeath", this);
        RenderAt(0f);
    }

    public void RenderAt(float elapsed)
    {
        if (root == null) return;
        FitBackdrop(view);
        CurrentPhase = elapsed < SoundStart ? Phase.Impact : elapsed < SplitTime ? Phase.Soul
            : elapsed < ShatterTime ? Phase.Broken : elapsed < Duration ? Phase.Shattered : Phase.Finished;
        float dissolve = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / SoundStart));
        character.color = new Color(characterColor.r, characterColor.g, characterColor.b, characterColor.a * (1f - dissolve));
        veil.color = new Color(0f, 0f, 0f, dissolve);
        heart.enabled = elapsed < SplitTime;
        heart.color = new Color(1f, 0f, 0f, dissolve);
        bool broken = elapsed >= SplitTime && elapsed < ShatterTime;
        leftHalf.enabled = rightHalf.enabled = broken;
        // A jagged gap opens once, then holds during the reference's silent beat.
        float opening = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - SplitTime) / 0.12f));
        leftHalf.transform.position = origin + Vector3.left * (0.035f * opening);
        rightHalf.transform.position = origin + new Vector3(0.035f * opening, -0.025f * opening, 0f);
        float flight = Mathf.Max(0f, elapsed - ShatterTime);
        FragmentAlpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.05f, 2f, flight));
        VisibleFragments = 0;
        for (int i = 0; i < fragments.Count; i++)
        {
            var shard = fragments[i];
            shard.enabled = elapsed >= ShatterTime && FragmentAlpha > 0f;
            if (shard.enabled) VisibleFragments++;
            Vector2 offset = Velocities[i] * flight + Vector2.down * (0.42f * flight * flight);
            // Keep the tiny shapes on the same pixel grid as the soul.
            offset.x = Mathf.Round(offset.x * 40f) / 40f;
            offset.y = Mathf.Round(offset.y * 40f) / 40f;
            shard.transform.position = origin + (Vector3)offset;
            shard.transform.rotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 1f : -1f) * 90f * Mathf.Floor(flight * 5f));
            shard.color = new Color(1f, 0f, 0f, FragmentAlpha);
        }
        if (!SoundStarted && elapsed >= SoundStart)
        {
            SoundStarted = true;
            if (DeathSound != null) deathAudio.PlayOneShot(DeathSound);
        }
    }

    public void FitBackdrop(Camera camera)
    {
        if (veil == null) return;
        if (camera == null) { veil.transform.localScale = new Vector3(100f, 100f, 1f); return; }
        veil.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, origin.z);
        veil.transform.localScale = new Vector3(camera.orthographicSize * camera.aspect * 2f + 1f,
            camera.orthographicSize * 2f + 1f, 1f);
    }

    private SpriteRenderer Visual(string name, Sprite sprite, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        go.transform.position = origin;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = character.sharedMaterial;
        renderer.color = color;
        renderer.sortingLayerID = character.sortingLayerID;
        renderer.sortingOrder = order;
        return renderer;
    }

    private Sprite MakeSprite(string[] rows, float ppu, int half = 0)
    {
        int height = rows.Length, width = rows[0].Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var colors = new Color32[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int crack = width / 2 + (y % 3 == 0 ? 1 : 0);
            if (rows[y][x] == '#' && (half == 0 || (half < 0 ? x < crack : x >= crack)))
                colors[(height - 1 - y) * width + x] = new Color32(255, 255, 255, 255);
        }
        texture.SetPixels32(colors); texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), ppu);
        textures.Add(texture); sprites.Add(sprite);
        return sprite;
    }

    private void OnDestroy()
    {
        if (root != null) Destroy(root);
        foreach (var sprite in sprites) if (sprite != null) Destroy(sprite);
        foreach (var texture in textures) if (texture != null) Destroy(texture);
    }
}
