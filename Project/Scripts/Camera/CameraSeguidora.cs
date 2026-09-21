using UnityEngine;

public class CameraSeguidora : MonoBehaviour
{
    [Header("Referência")]
    public Transform player;

    [Header("Configuração")]
    public float velocidadeSuavizacao = 5f;
    public float offsetY = 2f; // mantém o player um pouco abaixo do centro da tela

    private float alturaMaxima;

    void Start()
    {
        alturaMaxima = transform.position.y;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // A câmera só sobe, nunca desce (característica de jogo de torre)
        if (player.position.y + offsetY > alturaMaxima)
        {
            alturaMaxima = player.position.y + offsetY;
        }

        Vector3 posAlvo = new Vector3(transform.position.x, alturaMaxima, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, posAlvo, velocidadeSuavizacao * Time.deltaTime);
    }

    // Outros scripts (como o GameManager) podem consultar até onde a câmera já subiu
    public float ObterAlturaMaxima()
    {
        return alturaMaxima;
    }
}