using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Detecção de chão")]
    [SerializeField] private Transform groundCheck;   // objeto vazio na base do player
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;   // layer "Ground" (plataformas)

    private Rigidbody2D rb;
    
    private Animator anim;
    private float facingDirection = 1f;

    private float horizontalInput;
    private bool isGrounded;
    private bool isJumping;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
        }
        facingDirection = transform.localScale.x < 0f ? -1f : 1f;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Leitura do input sempre no Update (mais responsivo)
        horizontalInput = Input.GetAxisRaw("Horizontal");
        // Vira também no ar e mantém a direção ao soltar o movimento.
        if (horizontalInput != 0f)
        {
            facingDirection = horizontalInput < 0f ? -1f : 1f;
        }

        anim.SetBool("correr", horizontalInput != 0f);

        // Checagem de chão
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Mantém o pulo ativo até aterrissar, sem cancelar no início da subida.
        if (isGrounded && rb.linearVelocity.y <= 0f)
        {
            isJumping = false;
        }

        // Pulo: só se estiver no chão
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumping = true;
        }

        anim.SetBool("pular", isJumping);
    }

    private void LateUpdate()
    {
        // Aplica a direção após a animação, preservando o tamanho do personagem.
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDirection;
        transform.localScale = scale;
    }

    private void FixedUpdate()
    {
        // Movimento horizontal, preservando a velocidade vertical (queda/pulo/impulso)
        rb.linearVelocity = new Vector2(horizontalInput * speed, rb.linearVelocity.y);
    }

    // Desenha o raio de detecção de chão na cena, só pra facilitar o ajuste no Editor
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
