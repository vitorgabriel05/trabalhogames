using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class PauseScreen : MonoBehaviour
{
    public static bool IsPaused { get; private set; }
    public int SelectedOption { get; private set; } = 3;
    private Texture2D artwork, panelArtwork, backdrop, halo;
    private AudioSource uiAudio;
    private AudioClip selectSound, confirmSound;
    private float previousScale;
    private bool previousAudioPause, ready, confirming;
    private Vector2 lastMouse;
    private readonly float[] emphasis = new float[4];
    // Bounds of the complete panel in pausa leproso2.png (1536 x 1024).
    private const int PanelX = 204, PanelY = 90, PanelWidth = 1128, PanelHeight = 886;
    private static readonly Rect PanelUV = new Rect(PanelX / 1536f, 1 - (PanelY + PanelHeight) / 1024f, PanelWidth / 1536f, PanelHeight / 1024f);
    private static readonly Rect[] Regions = {
        new Rect(107f / PanelWidth, 297f / PanelHeight, 251f / PanelWidth, 248f / PanelHeight),
        new Rect(433f / PanelWidth, 297f / PanelHeight, 260f / PanelWidth, 248f / PanelHeight),
        new Rect(766f / PanelWidth, 297f / PanelHeight, 257f / PanelWidth, 248f / PanelHeight),
        new Rect(187f / PanelWidth, 669f / PanelHeight, 753f / PanelWidth, 171f / PanelHeight)
    };
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() { IsPaused = false; }

    private void Awake()
    {
        artwork = Resources.Load<Texture2D>("UI/Pause");
        if (artwork != null) panelArtwork = CreatePanelArtwork(artwork);
        selectSound = Resources.Load<AudioClip>("Audio/MenuSelect");
        confirmSound = Resources.Load<AudioClip>("Audio/MenuConfirm");
        uiAudio = gameObject.AddComponent<AudioSource>();
        uiAudio.playOnAwake = false;
        uiAudio.ignoreListenerPause = true;
        uiAudio.volume = .45f;
        halo = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            float r = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 32f;
            halo.SetPixel(x, y, new Color(.8f, 1f, .4f, Mathf.Pow(Mathf.Clamp01(1-r), 2)));
        }
        halo.Apply();
    }

    private void Update()
    {
        if (MainMenuScreen.IsActive || GetComponent<PlayerController>().IsDead || confirming) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (IsPaused) Resume(); else Open();
            return;
        }
        if (!IsPaused || !ready) return;
        int next = SelectedOption;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) next = SelectedOption == 3 ? 0 : (SelectedOption + 2) % 3;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) next = SelectedOption == 3 ? 0 : (SelectedOption + 1) % 3;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) next = SelectedOption == 3 ? 1 : 3;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) next = SelectedOption == 3 ? 1 : 3;
        Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (mouse != lastMouse)
            for (int i = 0; i < 4; i++) if (ButtonRect(i).Contains(mouse)) next = i;
        lastMouse = mouse;
        Select(next);
        for (int i = 0; i < 4; i++) emphasis[i] = Mathf.MoveTowards(emphasis[i], i == SelectedOption ? 1 : 0, Time.unscaledDeltaTime * 8);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) Confirm();
        if (Input.GetMouseButtonDown(0))
            for (int i = 0; i < 4; i++) if (ButtonRect(i).Contains(mouse)) { Select(i); Confirm(); break; }
    }

    public void Open()
    {
        if (MainMenuScreen.IsActive || IsPaused || GetComponent<PlayerController>().IsDead || Time.timeScale == 0) return;
        previousScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        IsPaused = true;
        Time.timeScale = 0;
        AudioListener.pause = true;
        SelectedOption = 3;
        emphasis[3] = 1;
        lastMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        uiAudio.PlayOneShot(selectSound);
        StartCoroutine(CaptureBackdrop());
    }

    private IEnumerator CaptureBackdrop()
    {
        if (!Application.isBatchMode)
        {
            yield return new WaitForEndOfFrame();
            if (!IsPaused) yield break;
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            var small = RenderTexture.GetTemporary(320, 180, 0);
            Graphics.Blit(shot, small);
            var old = RenderTexture.active;
            RenderTexture.active = small;
            backdrop = new Texture2D(320, 180, TextureFormat.RGB24, false);
            backdrop.ReadPixels(new Rect(0, 0, 320, 180), 0, 0);
            RenderTexture.active = old;
            RenderTexture.ReleaseTemporary(small);
            Destroy(shot);
            // Separable box passes approximate a soft Gaussian blur, independent of render pipeline.
            Color[] pixels = backdrop.GetPixels(), output = new Color[pixels.Length];
            for (int pass = 0; pass < 6; pass++)
            {
                bool horizontal = pass % 2 == 0;
                for (int y = 0; y < 180; y++)
                for (int x = 0; x < 320; x++)
                {
                    Color sum = Color.clear;
                    for (int k = -3; k <= 3; k++) sum += pixels[Mathf.Clamp(y + (horizontal ? 0 : k), 0, 179) * 320 + Mathf.Clamp(x + (horizontal ? k : 0), 0, 319)];
                    output[y * 320 + x] = sum / 7;
                }
                var swap = pixels; pixels = output; output = swap;
            }
            backdrop.SetPixels(pixels);
            backdrop.Apply();
        }
        ready = IsPaused;
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = ready = false;
        Time.timeScale = previousScale;
        AudioListener.pause = previousAudioPause;
        if (backdrop != null) Destroy(backdrop);
        backdrop = null;
    }

    private void Select(int option)
    {
        if (option == SelectedOption) return;
        SelectedOption = option;
        uiAudio.PlayOneShot(selectSound);
    }

    public void Confirm()
    {
        if (!IsPaused || confirming) return;
        confirming = true;
        uiAudio.PlayOneShot(confirmSound);
        StartCoroutine(PerformAction(SelectedOption));
    }

    private IEnumerator PerformAction(int option)
    {
        yield return new WaitForSecondsRealtime(.16f);
        if (option == 1) GetComponent<HeightRecordHUD>()?.ResetRecord();
        Resume();
        confirming = false;
        if (option == 0 || option == 1) SceneManager.LoadScene(SceneManager.GetActiveScene().path);
        if (option == 2) SceneManager.LoadScene(MainMenuScreen.MenuScene);
    }

    // Use a fixed crop: dark artwork inside the panel must never be erased.
    private static Texture2D CreatePanelArtwork(Texture2D source)
    {
        // Import settings can resize the source; map the crop to its actual dimensions.
        int x = Mathf.RoundToInt(PanelUV.xMin * source.width);
        int y = Mathf.RoundToInt(PanelUV.yMin * source.height);
        int width = Mathf.RoundToInt(PanelUV.xMax * source.width) - x;
        int height = Mathf.RoundToInt(PanelUV.yMax * source.height) - y;
        Color[] pixels = source.GetPixels(x, y, width, height);
        var panel = new Texture2D(width, height, TextureFormat.RGBA32, false);
        panel.filterMode = FilterMode.Point;
        panel.wrapMode = TextureWrapMode.Clamp;
        panel.SetPixels(pixels);
        panel.Apply(false, true);
        return panel;
    }

    private Rect PanelRect()
    {
        float scale = Mathf.Min(Screen.width * .45f / PanelWidth, Screen.height * .45f / PanelHeight);
        return new Rect((Screen.width - PanelWidth * scale) / 2, (Screen.height - PanelHeight * scale) / 2, PanelWidth * scale, PanelHeight * scale);
    }
    private Rect ButtonRect(int i)
    {
        Rect panel = PanelRect(), region = Regions[i];
        return new Rect(panel.x + region.x * panel.width, panel.y + region.y * panel.height, region.width * panel.width, region.height * panel.height);
    }

    private void OnGUI()
    {
        if (!IsPaused || !ready) return;
        Color oldColor = GUI.color;
        int oldDepth = GUI.depth;
        GUI.depth = -900;
        GUI.color = Color.white;
        if (backdrop != null) GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), backdrop, ScaleMode.StretchToFill);
        GUI.color = new Color(0, 0, 0, .48f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        Rect panel = PanelRect();
        GUI.color = Color.white;
        if (panelArtwork != null) GUI.DrawTexture(panel, panelArtwork, ScaleMode.StretchToFill);
        else
        {
            GUI.Box(panel, "PAUSA");
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(12, (int)(panel.width * .035f)), wordWrap = true };
            string[] labels = { "reiniciar", "reiniciar\npontuação", "dar o fora", "BORA!" };
            for (int i = 0; i < 4; i++) GUI.Box(ButtonRect(i), labels[i], style);
        }
        for (int i = 0; i < 4; i++)
        {
            float amount = emphasis[i];
            if (amount <= 0) continue;
            Rect button = ButtonRect(i);
            float grow = button.height * .055f * amount;
            Rect raised = new Rect(button.x - grow, button.y - grow - 5 * amount, button.width + 2 * grow, button.height + 2 * grow);
            GUI.color = new Color(.7f, 1, .3f, .65f * amount);
            GUI.DrawTexture(new Rect(raised.x - 20, raised.y - 20, raised.width + 40, raised.height + 40), halo);
            if (artwork != null)
            {
                GUI.color = new Color(0, 0, 0, .4f * amount);
                GUI.DrawTextureWithTexCoords(new Rect(raised.x + 3, raised.y + 9, raised.width, raised.height), artwork, TextureRegion(i));
                GUI.color = new Color(1, 1, 1, amount);
                GUI.DrawTextureWithTexCoords(raised, artwork, TextureRegion(i));
                GUI.color = new Color(1, 1, .7f, .12f * amount);
                GUI.DrawTextureWithTexCoords(raised, artwork, TextureRegion(i));
            }
            // Bright top edge and dark bottom edge reinforce the lifted button.
            GUI.color = new Color(.95f, 1, .65f, .9f * amount);
            GUI.DrawTexture(new Rect(raised.x, raised.y, raised.width, 3), Texture2D.whiteTexture);
            GUI.color = new Color(0, .15f, .1f, .8f * amount);
            GUI.DrawTexture(new Rect(raised.x, raised.yMax, raised.width, 4), Texture2D.whiteTexture);
        }
        GUI.color = oldColor;
        GUI.depth = oldDepth;
    }
    private static Rect TextureRegion(int i)
    {
        Rect r = Regions[i];
        return new Rect(PanelUV.x + r.x * PanelUV.width, PanelUV.y + (1 - r.yMax) * PanelUV.height, r.width * PanelUV.width, r.height * PanelUV.height);
    }
    private void OnDisable() { Resume(); }
    private void OnDestroy()
    {
        Resume();
        if (halo != null) Destroy(halo);
        if (panelArtwork != null) Destroy(panelArtwork);
    }
}
