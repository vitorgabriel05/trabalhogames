using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class BouncePad : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float bounceSpeed = 18f;
    [SerializeField] private Sprite idleSprite;

    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D surface;
    private float nextBounceTime;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (idleSprite == null)
            idleSprite = spriteRenderer.sprite;

        ShowIdle();

        surface = GetComponent<BoxCollider2D>();
        if (surface == null)
            surface = gameObject.AddComponent<BoxCollider2D>();

        surface.isTrigger = false;
        surface.size = idleSprite.bounds.size;
        surface.offset = idleSprite.bounds.center;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryBounce(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryBounce(collision);
    }

    private void TryBounce(Collision2D collision)
    {
        Rigidbody2D body = collision.rigidbody;
        if (body == null || Time.time < nextBounceTime || body.linearVelocity.y > 0.1f)
            return;

        PlayerController player = body.GetComponent<PlayerController>();
        if (player == null || body.worldCenterOfMass.y <= surface.bounds.max.y)
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            // Só a superfície superior ativa o trampolim, nunca os lados ou a base.
            if (Mathf.Abs(contact.normal.y) < 0.5f ||
                contact.point.y < surface.bounds.max.y - 0.05f)
                continue;

            player.Bounce(bounceSpeed);
            nextBounceTime = Time.time + 0.1f;
            anim.enabled = true;
            anim.Play("Base Layer.New Animation", 0, 0f);
            anim.Update(0f);
            break;
        }
    }

    private void LateUpdate()
    {
        if (anim.enabled && anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            ShowIdle();
    }

    private void ShowIdle()
    {
        anim.enabled = false;
        spriteRenderer.sprite = idleSprite;
    }

    private void OnDisable()
    {
        if (anim != null)
            ShowIdle();
    }
}
