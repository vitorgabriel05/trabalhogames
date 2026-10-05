using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Pulo")]
    [SerializeField] private int maxJumps = 2;

    [Header("Detecção de chão")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpSound;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    private float horizontalInput;

    private bool isGrounded;
    private bool isJumping;
    private bool isRunning;

    private int jumpCount;
    private Camera gameCamera;
    private Collider2D playerCollider;
    private bool isDead;
    public bool IsDead => isDead;
    public const float DeathDuration = PlayerDeathEffect.Duration;
    // Falling is lethal only below the bottom of the level, never below the camera.
    [SerializeField, Min(6f)] private float fallDepthBelowStart = 12f;
    public float FallDeathY { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
        gameCamera = Camera.main;
        FallDeathY = transform.position.y - fallDepthBelowStart;
        if (GetComponent<PauseScreen>() == null) gameObject.AddComponent<PauseScreen>();
    }

    private void Update()
    {
        if (MainMenuScreen.IsActive || isDead || PauseScreen.IsPaused) return;
        // =========================
        // MOVIMENTO
        // =========================

        horizontalInput = Input.GetAxisRaw("Horizontal");

        isRunning = Mathf.Abs(horizontalInput) > 0.01f;

        anim.SetBool("correr", isRunning);

        // Vira o personagem
        if (horizontalInput > 0f)
        {
            spriteRenderer.flipX = false;
        }
        else if (horizontalInput < 0f)
        {
            spriteRenderer.flipX = true;
        }

        // =========================
        // DETECÇÃO DE CHÃO
        // =========================

        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        // Quando aterrissa, libera novamente os dois pulos
        if (isGrounded && rb.linearVelocity.y <= 0f)
        {
            jumpCount = 0;
            isJumping = false;
        }

        // =========================
        // PULO / PULO DUPLO
        // =========================

        if (Input.GetButtonDown("Jump") && jumpCount < maxJumps)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );

            jumpCount++;
            isJumping = true;

            if (audioSource != null && jumpSound != null)
            {
                audioSource.PlayOneShot(jumpSound);
            }
        }

        // =========================
        // QUEDA
        // =========================

        bool isFalling =
            !isGrounded &&
            rb.linearVelocity.y < 0f;

        // Quando começa a cair, para animação de pulo
        if (isFalling)
        {
            isJumping = false;
        }

        // =========================
        // ANIMAÇÕES
        // =========================

        anim.SetBool("pular", isJumping && jumpCount == 1);
        anim.SetBool("puloduplo", isJumping && jumpCount > 1);
        anim.SetBool("cair", isFalling);
    }

    public void Bounce(float speed)
    {
        if (MainMenuScreen.IsActive || isDead || PauseScreen.IsPaused) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, speed);
        jumpCount = 1;
        isGrounded = false;
        isJumping = true;
        anim.SetBool("pular", true);
        anim.SetBool("puloduplo", false);
        anim.SetBool("cair", false);
    }

    private void FixedUpdate()
    {
        if (MainMenuScreen.IsActive || isDead || PauseScreen.IsPaused) return;
        rb.linearVelocity = new Vector2(
            horizontalInput * speed,
            rb.linearVelocity.y
        );
    }

    private void LateUpdate()
    {
        if (MainMenuScreen.IsActive || isDead || PauseScreen.IsPaused) return;
        if (gameCamera == null)
            gameCamera = Camera.main;
        float topY = playerCollider != null ? playerCollider.bounds.max.y : transform.position.y;
        if (rb.linearVelocity.y < 0f && topY < FallDeathY)
            BeginDeath(true);
    }

    public void Die()
    {
        BeginDeath(false);
    }

    private void BeginDeath(bool fellOffScreen)
    {
        if (MainMenuScreen.IsActive || isDead || PauseScreen.IsPaused) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (playerCollider != null) playerCollider.enabled = false;
        var limits = GetComponent<PlayerLimite>();
        if (limits != null) limits.enabled = false;
        var follow = gameCamera != null ? gameCamera.GetComponent<CameraSeguidora>() : null;
        if (follow != null) follow.enabled = false;
        Time.timeScale = 1f;
        if (fellOffScreen && gameCamera != null)
        {
            // Keep the fall reaction visible just inside the lower edge.
            Vector3 position = transform.position;
            position.y = gameCamera.transform.position.y - gameCamera.orthographicSize
                + spriteRenderer.bounds.extents.y + 0.2f;
            transform.position = position;
        }
        foreach (var source in FindObjectsByType<AudioSource>())
            if (source.isPlaying) source.Stop();
        var hud = GetComponent<HeightRecordHUD>();
        if (hud != null) hud.HideForDeath();
        StartCoroutine(AnimateDeath());
    }

    private IEnumerator AnimateDeath()
    {
        anim.enabled = false;
        var effect = GetComponent<PlayerDeathEffect>();
        if (effect == null) effect = gameObject.AddComponent<PlayerDeathEffect>();
        effect.Begin(spriteRenderer, gameCamera);
        float elapsed = 0f;
        while (elapsed < DeathDuration)
        {
            effect.RenderAt(elapsed);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        effect.RenderAt(DeathDuration);
        gameObject.AddComponent<GameOverScreen>().Show();
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}
