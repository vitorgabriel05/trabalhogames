using UnityEngine;

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

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    private float horizontalInput;

    private bool isGrounded;
    private bool isJumping;
    private bool isRunning;

    private int jumpCount;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
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

        anim.SetBool("pular", isJumping);
        anim.SetBool("cair", isFalling);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(
            horizontalInput * speed,
            rb.linearVelocity.y
        );
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