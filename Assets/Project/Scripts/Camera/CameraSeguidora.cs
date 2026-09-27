using UnityEngine;

public class CameraSeguidora : MonoBehaviour
{
    [Header("Alvo")]
    [SerializeField] private Transform player;

    [Header("Movimento")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float offsetY = 1f; // câmera um pouco acima do player

    [Header("Limite de descida")]
    [SerializeField] private float maxDescentBelowPeak = 6f; // quanto a câmera pode "afundar" abaixo do pico já alcançado

    private float highestY; // maior altura (pico) já alcançada pela câmera

    private void Start()
    {
        highestY = transform.position.y;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        float rawTargetY = player.position.y + offsetY;

        // Atualiza o pico se o player subiu mais do que a câmera já esteve
        if (rawTargetY > highestY)
        {
            highestY = rawTargetY;
        }

        // A câmera segue o player pra baixo, mas nunca abaixo de (pico - margem)
        float clampedTargetY = Mathf.Max(rawTargetY, highestY - maxDescentBelowPeak);

        Vector3 targetPosition = new Vector3(transform.position.x, clampedTargetY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }

    // Usado pelo GameManager para saber a maior altura já alcançada (ex: pontuação)
    public float ObterAlturaMaxima()
    {
        return highestY;
    }
}