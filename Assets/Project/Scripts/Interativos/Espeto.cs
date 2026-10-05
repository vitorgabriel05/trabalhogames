using UnityEngine;

public class Espeto : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            MatarPlayer(other.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            MatarPlayer(collision.gameObject);
        }
    }

    private void MatarPlayer(GameObject playerObject)
    {
        PlayerMorte playerMorte = playerObject.GetComponent<PlayerMorte>();

        if (playerMorte != null)
        {
            playerMorte.Morrer();
            return;
        }

        PlayerController player = playerObject.GetComponent<PlayerController>();

        if (player != null)
        {
            player.Die();
        }
    }
}