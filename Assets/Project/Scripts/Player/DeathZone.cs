using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log("PLAYER CAIU NA DEATHZONE");

        PlayerMorte playerMorte = other.GetComponent<PlayerMorte>();

        if (playerMorte != null)
        {
            playerMorte.Morrer();
        }
    }
}