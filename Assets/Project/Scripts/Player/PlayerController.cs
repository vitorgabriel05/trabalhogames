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

    private float horizontalInput;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Leitura do input sempre no Update (mais responsivo)
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Checagem de chão
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Pulo: só se estiver no chão
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
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