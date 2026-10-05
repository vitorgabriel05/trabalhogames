using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[DefaultExecutionOrder(-10000)]
public class MainMenuScreen : MonoBehaviour
{
    public const string MenuScene = "Assets/Project/Scenes/Menu.unity";
    public const string GameScene = "Assets/Project/Scenes/TorredasPlataformas.unity";
    public enum Page { Title, Menu, Options, Record, Intro }
    public Page CurrentPage { get; private set; }
    public int SelectedOption { get; private set; }
    public static bool IsActive { get; private set; }
    private static MainMenuScreen activeMenu;
    private static bool introShown;
    private AudioSource introAudio;
    private float prepareDeadline;
    private Texture2D menu, options, recordArtwork, glow;
    private Font recordFont;
    private readonly List<GameObject> suspendedRoots = new List<GameObject>();
    private VideoPlayer video;
    private AudioSource music, effects;
    private AudioClip select, click;
    private Vector2 lastMouse;
    private readonly float[] emphasis = new float[5];
    private bool busy;
    private int dragging = -1;
    private GUIStyle text;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() { IsActive = false; activeMenu = null; introShown = false; }
    private static readonly Rect[] MenuRegions = {
        new Rect(.158f,.231f,.689f,.144f), new Rect(.158f,.391f,.689f,.143f),
        new Rect(.158f,.552f,.689f,.143f), new Rect(.158f,.704f,.689f,.144f),
        new Rect(.417f,.849f,.163f,.136f)
    };
    private static readonly Rect[] OptionRegions = {
        new Rect(.19f,.38f,.12f,.135f), new Rect(.691f,.38f,.12f,.135f),
        new Rect(.19f,.628f,.12f,.135f), new Rect(.691f,.628f,.12f,.135f),
        new Rect(.432f,.795f,.139f,.164f)
    };
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        for(int i=0;i<SceneManager.sceneCount;i++)
            if(SceneManager.GetSceneAt(i).path==MenuScene)
            { OnSceneLoaded(SceneManager.GetSceneAt(i), LoadSceneMode.Single); break; }
    }
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.path == MenuScene && FindAnyObjectByType<MainMenuScreen>() == null)
            new GameObject("Leproso main menu").AddComponent<MainMenuScreen>();
        else if (IsActive)
            FindAnyObjectByType<MainMenuScreen>()?.SuspendGameplay();
    }
    private void Awake()
    {
        IsActive = true;
        activeMenu = this;
        SuspendGameplay();
        Time.timeScale = 0; AudioListener.pause = true;
        menu = Resources.Load<Texture2D>("UI/MainMenu");
        options = Resources.Load<Texture2D>("UI/Options");
        recordArtwork = Resources.Load<Texture2D>("UI/Record");
        recordFont = Resources.Load<Font>("UI/RecordFont");
        select = Resources.Load<AudioClip>("Audio/TitleSelect");
        click = Resources.Load<AudioClip>("Audio/TitleConfirm");
        var cameraObject = new GameObject("Menu Camera");
        cameraObject.transform.SetParent(transform);
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.025f,.055f,.025f);
        cameraObject.AddComponent<AudioListener>();
        video = gameObject.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        CurrentPage = introShown ? Page.Title : Page.Intro;
        video.clip = Resources.Load<VideoClip>(CurrentPage == Page.Intro ? "Video/Opening" : "Video/TitleLoop");
        video.isLooping = CurrentPage != Page.Intro;
        video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        video.audioOutputMode = VideoAudioOutputMode.None;
        video.renderMode = VideoRenderMode.APIOnly;
        video.prepareCompleted += Prepared;
        video.errorReceived += VideoError;
        video.loopPointReached += VideoFinished;
        prepareDeadline = Time.unscaledTime + 20f;
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false; music.loop = true; music.volume = .55f; music.ignoreListenerPause = true;
        music.clip = Resources.Load<AudioClip>("Audio/TitleTheme");
        GameAudioSettings.Register(music, true);
        introAudio = gameObject.AddComponent<AudioSource>();
        introAudio.playOnAwake = false; introAudio.ignoreListenerPause = true;
        GameAudioSettings.Register(introAudio, false);
        if (CurrentPage == Page.Intro)
        {
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            video.controlledAudioTrackCount = 1;
            video.EnableAudioTrack(0, true); video.SetTargetAudioSource(0, introAudio);
        }
        else music.Play();
        if (video.clip != null) video.Prepare();
        else if (CurrentPage == Page.Intro) FinishIntro();
        effects = gameObject.AddComponent<AudioSource>(); effects.playOnAwake = false;
        effects.volume = .5f; effects.ignoreListenerPause = true; GameAudioSettings.Register(effects, false);
        glow = new Texture2D(64,64,TextureFormat.RGBA32,false);
        for (int y=0;y<64;y++) for (int x=0;x<64;x++)
        {
            float r = Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f))/32;
            glow.SetPixel(x,y,new Color(.85f,1,.22f,Mathf.Pow(Mathf.Clamp01(1-r),2)));
        }
        glow.Apply();
        lastMouse = Mouse();
    }
    // The editor can enter Play with both the game and menu scenes loaded.
    // Disable their roots, rather than letting Update/input/audio run behind the UI.
    private void SuspendGameplay()
    {
        foreach (var source in FindObjectsByType<AudioSource>())
            if (!source.transform.IsChildOf(transform)) source.Stop();
        for (int i = 0; i < SceneManager.sceneCount; i++)
            foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                if (root != transform.root.gameObject && root.activeSelf && root.GetComponent<GameAudioSettings>() == null)
                {
                    suspendedRoots.Add(root);
                    root.SetActive(false);
                }
        Time.timeScale=0; AudioListener.pause=true;
    }
    public static bool AllowsAudio(AudioSource source)
    {
        if (!IsActive) return true;
        return activeMenu != null && source.transform.IsChildOf(activeMenu.transform);
    }
    private void Prepared(VideoPlayer player) { player.Play(); }
    private void VideoError(VideoPlayer player, string message)
    {
        Debug.LogWarning("Menu video: " + message);
        if (CurrentPage == Page.Intro) FinishIntro();
    }
    private void VideoFinished(VideoPlayer player) { if (CurrentPage == Page.Intro) FinishIntro(); }
    public void FinishIntro()
    {
        if (CurrentPage != Page.Intro) return;
        introShown = true;
        video.Stop(); introAudio.Stop();
        video.audioOutputMode = VideoAudioOutputMode.None;
        video.clip = Resources.Load<VideoClip>("Video/TitleLoop");
        video.isLooping = true;
        Show(Page.Title);
        music.Play();
        if (video.clip != null) video.Prepare();
    }
    private Vector2 Mouse() { return new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y); }
    public void Show(Page page)
    {
        if (CurrentPage == Page.Options) GameAudioSettings.Instance.Save();
        CurrentPage = page; SelectedOption = 0; dragging = -1;
        for (int i=0;i<5;i++) emphasis[i]=0;
        lastMouse = Mouse();
    }
    private void Update()
    {
        if (busy) return;
        if (CurrentPage == Page.Intro)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.Space) || (!video.isPrepared && Time.unscaledTime > prepareDeadline)) FinishIntro();
            return;
        }
        bool confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
        if (CurrentPage == Page.Title)
        {
            if (confirm || Input.GetMouseButtonDown(0)) { effects.PlayOneShot(click); Show(Page.Menu); }
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) { effects.PlayOneShot(click); Show(CurrentPage == Page.Menu ? Page.Title : Page.Menu); return; }
        if (CurrentPage == Page.Record)
        {
            if (confirm || (Input.GetMouseButtonDown(0) && Map(new Rect(.41f,.832f,.18f,.15f)).Contains(Mouse())))
            { effects.PlayOneShot(click); Show(Page.Menu); }
            return;
        }
        int next = SelectedOption;
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) next = (next+4)%5;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) next = (next+1)%5;
        if (CurrentPage == Page.Options && SelectedOption < 4)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Adjust(SelectedOption/2,-.1f);
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Adjust(SelectedOption/2,.1f);
        }
        Vector2 mouse=Mouse();
        if (mouse != lastMouse)
            for(int i=0;i<5;i++) if(ButtonRect(i).Contains(mouse)) next=i;
        lastMouse=mouse;
        if(next != SelectedOption) { SelectedOption=next; effects.PlayOneShot(select); }
        if (Input.GetMouseButtonDown(0))
        {
            for(int row=0;row<2 && CurrentPage==Page.Options;row++)
                if (BarRect(row).Contains(mouse)) { dragging=row; SelectedOption=row*2+1; }
            if (dragging < 0) for(int i=0;i<5;i++) if(ButtonRect(i).Contains(mouse)) { SelectedOption=i; confirm=true; break; }
        }
        if (dragging >= 0)
        {
            if (Input.GetMouseButton(0))
            {
                Rect bar=BarRect(dragging);
                SetVolume(dragging,Mathf.Clamp01((mouse.x-bar.x)/bar.width));
            }
            else { dragging=-1; GameAudioSettings.Instance.Save(); effects.PlayOneShot(click); }
        }
        for(int i=0;i<5;i++) emphasis[i]=Mathf.MoveTowards(emphasis[i],i==SelectedOption?1:0,Time.unscaledDeltaTime*8);
        if(confirm) Confirm();
    }
    public void Confirm()
    {
        if(busy) return;
        effects.PlayOneShot(click);
        if(CurrentPage==Page.Options)
        {
            if(SelectedOption==4) Show(Page.Menu);
            else Adjust(SelectedOption/2,SelectedOption%2==0?-.1f:.1f);
        }
        else if(CurrentPage==Page.Menu)
        {
            if(SelectedOption==0) StartCoroutine(StartGame());
            else if(SelectedOption==1) Show(Page.Record);
            else if(SelectedOption==2) Show(Page.Options);
            else Show(Page.Title);
        }
    }
    public void SetVolume(int row,float value)
    {
        var settings=GameAudioSettings.Instance;
        settings.SetVolumes(row==0?value:settings.Music,row==1?value:settings.Sounds);
    }
    private void Adjust(int row,float amount)
    {
        var settings=GameAudioSettings.Instance;
        SetVolume(row,(row==0?settings.Music:settings.Sounds)+amount);
        settings.Save();
    }
    private IEnumerator StartGame()
    {
        busy=true;
        yield return new WaitForSecondsRealtime(.18f);
        music.Stop(); video.Stop();
        IsActive = false;
        Time.timeScale = 1; AudioListener.pause = false;
        var load=SceneManager.LoadSceneAsync(GameScene, LoadSceneMode.Single);
        if(load!=null) yield return load;
    }
    public Rect PanelRect()
    {
        var art=CurrentPage==Page.Options?options:CurrentPage==Page.Record?recordArtwork:menu;
        float ratio=art!=null?(float)art.width/art.height: .8f;
        float h=Mathf.Min(Screen.height*.88f,Screen.width*.83f/ratio);
        return new Rect((Screen.width-h*ratio)/2,(Screen.height-h)/2,h*ratio,h);
    }
    private Rect Map(Rect r)
    {
        Rect p=PanelRect(); return new Rect(p.x+r.x*p.width,p.y+r.y*p.height,r.width*p.width,r.height*p.height);
    }
    private Rect ButtonRect(int i) { return Map((CurrentPage==Page.Options?OptionRegions:MenuRegions)[i]); }
    private Rect BarRect(int row) { return Map(new Rect(.32f,row==0?.409f:.658f,.36f,.065f)); }
    private void Label(Rect rect,string label,int size,Color color)
    {
        if(text==null) text=new GUIStyle(GUI.skin.label) { alignment=TextAnchor.MiddleCenter,wordWrap=true };
        text.fontSize=size; text.normal.textColor=Color.black;
        GUI.Label(new Rect(rect.x+2,rect.y+3,rect.width,rect.height),label,text);
        text.normal.textColor=color; GUI.Label(rect,label,text);
    }
    public static Rect FitVideoRect(float width,float height,float aspect)
    {
        float w=Mathf.Min(width,height*aspect), h=w/aspect;
        return new Rect((width-w)/2,(height-h)/2,w,h);
    }
    private void OnGUI()
    {
        Color old=GUI.color; int depth=GUI.depth; GUI.depth=-1000; GUI.color=Color.white;
        Rect screen=new Rect(0,0,Screen.width,Screen.height);
        GUI.color=Color.black; GUI.DrawTexture(screen,Texture2D.whiteTexture); GUI.color=Color.white;
        if(video!=null && video.texture!=null)
        {
            var frame=video.texture;
            GUI.DrawTexture(FitVideoRect(Screen.width,Screen.height,(float)frame.width/frame.height),frame,ScaleMode.StretchToFill);
        }
        if(CurrentPage==Page.Intro)
        {
            Label(new Rect(Screen.width*.60f,Screen.height-65,Screen.width*.37f,45),
                "ESC para pular",Mathf.Clamp(Screen.height/36,16,30),Color.white);
        }
        else if(CurrentPage==Page.Title)
        {
            float pulse=.7f+.3f*Mathf.Sin(Time.unscaledTime*2);
            if(video==null || video.texture==null)
                Label(new Rect(0,Screen.height*.85f,Screen.width,Screen.height*.09f),"PRESSIONE ENTER",Mathf.Max(16,Screen.height/32),new Color(1,1,.8f,pulse));
        }
        else
        {
            GUI.color=new Color(0,0,0,.55f); GUI.DrawTexture(screen,Texture2D.whiteTexture); GUI.color=Color.white;
            if(CurrentPage==Page.Record)
            {
                DrawRecord();
            }
            else
            {
                var art=CurrentPage==Page.Options?options:menu;
                if(art!=null) GUI.DrawTexture(PanelRect(),art);
                if(CurrentPage==Page.Options) for(int row=0;row<2;row++)
                {
                    Rect bar=BarRect(row); float value=row==0?GameAudioSettings.Instance.Music:GameAudioSettings.Instance.Sounds;
                    GUI.color=new Color(.035f,.15f,.015f,.93f); GUI.DrawTexture(bar,Texture2D.whiteTexture);
                    GUI.color=new Color(.22f,.85f,.025f); GUI.DrawTexture(new Rect(bar.x,bar.y,bar.width*value,bar.height),Texture2D.whiteTexture);
                    GUI.color=Color.white; Label(bar,Mathf.RoundToInt(value*100)+"%",Mathf.Max(12,(int)(bar.height*.6f)),Color.white);
                }
                if(art!=null) for(int i=0;i<5;i++) DrawEmphasis(art,i);
            }
        }
        GUI.color=old; GUI.depth=depth;
    }
    private void DrawEmphasis(Texture2D art,int i)
    {
        float amount=emphasis[i]; if(amount<=0) return;
        Rect r=ButtonRect(i); float grow=r.height*.035f*amount;
        Rect raised=new Rect(r.x-grow,r.y-grow-r.height*.035f*amount,r.width+2*grow,r.height+2*grow);
        Rect region=(CurrentPage==Page.Options?OptionRegions:MenuRegions)[i];
        Rect uv=new Rect(region.x,1-region.yMax,region.width,region.height);
        GUI.color=new Color(.8f,1,.3f,.7f*amount); GUI.DrawTexture(new Rect(raised.x-14,raised.y-14,raised.width+28,raised.height+28),glow);
        GUI.color=new Color(0,0,0,.65f*amount); GUI.DrawTextureWithTexCoords(new Rect(raised.x+3,raised.y+7,raised.width,raised.height),art,uv);
        GUI.color=new Color(1,1,1,amount); GUI.DrawTextureWithTexCoords(raised,art,uv);
        GUI.color=new Color(1,1,.6f,.14f*amount); GUI.DrawTextureWithTexCoords(raised,art,uv);
        GUI.color=Color.white;
    }
    public int DisplayedRecord => Mathf.Max(0, PlayerPrefs.GetInt("HeightRecord.v1."+GameScene,0));
    private void DrawRecord()
    {
        if (recordArtwork == null) return;
        GUI.DrawTexture(PanelRect(),recordArtwork);
        // The reference includes a sample "118 m". Cover that sample using a clean
        // wood section from the same artwork, then draw the actual saved distance.
        Rect value = Map(new Rect(.20f,.385f,.60f,.20f));
        GUI.DrawTextureWithTexCoords(Map(new Rect(.103f,.33f,.794f,.325f)),recordArtwork,new Rect(.103f,1-.38f,.794f,.16f));
        var style = new GUIStyle(GUI.skin.label) { font=recordFont, alignment=TextAnchor.MiddleCenter };
        string label=DisplayedRecord+" m";
        style.fontSize=Mathf.Max(14,(int)(value.height*.72f));
        while (style.fontSize>10 && style.CalcSize(new GUIContent(label)).x>value.width*.94f) style.fontSize--;
        float edge=Mathf.Max(2,value.height*.026f);
        style.normal.textColor=new Color(.12f,.035f,0);
        GUI.Label(new Rect(value.x+edge,value.y+edge*2,value.width,value.height),label,style);
        for(int y=-1;y<=1;y++) for(int x=-1;x<=1;x++)
            if(x!=0 || y!=0) GUI.Label(new Rect(value.x+x*edge,value.y+y*edge,value.width,value.height),label,style);
        style.normal.textColor=new Color(1,.8f,.06f);
        GUI.Label(value,label,style);
        style.normal.textColor=new Color(1,1,.62f,.85f);
        GUI.Label(new Rect(value.x,value.y-edge*.5f,value.width,value.height),label,style);
    }
    private void OnDestroy()
    {
        if(activeMenu==this) activeMenu=null;
        if (!busy)
        {
            IsActive=false; Time.timeScale=1; AudioListener.pause=false;
            foreach(var root in suspendedRoots) if(root!=null) root.SetActive(true);
        }
        if(video!=null) { video.prepareCompleted-=Prepared; video.errorReceived-=VideoError; video.loopPointReached-=VideoFinished; }
        if(glow!=null) Destroy(glow);
    }
}
