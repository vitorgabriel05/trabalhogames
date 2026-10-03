using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instancia { get; private set; }

    [Header("Referências")]
    public Transform player;
    public CameraSeguidora cameraSeguidora;

    [Header("Configuração")]
    public float margemQuedaGameOver = 6f; // distância abaixo da câmera até dar game over

    [Header("Estado (somente leitura)")]
    public int pontuacao;
    public bool jogoAtivo = true;

    void Awake()
    {
        // Garante que só existe um GameManager na cena
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
    }

    void Update()
    {
        if (!jogoAtivo) return;

        AtualizarPontuacao();
        VerificarGameOver();
    }

    void AtualizarPontuacao()
    {
        // Pontuação = altura máxima já alcançada pela câmera (arredondada)
        float altura = cameraSeguidora.ObterAlturaMaxima();
        pontuacao = Mathf.Max(pontuacao, Mathf.FloorToInt(altura));
    }

    void VerificarGameOver()
    {
        float limiteInferior = cameraSeguidora.transform.position.y - margemQuedaGameOver;

        if (player.position.y < limiteInferior)
        {
            AcionarGameOver();
        }
    }

    void AcionarGameOver()
    {
        jogoAtivo = false;
        Debug.Log($"Game Over! Pontuação final: {pontuacao}");

        // HUDcontroller pode escutar esse evento, ou consultar jogoAtivo diretamente
    }

    public void ReiniciarJogo()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}