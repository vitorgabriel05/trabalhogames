using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SawHazard : MonoBehaviour
{
    private void Awake()
    {
        CircleCollider2D blade = GetComponent<CircleCollider2D>();
        if (blade == null)
        {
            blade = gameObject.AddComponent<CircleCollider2D>();
            Bounds spriteBounds = GetComponent<SpriteRenderer>().sprite.bounds;
            blade.offset = spriteBounds.center;
            blade.radius = Mathf.Min(spriteBounds.extents.x, spriteBounds.extents.y);
        }
        blade.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        KillPlayer(other.attachedRigidbody);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        KillPlayer(other.attachedRigidbody);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        KillPlayer(collision.rigidbody);
    }

    private void KillPlayer(Rigidbody2D body)
    {
        if (body != null && body.TryGetComponent(out PlayerController player))
            player.Die();
    }
}
