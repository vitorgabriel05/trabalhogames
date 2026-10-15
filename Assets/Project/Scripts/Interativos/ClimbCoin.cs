using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class ClimbCoin : MonoBehaviour
{
    private Sprite[] frames;
    private SpriteRenderer art;
    private bool collected;
    private Vector3 origin;
    public int FrameIndex { get; private set; }
    private void Awake()
    {
        frames = new Sprite[16];
        for (int i = 0; i < frames.Length; i++) frames[i] = Resources.Load<Sprite>("Coins/coin_" + i.ToString("00"));
        art = GetComponent<SpriteRenderer>();
        origin = transform.localPosition;
        GetComponent<CircleCollider2D>().isTrigger = true;
    }
    private void Update()
    {
        if (collected) return;
        FrameIndex = Mathf.FloorToInt(Time.time * 12f) % frames.Length;
        art.sprite = frames[FrameIndex];
        transform.localPosition = origin + Vector3.up * (Mathf.Sin(Time.time * 3f + origin.y) * .07f);
    }
    private void OnTriggerEnter2D(Collider2D other) { TryCollect(other); }
    private void OnTriggerStay2D(Collider2D other) { TryCollect(other); }
    public bool TryCollect(Collider2D other)
    {
        var player = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerController>() : null;
        if (collected || player == null || player.IsDead || MainMenuScreen.IsActive || PauseScreen.IsPaused) return false;
        var hud = player.GetComponent<HeightRecordHUD>();
        if (hud == null) return false;
        collected = true;
        transform.SetParent(null, true);
        hud.AddCoin();
        GetComponent<Collider2D>().enabled = false;
        art.enabled = false;
        var sound = gameObject.AddComponent<AudioSource>();
        sound.playOnAwake = false; sound.volume = .3f;
        sound.clip = Resources.Load<AudioClip>("Coins/collect");
        GameAudioSettings.Register(sound, false);
        sound.Play();
        StartCoroutine(CollectionSparkles());
        Destroy(gameObject, Mathf.Max(.65f, sound.clip != null ? sound.clip.length : 0f));
        return true;
    }

    private IEnumerator CollectionSparkles()
    {
        const int count = 10;
        const float duration = .55f;
        var sparks = new SpriteRenderer[count];
        var velocities = new Vector3[count];
        // Reuse the coin artwork to keep the particles in the game's pixel style.
        for (int i = 0; i < count; i++)
        {
            var spark = new GameObject("Coin sparkle");
            spark.transform.SetParent(transform, false);
            sparks[i] = spark.AddComponent<SpriteRenderer>();
            sparks[i].sprite = art.sprite;
            sparks[i].sortingLayerID = art.sortingLayerID;
            sparks[i].sortingOrder = art.sortingOrder + 1;
            float angle = i * Mathf.PI * 2f / count;
            velocities[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * Random.Range(1.1f, 2.1f) + Vector3.up * .65f;
            spark.transform.localScale = Vector3.one * .22f;
        }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            for (int i = 0; i < count; i++)
            {
                var spark = sparks[i];
                spark.transform.localPosition = velocities[i] * elapsed + Vector3.down * (2f * elapsed * elapsed);
                spark.transform.localScale = Vector3.one * (.22f * (1f - progress));
                spark.transform.localRotation = Quaternion.Euler(0, 0, i * 36f + elapsed * 240f);
                spark.color = new Color(1f, .85f, .35f, 1f - progress);
            }
            yield return null;
            elapsed += Time.deltaTime;
        }
        foreach (var spark in sparks) Destroy(spark.gameObject);
    }
}
