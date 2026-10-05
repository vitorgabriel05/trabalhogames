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
        hud.AddCoin();
        GetComponent<Collider2D>().enabled = false;
        art.enabled = false;
        var sound = gameObject.AddComponent<AudioSource>();
        sound.playOnAwake = false; sound.volume = .3f;
        sound.clip = Resources.Load<AudioClip>("Coins/collect");
        GameAudioSettings.Register(sound, false);
        sound.Play();
        Destroy(gameObject, .3f);
        return true;
    }
}
