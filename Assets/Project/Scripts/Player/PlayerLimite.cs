using UnityEngine;

public class PlayerMorte : MonoBehaviour
{
    private PlayerController player;
    private bool morto;

    void Start()
    {
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (morto || player == null || player.IsDead || MainMenuScreen.IsActive || PauseScreen.IsPaused) return;
        var body = GetComponent<Rigidbody2D>();
        var shape = GetComponent<Collider2D>();
        float topY = shape != null ? shape.bounds.max.y : transform.position.y;
        if (body != null && body.linearVelocity.y < 0f && topY < player.FallDeathY)
        {
            Morrer();
        }
    }

    void Morrer()
    {
        morto = true;
        player.Die();
    }
}

public class PlayerLimite : MonoBehaviour
{
    [SerializeField] private float limiteEsquerdo = -5f;
    [SerializeField] private float limiteDireito = 5f;
    [SerializeField] private float margem = 0.5f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        Vector2 pos = rb.position;

        if (pos.x > limiteDireito + margem)
            pos.x = limiteEsquerdo - margem;
        else if (pos.x < limiteEsquerdo - margem)
            pos.x = limiteDireito + margem;
        else
            return;

        rb.position = pos;
        transform.position = pos;
    }
}
