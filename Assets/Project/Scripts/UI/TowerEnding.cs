using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TowerEnding : MonoBehaviour
{
    private const float MessageDuration = 4.5f;
    private const string Message = "A aventura acaba aqui,\npequeno girino.\nPor enquanto...";

    private bool isEnding;
    private GUIStyle messageStyle;
    private GUIStyle subtitleStyle;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isEnding || MainMenuScreen.IsActive || PauseScreen.IsPaused) return;

        var player = collision.collider != null
            ? collision.collider.GetComponentInParent<PlayerController>()
            : null;
        if (player == null && collision.otherCollider != null)
            player = collision.otherCollider.GetComponentInParent<PlayerController>();
        if (player == null || !player.isActiveAndEnabled || player.IsDead) return;

        var playerCollider = player.GetComponent<Collider2D>();
        var platformCollider = GetComponent<Collider2D>();
        if (playerCollider == null || platformCollider == null ||
            Mathf.Abs(playerCollider.bounds.min.y - platformCollider.bounds.max.y) > .2f) return;

        isEnding = true;
        if (GameManager.Instancia != null) GameManager.Instancia.jogoAtivo = false;

        player.enabled = false;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
        }

        var pause = player.GetComponent<PauseScreen>();
        if (pause != null) pause.enabled = false;
        var limits = player.GetComponent<PlayerLimite>();
        if (limits != null) limits.enabled = false;

        Time.timeScale = 0f;
        AudioListener.pause = true;
        StartCoroutine(ReturnToMenu());
    }

    private IEnumerator ReturnToMenu()
    {
        yield return new WaitForSecondsRealtime(MessageDuration);
        SceneManager.LoadScene(MainMenuScreen.MenuScene, LoadSceneMode.Single);
    }

    private void OnGUI()
    {
        if (!isEnding) return;

        Color oldColor = GUI.color;
        int oldDepth = GUI.depth;
        GUI.depth = -1000;

        var screen = new Rect(0, 0, Screen.width, Screen.height);
        GUI.color = new Color(.015f, .035f, .025f, .82f);
        GUI.DrawTexture(screen, Texture2D.whiteTexture);

        float panelWidth = Mathf.Min(Screen.width * .82f, 900f);
        float panelHeight = Mathf.Min(Screen.height * .46f, 330f);
        var panel = new Rect((Screen.width - panelWidth) * .5f, (Screen.height - panelHeight) * .5f, panelWidth, panelHeight);
        GUI.color = new Color(.035f, .12f, .055f, .96f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(.58f, .9f, .3f, .95f);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 4f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(panel.x, panel.yMax - 4f, panel.width, 4f), Texture2D.whiteTexture);

        if (messageStyle == null)
        {
            var font = Resources.Load<Font>("UI/RecordFont");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            messageStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = Mathf.Clamp(Screen.height / 22, 22, 42)
            };
            messageStyle.normal.textColor = new Color32(226, 255, 173, 255);
            subtitleStyle = new GUIStyle(messageStyle)
            {
                fontSize = Mathf.Clamp(Screen.height / 42, 14, 22)
            };
            subtitleStyle.normal.textColor = new Color32(239, 246, 218, 255);
        }

        var messageRect = new Rect(panel.x + 28f, panel.y + panel.height * .12f, panel.width - 56f, panel.height * .62f);
        GUI.color = new Color(0, 0, 0, .75f);
        GUI.Label(new Rect(messageRect.x + 2f, messageRect.y + 3f, messageRect.width, messageRect.height), Message, messageStyle);
        GUI.color = Color.white;
        GUI.Label(messageRect, Message, messageStyle);
        GUI.Label(new Rect(panel.x + 20f, panel.y + panel.height * .77f, panel.width - 40f, panel.height * .14f),
            "Voltando ao menu...", subtitleStyle);

        GUI.color = oldColor;
        GUI.depth = oldDepth;
    }
}
