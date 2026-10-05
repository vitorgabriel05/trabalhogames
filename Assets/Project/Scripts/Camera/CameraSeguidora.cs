

using UnityEngine;

public class CameraSeguidora : MonoBehaviour
{
    [Header("Alvo")]
    [SerializeField] private Transform player;

    [Header("Movimento")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float offsetY = 1f; // câmera um pouco acima do player

    private float highestY; // maior altura (pico) já alcançada pela câmera
    private float initialY;
    private Rigidbody2D playerBody;
    private Camera view;

    private void Start()
    {
        highestY = transform.position.y;
        initialY = transform.position.y;
        playerBody = player != null ? player.GetComponent<Rigidbody2D>() : null;
        view = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (player == null || MainMenuScreen.IsActive || PauseScreen.IsPaused || Time.timeScale == 0) return;

        float rawTargetY = player.position.y + offsetY;

        // Atualiza o pico se o player subiu mais do que a câmera já esteve
        if (rawTargetY > highestY)
        {
            highestY = rawTargetY;
        }

        // Follow the descent too, looking ahead so fast falls stay visible.
        float lookAhead = playerBody != null ? Mathf.Clamp(playerBody.linearVelocity.y * .2f, -2.5f, 0f) : 0f;
        float targetY = Mathf.Max(initialY, rawTargetY + lookAhead);
        float nextY = Mathf.Lerp(transform.position.y, targetY, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        if (view != null && view.orthographic)
        {
            // Keep the player inside the central 60% even when smoothing lags.
            float margin = view.orthographicSize * .6f;
            nextY = Mathf.Clamp(nextY, player.position.y - margin, player.position.y + margin);
        }
        transform.position = new Vector3(transform.position.x, Mathf.Max(initialY, nextY), transform.position.z);
    }

    // Usado pelo GameManager para saber a maior altura já alcançada (ex: pontuação)
    public float ObterAlturaMaxima()
    {
        return highestY;
    }
}
