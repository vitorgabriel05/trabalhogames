using UnityEngine;
using UnityEngine.UI;

// Attached to the player so every scene restart begins a fresh attempt.
public class HeightRecordHUD : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float metersPerUnit = 1f;
    [SerializeField] private AudioClip recordSound;
    [SerializeField, Range(0f, 1f)] private float recordVolume = 0.65f;
    [SerializeField] private Font pixelFont;

    public int Coins { get; private set; }
    private Text coinText;
    public void AddCoin() { Coins++; RefreshLabels(); }
    public int Height { get; private set; }
    public int Record { get; private set; }

    private float startY;
    private string recordKey;
    private bool celebrated;
    private bool pendingSave;
    private float celebrationUntil;
    private GameObject hud;
    private Text heightText;
    private Text recordText;
    private CanvasGroup toast;
    private AudioSource audioSource;
    private Font font;

    private void Awake()
    {
        startY = transform.position.y;
        recordKey = "HeightRecord.v1." + gameObject.scene.path;
        Record = Mathf.Max(0, PlayerPrefs.GetInt(recordKey, 0));
        CreateHUD();
        RefreshLabels();
    }

    private void LateUpdate()
    {
        var player = GetComponent<PlayerController>();
        if (player != null && player.IsDead) return;
        int current = Mathf.Max(0, Mathf.FloorToInt((transform.position.y - startY) * metersPerUnit));
        if (current != Height)
        {
            Height = current;
            if (Height > Record)
            {
                Record = Height;
                PlayerPrefs.SetInt(recordKey, Record);
                pendingSave = true;
                if (!celebrated)
                {
                    celebrated = true;
                    celebrationUntil = Time.unscaledTime + 2f;
                    if (recordSound != null) audioSource.PlayOneShot(recordSound, recordVolume);
                }
            }
            RefreshLabels();
        }

        float remaining = celebrationUntil - Time.unscaledTime;
        toast.alpha = celebrated && remaining > 0f
            ? Mathf.Clamp01(Mathf.Min((2f - remaining) / 0.15f, remaining / 0.35f))
            : 0f;
        toast.transform.localScale = Vector3.one * (1f + 0.04f * toast.alpha * Mathf.Sin(Time.unscaledTime * 8f));
    }

    public void HideForDeath()
    {
        if (hud != null) hud.SetActive(false);
    }

    public void ResetRecord()
    {
        Record = 0;
        pendingSave = false;
        PlayerPrefs.DeleteKey(recordKey);
        PlayerPrefs.Save();
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (coinText != null) coinText.text = Coins.ToString();
        heightText.text = Height + " m";
        recordText.text = Record + " m";
    }

    private void SaveRecord()
    {
        if (!pendingSave) return;
        PlayerPrefs.Save();
        pendingSave = false;
    }

    private void OnDisable()
    {
        SaveRecord();
        if (hud != null) hud.SetActive(false);
    }

    private void OnEnable()
    {
        if (hud != null) hud.SetActive(true);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveRecord();
    }

    private void OnApplicationQuit() => SaveRecord();

    private void OnDestroy()
    {
        if (hud != null) Destroy(hud);
    }

    private void CreateHUD()
    {
        font = pixelFont != null ? pixelFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hud = new GameObject("Height and Record HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvas.pixelPerfect = true;
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        audioSource = hud.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        var art = hud.AddComponent<PixelHUDArt>();
        var panel = Rect("Height panel", hud.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -40), new Vector2(336, 142));
        Picture(panel, art.Frame(168, 71, false));
        panel.localScale = Vector3.one * 1.1f;
        var trophy = Rect("Trophy", panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -22), new Vector2(56, 52));
        Picture(trophy, art.Trophy());
        var heightLabel = Label("Height label", panel, new Vector2(92, -13), new Vector2(130, 36), "ALTURA", 22, Color.white);
        var recordLabel = Label("Record label", panel, new Vector2(92, -48), new Vector2(136, 36), "RECORDE", 20, Color.white);
        heightLabel.horizontalOverflow = recordLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        heightText = Label("Height", panel, new Vector2(224, -13), new Vector2(94, 36), "", 22, Color.white);
        recordText = Label("Record", panel, new Vector2(224, -48), new Vector2(94, 36), "", 22, Color.white);
        heightText.resizeTextForBestFit = recordText.resizeTextForBestFit = true;
        heightText.resizeTextMinSize = recordText.resizeTextMinSize = 12;
        heightText.resizeTextMaxSize = recordText.resizeTextMaxSize = 22;

        var coinIcon = Rect("Coin icon", panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -92), new Vector2(32, 38));
        Picture(coinIcon, Resources.Load<Sprite>("Coins/coin_00"));
        coinIcon.GetComponent<Image>().preserveAspect = true;
        Label("Coin label", panel, new Vector2(92, -91), new Vector2(130, 36), "MOEDAS", 20, new Color32(255, 222, 99, 255));
        coinText = Label("Coins", panel, new Vector2(224, -91), new Vector2(94, 36), "0", 22, new Color32(255, 222, 99, 255));
        var celebration = Rect("New record", hud.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
        var banner = Rect("Gold banner", celebration, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -154), new Vector2(348, 64));
        Picture(banner, art.Frame(174, 32, true));
        var message = Label("Message", banner, Vector2.zero, new Vector2(348, 64), "NOVO RECORDE!", 28, new Color32(12, 29, 49, 255));
        message.alignment = TextAnchor.MiddleCenter;
        var star = art.Sparkle();
        for (int side = -1; side <= 1; side += 2)
        for (int i = 0; i < 3; i++)
        {
            float x = side * (i == 1 ? 218 : 194);
            float y = -154 - i * 24;
            var sparkle = Rect("Sparkle", celebration, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(x, y), Vector2.one * (i == 1 ? 20 : 28));
            Picture(sparkle, star);
        }
        toast = celebration.gameObject.AddComponent<CanvasGroup>();
        toast.alpha = 0f;
        toast.blocksRaycasts = false;
        toast.interactable = false;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void Picture(RectTransform rect, Sprite sprite)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
    }

    private Text Label(string name, Transform parent, Vector2 position, Vector2 size, string content, int fontSize, Color color)
    {
        var rect = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), position, size);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Normal;
        text.color = color;
        text.text = content;
        text.supportRichText = true;
        text.raycastTarget = false;
        text.alignment = TextAnchor.MiddleLeft;
        return text;
    }
}
