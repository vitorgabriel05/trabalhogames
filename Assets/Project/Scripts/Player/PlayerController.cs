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
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float deathDelay = 1f;
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
        gameCamera = Camera.main;
    }

    private void Update()
    {
        if (isDead) return;
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

            audioSource.PlayOneShot(jumpSound);
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
        if (isDead) return;
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
        if (isDead) return;
        rb.linearVelocity = new Vector2(
            horizontalInput * speed,
            rb.linearVelocity.y
        );
    }

    private void LateUpdate()
    {
        if (isDead) return;
        if (gameCamera == null)
            gameCamera = Camera.main;
        if (gameCamera == null) return;

        // Reinicia quando o corpo inteiro sai pelo limite inferior da tela.
        Vector3 top = playerCollider != null
            ? new Vector3(playerCollider.bounds.center.x, playerCollider.bounds.max.y, transform.position.z)
            : transform.position;
        if (rb.linearVelocity.y < 0f && gameCamera.WorldToViewportPoint(top).y < 0f)
            Die();
    }

    public void Die()
    {
    if (isDead) return;

    isDead = true;

    rb.linearVelocity = Vector2.zero;
    rb.simulated = false;

    if (audioSource != null && deathSound != null)
    {
        audioSource.PlayOneShot(deathSound);
    }

    Invoke(nameof(ReiniciarCena), deathDelay);
    }
    private void ReiniciarCena()
{
    Time.timeScale = 1f;
    SceneManager.LoadScene(SceneManager.GetActiveScene().path);
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
