using System.Collections.Generic;
using UnityEngine;

// One persistent owner applies separate gains to every current and future source.
public class GameAudioSettings : MonoBehaviour
{
    public const string MusicKey = "Leproso.Audio.Music", SoundsKey = "Leproso.Audio.Sounds";
    public static GameAudioSettings Instance { get; private set; }
    public float Music { get; private set; }
    public float Sounds { get; private set; }
    private struct Channel { public float gain; public bool music; }
    private readonly Dictionary<AudioSource, Channel> channels = new Dictionary<AudioSource, Channel>();
    private readonly List<AudioSource> removed = new List<AudioSource>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null) new GameObject("Game audio settings").AddComponent<GameAudioSettings>();
    }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, .75f));
        Sounds = Mathf.Clamp01(PlayerPrefs.GetFloat(SoundsKey, .8f));
    }
    public static void Register(AudioSource source, bool music)
    {
        if (source == null || Instance == null) return;
        if (!Instance.channels.TryGetValue(source, out var channel)) channel.gain = source.volume;
        channel.music = music;
        Instance.channels[source] = channel;
        source.volume = MainMenuScreen.AllowsAudio(source) ? channel.gain * (music ? Instance.Music : Instance.Sounds) : 0;
    }
    public void SetVolumes(float music, float sounds)
    {
        Music = Mathf.Clamp01(music); Sounds = Mathf.Clamp01(sounds);
        PlayerPrefs.SetFloat(MusicKey, Music); PlayerPrefs.SetFloat(SoundsKey, Sounds);
        Apply();
    }
    private void LateUpdate()
    {
        foreach (var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include))
            if (!channels.ContainsKey(source)) Register(source, source.clip != null && source.clip.name == "MusicManager");
        Apply();
    }
    private void Apply()
    {
        removed.Clear();
        foreach (var pair in channels)
            if (pair.Key == null) removed.Add(pair.Key);
            else pair.Key.volume = MainMenuScreen.AllowsAudio(pair.Key) ? pair.Value.gain * (pair.Value.music ? Music : Sounds) : 0;
        foreach (var source in removed) channels.Remove(source);
    }
    public void Save() { PlayerPrefs.Save(); }
    private void OnApplicationQuit() { Save(); }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
