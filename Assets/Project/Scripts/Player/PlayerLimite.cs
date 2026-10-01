using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMorte : MonoBehaviour
{
    [SerializeField] private float margem = 3f; // folga abaixo da tela antes de morrer
    private Camera cam;
    private bool morto;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (morto) return;

        // borda inferior da câmera no mundo (ortográfica 2D)
        float bordaInferior = cam.transform.position.y - cam.orthographicSize;

        if (transform.position.y < bordaInferior - margem)
        {
            Morrer();
        }
    }

    void Morrer()
    {
        morto = true;
        // opção simples: reiniciar a fase
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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