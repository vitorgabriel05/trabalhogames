using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FanWind : MonoBehaviour
{
    [Header("Vento")]
    [SerializeField, Min(0.1f)] private float windHeight = 4f;
    [SerializeField, Min(0.1f)] private float liftSpeed = 8f;

    private SpriteRenderer fanSprite;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();

    private void Awake()
    {
        fanSprite = GetComponent<SpriteRenderer>();
    }

    private Bounds GetWindBounds()
    {
        if (fanSprite == null)
            fanSprite = GetComponent<SpriteRenderer>();

        Bounds bounds = fanSprite.bounds;
        return new Bounds(
            new Vector3(bounds.center.x, bounds.max.y + windHeight * 0.5f, bounds.center.z),
            new Vector3(bounds.size.x, windHeight, 1f)
        );
    }

    private void FixedUpdate()
    {
        Bounds wind = GetWindBounds();
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        Physics2D.OverlapBox(wind.center, wind.size, 0f, filter, overlaps);

        foreach (Collider2D hit in overlaps)
        {
            Rigidbody2D body = hit.attachedRigidbody;
            if (body == null || body.GetComponent<PlayerController>() == null)
                continue;

            // Eleva o player sem alterar o movimento lateral ou reduzir um pulo mais forte.
            Vector2 velocity = body.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, liftSpeed);
            body.linearVelocity = velocity;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Bounds wind = GetWindBounds();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(wind.center, wind.size);
    }
}
