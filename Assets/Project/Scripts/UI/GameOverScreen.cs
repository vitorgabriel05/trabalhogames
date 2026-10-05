using UnityEngine;
using UnityEngine.SceneManagement;

// Overlay shown only after the soul and its fragments finish disappearing.
public class GameOverScreen : MonoBehaviour
{
    public bool IsVisible { get; private set; }
    public bool ContinueSelected { get; private set; } = true;
    [Header("Game over audio")]
    [SerializeField, Range(0f, 1f)] private float musicVolume = .35f;
    [SerializeField, Range(0f, 1f)] private float voiceVolume = 1f;
    public static int DeathCount { get; private set; }
    public int SelectedVoiceIndex { get; private set; } = -1;
    private static int previousVoiceIndex = -1;
    private AudioSource musicSource, voiceSource;
    private Texture2D artwork, glow, animatedTitle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        DeathCount = 0;
        previousVoiceIndex = -1;
    }

    private void StartGameOverAudio()
    {
        DeathCount++;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.ignoreListenerPause = true;
        musicSource.loop = false;
        musicSource.volume = musicVolume;
        GameAudioSettings.Register(musicSource, true);
        musicSource.clip = Resources.Load<AudioClip>("Audio/GameOverMusic");
        var continuation = Resources.Load<AudioClip>("Audio/GameOverAmbient");
        double start = AudioSettings.dspTime + .1;
        if (musicSource.clip != null) musicSource.PlayScheduled(start);
        if (continuation != null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.playOnAwake = false;
            ambientSource.ignoreListenerPause = true;
            ambientSource.loop = true;
            ambientSource.volume = musicVolume;
            GameAudioSettings.Register(ambientSource, true);
            ambientSource.clip = continuation;
            ambientSource.PlayScheduled(start + (musicSource.clip != null ? musicSource.clip.length : 0));
        }

        var voices = new System.Collections.Generic.List<AudioClip>();
        for (int i = 1; i <= 6; i++)
        {
            var clip = Resources.Load<AudioClip>("Audio/GameOverVoice" + i);
            if (clip != null) voices.Add(clip);
        }
        if (voices.Count == 0) return;
        // Random on every death, excluding the previous line when possible.
        int selected = Random.Range(0, voices.Count > 1 && previousVoiceIndex >= 0 ? voices.Count - 1 : voices.Count);
        if (voices.Count > 1 && previousVoiceIndex >= 0 && selected >= previousVoiceIndex)
            selected++;
        SelectedVoiceIndex = selected;
        previousVoiceIndex = selected;
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.ignoreListenerPause = true;
        voiceSource.volume = voiceVolume;
        voiceSource.clip = voices[selected];
        voiceSource.Play();
    }

    private void CreateAnimatedTitle()
    {
        if (artwork == null || !artwork.isReadable) return;
        int x = Mathf.RoundToInt(artwork.width * .175f);
        int y = Mathf.RoundToInt(artwork.height * .405f);
        int width = Mathf.RoundToInt(artwork.width * .66f);
        int height = Mathf.RoundToInt(artwork.height * .26f);
        var pixels = artwork.GetPixels(x, y, width, height);
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            // Isolate the pale lettering from the green painted background.
            float alpha = Mathf.Clamp01((Mathf.Min(c.r, c.g, c.b) - .48f) * 8f);
            pixels[i] = new Color(.85f, 1f, .7f, alpha);
        }
        animatedTitle = new Texture2D(width, height, TextureFormat.RGBA32, false);
        animatedTitle.filterMode = FilterMode.Point;
        animatedTitle.SetPixels(pixels);
        animatedTitle.Apply();
    }
    private AudioSource ambientSource;
    private GUIStyle lettering;
    private float openedAt, previousTimeScale;
    private bool confirmed;
    private static readonly Vector2[] Lights = {
        new Vector2(.177f, .309f), new Vector2(.073f, .558f),
        new Vector2(.188f, .630f), new Vector2(.126f, .870f),
        new Vector2(.963f, .514f), new Vector2(.902f, .665f),
        new Vector2(.820f, .703f), new Vector2(.919f, .876f)
    };

    public void Show()
    {
        if (IsVisible) return;
        artwork = Resources.Load<Texture2D>("UI/GameOver");
        lettering = new GUIStyle {
            font = Resources.Load<Font>("UI/GameOverFont"),
            alignment = TextAnchor.MiddleCenter
        };
        lettering.normal.textColor = Color.white;
        glow = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        for (int x = 0; x < 32; x++)
        {
            float radius = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 16f;
            glow.SetPixel(x, y, new Color(.72f, 1f, .12f, Mathf.Pow(Mathf.Clamp01(1f - radius), 2f) * .65f));
        }
        glow.Apply();
        CreateAnimatedTitle();
        StartGameOverAudio();
        openedAt = Time.unscaledTime;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        IsVisible = true;
    }

    private void Update()
    {
        if (!IsVisible || confirmed || Time.unscaledTime - openedAt < .25f) return;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) ContinueSelected = true;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) ContinueSelected = false;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            Confirm();
    }

    private void Confirm()
    {
        if (confirmed) return;
        confirmed = true;
        Time.timeScale = previousTimeScale;
        if (musicSource != null) musicSource.Stop();
        if (ambientSource != null) ambientSource.Stop();
        if (voiceSource != null) voiceSource.Stop();
        if (ContinueSelected) SceneManager.LoadScene(SceneManager.GetActiveScene().path);
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void OnGUI()
    {
        if (!IsVisible) return;
        int oldDepth = GUI.depth;
        Color oldColor = GUI.color;
        GUI.depth = -1000;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        // Fill the game viewport; normalized coordinates keep the complete composition visible.
        float size = Mathf.Min(Screen.width, Screen.height);
        Rect image = new Rect(0f, 0f, Screen.width, Screen.height);
        if (artwork != null) GUI.DrawTexture(image, artwork, ScaleMode.StretchToFill);
        float time = Time.unscaledTime - openedAt;
        if (animatedTitle != null)
        {
            Rect title = new Rect(image.width * .175f, image.height * .335f,
                image.width * .66f, image.height * .26f);
            float pulse = .5f + .5f * Mathf.Sin(time * 3f);
            GUI.color = new Color(1f, 1f, 1f, .15f + .55f * pulse);
            GUI.DrawTexture(title, animatedTitle);
            // Expanding translucent lettering makes a breathing halo around the title.
            float spread = size * (.002f + .005f * pulse);
            GUI.color = new Color(.65f, 1f, .35f, .12f * pulse);
            GUI.DrawTexture(new Rect(title.x - spread, title.y - spread,
                title.width + spread * 2f, title.height + spread * 2f), animatedTitle);
        }
        for (int i = 0; i < Lights.Length; i++)
        {
            float phase = time * (1.1f + i * .08f) + i * 2.3f;
            Vector2 anchor = image.position + Vector2.Scale(Lights[i], image.size);
            Vector2 drift = new Vector2(Mathf.Sin(phase * .7f), Mathf.Cos(phase * .53f)) * size * .018f;
            Vector2 moving = anchor + drift;
            float pulse = .55f + .45f * Mathf.Sin(phase);
            float halo = size * (.035f + .015f * pulse);
            GUI.color = new Color(1f, 1f, 1f, .25f + .65f * pulse);
            // Pulsing glow on the painted fireflies and a small drifting light around each.
            GUI.DrawTexture(new Rect(anchor.x - halo / 2, anchor.y - halo / 2, halo, halo), glow);
            GUI.DrawTexture(new Rect(moving.x - halo / 2, moving.y - halo / 2, halo, halo), glow);
            GUI.color = new Color(.92f, 1f, .38f, .4f + .6f * pulse);
            float pixel = Mathf.Max(2f, size * .005f);
            GUI.DrawTexture(new Rect(moving.x, moving.y, pixel, pixel), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
        Rect panel = new Rect(image.width * .26f, image.height * .785f, image.width * .48f, image.height * .13f);
        GUI.color = Color.white;
        lettering.fontSize = Mathf.Max(12, Mathf.RoundToInt(size * .028f));
        GUI.Label(new Rect(panel.x, panel.y, panel.width, panel.height * .46f), "CONTINUE?", lettering);
        Rect yes = new Rect(panel.x + panel.width * .12f, panel.y + panel.height * .47f, panel.width * .32f, panel.height * .48f);
        Rect no = new Rect(panel.x + panel.width * .57f, yes.y, yes.width, yes.height);
        GUI.Label(yes, "Sim", lettering);
        GUI.Label(no, "Não", lettering);
        if (Mathf.Repeat(time, .8f) < .5f)
        {
            Rect selected = ContinueSelected ? yes : no;
            float p = Mathf.Max(1f, Mathf.Round(size * .003f));
            // Pixel triangle matching the reference selector.
            for (int row = 0; row < 7; row++)
                GUI.DrawTexture(new Rect(selected.x - p * 5, selected.center.y + (row - 3) * p,
                    (4 - Mathf.Abs(row - 3)) * p, p), Texture2D.whiteTexture);
        }
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && time >= .25f)
        {
            if (yes.Contains(Event.current.mousePosition) || no.Contains(Event.current.mousePosition))
            {
                ContinueSelected = yes.Contains(Event.current.mousePosition);
                Event.current.Use();
                Confirm();
            }
        }
        GUI.color = oldColor;
        GUI.depth = oldDepth;
    }

    private void OnDestroy()
    {
        if (IsVisible) Time.timeScale = previousTimeScale;
        if (glow != null) Destroy(glow);
        if (animatedTitle != null) Destroy(animatedTitle);
        if (musicSource != null) Destroy(musicSource);
        if (ambientSource != null) Destroy(ambientSource);
        if (voiceSource != null) Destroy(voiceSource);
    }
}
