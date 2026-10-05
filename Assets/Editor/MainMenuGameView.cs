using System.Reflection;
using UnityEditor;
using UnityEngine;

// Game View zoom is an editor setting, independent of the video's aspect ratio.
// Reset a zoomed preview once on menu entry so its edges are visible in Play.
[InitializeOnLoad]
public static class MainMenuGameView
{
    private static bool adjusted;
    static MainMenuGameView() { EditorApplication.update += Update; }
    private static void Update()
    {
        if(!EditorApplication.isPlaying || !MainMenuScreen.IsActive) { adjusted=false; return; }
        if(adjusted || Application.isBatchMode) return;
        var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if(type==null) return;
        foreach(var window in Resources.FindObjectsOfTypeAll(type))
        {
            var zoom=type.GetField("m_ZoomArea",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(window);
            if(zoom==null) continue;
            var reset=zoom.GetType().GetMethod("SetTransform",new[]{typeof(Vector2),typeof(Vector2)});
            if(reset==null) continue;
            var defaultValue=type.GetField("m_defaultScale",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(window);
            float fit=defaultValue is float value && value>0 ? value : 1f;
            reset.Invoke(zoom,new object[]{Vector2.zero,Vector2.one*fit});
            type.GetMethod("EnforceZoomAreaConstraints",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(window,null);
            ((EditorWindow)window).Repaint();
            adjusted=true;
        }
    }
}
