using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[InitializeOnLoad]
public static class MainMenuValidation
{
    private const string Stage = "MainMenuValidation.Stage";
    private static double deadline;
    static MainMenuValidation() { EditorApplication.update += Tick; }
    public static void Run()
    {
        SessionState.SetBool(Stage+"MusicExists",PlayerPrefs.HasKey(GameAudioSettings.MusicKey));
        SessionState.SetBool(Stage+"SoundsExists",PlayerPrefs.HasKey(GameAudioSettings.SoundsKey));
        SessionState.SetFloat(Stage+"Music",PlayerPrefs.GetFloat(GameAudioSettings.MusicKey,.75f));
        SessionState.SetFloat(Stage+"Sounds",PlayerPrefs.GetFloat(GameAudioSettings.SoundsKey,.8f));
        Require(EditorBuildSettings.scenes[0].path == MainMenuScreen.MenuScene, "Title scene is first in build");
        Require(Resources.Load<Texture2D>("UI/MainMenu") != null && Resources.Load<Texture2D>("UI/Options") != null && Resources.Load<Texture2D>("UI/Record")!=null, "All three supplied menu textures imported");
        Require(Resources.Load<AudioClip>("Audio/TitleTheme").length > 70, "Original title composition imported");
        Require(Resources.Load<VideoClip>("Video/TitleLoop") != null, "Supplied video imported");
        foreach(var size in new[]{new Vector2(1100,480),new Vector2(480,800),new Vector2(1280,720),new Vector2(2560,1080)})
        {
            Rect fit=MainMenuScreen.FitVideoRect(size.x,size.y,16f/9f);
            Require(fit.xMin>=0 && fit.yMin>=0 && fit.xMax<=size.x+.01f && fit.yMax<=size.y+.01f && Mathf.Abs(fit.width/fit.height-16f/9f)<.001f,"Entire video fits viewport "+size);
        }
        var viewType=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var zoomField=viewType?.GetField("m_ZoomArea",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        Require(zoomField!=null && viewType.GetField("m_defaultScale",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!=null && zoomField.FieldType.GetMethod("SetTransform",new[]{typeof(Vector2),typeof(Vector2)})!=null,"Editor supports resetting preview zoom to fit");
        // Reproduce entering Play with the game and menu open together.
        EditorSceneManager.OpenScene(MainMenuScreen.GameScene);
        var menuScene=EditorSceneManager.OpenScene(MainMenuScreen.MenuScene,OpenSceneMode.Additive);
        SceneManager.SetActiveScene(menuScene);
        SessionState.SetInt(Stage,1);
        EditorApplication.EnterPlaymode();
    }
    private static void Tick()
    {
        int stage=SessionState.GetInt(Stage,0);
        if(stage==0 || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if(stage==1)
            {
                var menu=UnityEngine.Object.FindAnyObjectByType<MainMenuScreen>(); if(menu==null) return;
                Require(menu.CurrentPage==MainMenuScreen.Page.Intro, "Starts with opening before title");
                Require(MainMenuScreen.IsActive && Time.timeScale==0 && AudioListener.pause,"Title blocks simulation and gameplay audio");
                foreach(var root in SceneManager.GetSceneByPath(MainMenuScreen.GameScene).GetRootGameObjects())
                    Require(!root.activeInHierarchy,"Game scene roots inactive behind title: "+root.name);
                var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
                Require(player!=null && !player.isActiveAndEnabled,"Player cannot process WASD or arrow input behind title");
                player.GetComponent<PauseScreen>().Open();
                Require(!PauseScreen.IsPaused,"Pause refuses to open while title is active");
                var velocity=player.GetComponent<Rigidbody2D>().linearVelocity;
                player.Bounce(12); player.Die();
                Require(!player.IsDead && player.GetComponent<Rigidbody2D>().linearVelocity==velocity,"External jump/death actions cannot change player behind menu");
                var video=menu.GetComponent<VideoPlayer>();
                Require(!video.isLooping && video.audioOutputMode==VideoAudioOutputMode.AudioSource, "Opening plays once with its own audio");
                menu.FinishIntro();
                Require(menu.CurrentPage==MainMenuScreen.Page.Title && video.isLooping && video.audioOutputMode==VideoAudioOutputMode.None, "Skip stops opening audio and starts silent title loop");
                var audio=GameAudioSettings.Instance;
                audio.SetVolumes(.3f,.6f);
                var source=new GameObject("Validation music").AddComponent<AudioSource>(); source.volume=.5f;
                source.transform.SetParent(menu.transform);
                GameAudioSettings.Register(source,true);
                Require(Mathf.Abs(source.volume-.15f)<.001f, "Independent music gain");
                audio.SetVolumes(0,.6f); Require(source.volume==0, "Music mutes completely");
                audio.SetVolumes(1,.6f); Require(Mathf.Abs(source.volume-.5f)<.001f, "Volume can recover from zero");
                var sound=new GameObject("Validation effect").AddComponent<AudioSource>(); sound.volume=.4f;
                sound.transform.SetParent(menu.transform);
                GameAudioSettings.Register(sound,false);
                Require(Mathf.Abs(sound.volume-.24f)<.001f, "Effects use independent gain");
                var background=new GameObject("Background music regression").AddComponent<AudioSource>();
                background.volume=.8f; background.ignoreListenerPause=true;
                GameAudioSettings.Register(background,true);
                Require(background.volume==0,"Even audio bypassing listener pause is muted outside the menu");
                UnityEngine.Object.Destroy(background.gameObject);
                menu.Show(MainMenuScreen.Page.Options); menu.SetVolume(0,.4f); menu.SetVolume(1,.2f);
                Require(Mathf.Abs(audio.Music-.4f)<.001f && Mathf.Abs(audio.Sounds-.2f)<.001f, "Both option controls update independent values");
                menu.Show(MainMenuScreen.Page.Menu);
                Require(Mathf.Abs(PlayerPrefs.GetFloat(GameAudioSettings.MusicKey)-.4f)<.001f, "Volume preferences saved");
                menu.Show(MainMenuScreen.Page.Record); Require(menu.CurrentPage==MainMenuScreen.Page.Record,"Record page accessible");
                Require(menu.DisplayedRecord==Mathf.Max(0,PlayerPrefs.GetInt("HeightRecord.v1."+MainMenuScreen.GameScene,0)),"Record artwork displays saved value rather than reference sample");
                menu.Show(MainMenuScreen.Page.Menu);
                deadline=EditorApplication.timeSinceStartup+30;
                SessionState.SetInt(Stage,2);
            }
            else if(stage==2)
            {
                var menu=UnityEngine.Object.FindAnyObjectByType<MainMenuScreen>();
                var video=menu.GetComponent<VideoPlayer>();
                if(video.texture==null || video.frame<2)
                {
                    if(EditorApplication.timeSinceStartup>=deadline) throw new InvalidOperationException("Video produces decoded frames within 30 seconds");
                    return;
                }
                Require(video.isPlaying,"Video decodes and plays");
                Require(menu.PanelRect().height<=Screen.height && menu.PanelRect().width<=Screen.width,"Panel fits viewport");
                ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../MenuPreview.png"));
                deadline=EditorApplication.timeSinceStartup+1; SessionState.SetInt(Stage,4);
            }
            else if(stage==4 && EditorApplication.timeSinceStartup>=deadline)
            {
                UnityEngine.Object.FindAnyObjectByType<MainMenuScreen>().Show(MainMenuScreen.Page.Options);
                deadline=EditorApplication.timeSinceStartup+1; SessionState.SetInt(Stage,5);
            }
            else if(stage==5 && EditorApplication.timeSinceStartup>=deadline)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../OptionsPreview.png"));
                deadline=EditorApplication.timeSinceStartup+1; SessionState.SetInt(Stage,6);
            }
            else if(stage==6 && EditorApplication.timeSinceStartup>=deadline)
            {
                var menu=UnityEngine.Object.FindAnyObjectByType<MainMenuScreen>();
                menu.Show(MainMenuScreen.Page.Menu); menu.Confirm();
                deadline=EditorApplication.timeSinceStartup+30; SessionState.SetInt(Stage,3);
            }
            else if(stage==3)
            {
                if(SceneManager.GetActiveScene().path!=MainMenuScreen.GameScene)
                {
                    if(EditorApplication.timeSinceStartup>=deadline) throw new InvalidOperationException("Start loads game within 30 seconds");
                    return;
                }
                Require(UnityEngine.Object.FindAnyObjectByType<PlayerController>()!=null,"Start loads existing game and player");
                Require(UnityEngine.Object.FindAnyObjectByType<MainMenuScreen>()==null,"Title music and video owner destroyed after start");
                Require(GameAudioSettings.Instance!=null && Mathf.Abs(GameAudioSettings.Instance.Music-.4f)<.001f,"Audio settings survive scene change");
                Require(Time.timeScale==1 && !AudioListener.pause,"Game starts with normal time and audio");
                Require(!MainMenuScreen.IsActive && SceneManager.sceneCount==1,"Start unloads old scenes and releases gameplay gate");
                foreach(var source in UnityEngine.Object.FindObjectsByType<AudioSource>())
                    if(source.clip!=null && source.clip.name=="MusicManager")
                        Require(source.isPlaying,"Gameplay music starts only after starting the game");
                RestorePreferences();
                SessionState.SetInt(Stage,0); Debug.Log("[MainMenuValidation] PASS");
                EditorApplication.Exit(0);
            }
        }
        catch(Exception error) { RestorePreferences(); SessionState.SetInt(Stage,0); Debug.LogException(error); EditorApplication.Exit(1); }
    }
    private static void RestorePreferences()
    {
        if(GameAudioSettings.Instance!=null) GameAudioSettings.Instance.SetVolumes(SessionState.GetFloat(Stage+"Music",.75f),SessionState.GetFloat(Stage+"Sounds",.8f));
        if(!SessionState.GetBool(Stage+"MusicExists",false)) PlayerPrefs.DeleteKey(GameAudioSettings.MusicKey);
        if(!SessionState.GetBool(Stage+"SoundsExists",false)) PlayerPrefs.DeleteKey(GameAudioSettings.SoundsKey);
        PlayerPrefs.Save();
    }
    private static void Require(bool value,string description)
    {
        if(!value) throw new InvalidOperationException(description);
        Debug.Log("[MainMenuValidation] "+description);
    }
}
