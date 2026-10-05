using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer), typeof(Animator))]
public class SpaceFallingPlatform : MonoBehaviour
{
    [Min(0.4f)] public float warningTime = 0.9f;
    [Min(1f)] public float returnDelay = 3.5f;
    public Sprite idleSprite;
    public SpriteRenderer warningLight;
    public enum Phase { Ready, Warning, Falling, Returning }
    public Phase CurrentPhase { get; private set; }
    private Vector3 origin;
    private BoxCollider2D surface;
    private SpriteRenderer art;
    private Animator animator;
    private float elapsed;
    private float fallSpeed;

    private void Awake()
    {
        origin = transform.position;
        surface = GetComponent<BoxCollider2D>();
        art = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if (idleSprite == null) idleSprite = art.sprite;
        ResetPlatform();
    }

    private void OnCollisionEnter2D(Collision2D hit) { TryActivate(hit); }
    private void OnCollisionStay2D(Collision2D hit) { TryActivate(hit); }
    private void TryActivate(Collision2D hit)
    {
        if (CurrentPhase != Phase.Ready || hit.rigidbody == null ||
            hit.rigidbody.GetComponent<PlayerController>() == null || hit.rigidbody.linearVelocity.y > 0.1f)
            return;
        for (int i = 0; i < hit.contactCount; i++)
        {
            var contact = hit.GetContact(i);
            if (Mathf.Abs(contact.normal.y) < 0.5f || contact.point.y < surface.bounds.max.y - 0.08f)
                continue;
            CurrentPhase = Phase.Warning;
            elapsed = 0f;
            animator.enabled = true;
            animator.Play("Base Layer.New Animation", 0, 0f);
            break;
        }
    }

    private void Update()
    {
        if (CurrentPhase == Phase.Ready) return;
        elapsed += Time.deltaTime;
        if (CurrentPhase == Phase.Warning)
        {
            // Visual tremor stays smaller than the collision tolerance.
            transform.position = origin + Vector3.right * (Mathf.Sin(elapsed * 65f) * 0.035f);
            if (warningLight != null)
                warningLight.color = Color.Lerp(Color.yellow, new Color(1f, 0.15f, 0.05f), (Mathf.Sin(elapsed * 25f) + 1f) * 0.5f);
            if (elapsed < warningTime) return;
            surface.enabled = false;
            CurrentPhase = Phase.Falling;
            elapsed = 0f;
            fallSpeed = 0f;
        }
        else if (CurrentPhase == Phase.Falling)
        {
            fallSpeed += 13f * Time.deltaTime;
            transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
            art.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - elapsed / 0.85f));
            if (warningLight != null) warningLight.color = new Color(1f, 0.3f, 0f, art.color.a);
            if (elapsed < returnDelay) return;
            CurrentPhase = Phase.Returning;
            elapsed = 0f;
            transform.position = origin;
            animator.enabled = false;
            art.sprite = idleSprite;
        }
        else
        {
            float alpha = Mathf.Clamp01(elapsed / 0.6f);
            art.color = new Color(1f, 1f, 1f, alpha);
            if (warningLight != null) warningLight.color = new Color(0.2f, 1f, 0.9f, alpha);
            // Do not re-materialize a solid platform inside the player.
            if (alpha >= 1f && !PlayerOccupiesSurface()) ResetPlatform();
        }
    }

    private bool PlayerOccupiesSurface()
    {
        Vector2 center = transform.TransformPoint(surface.offset);
        Vector2 size = Vector2.Scale(surface.size, transform.lossyScale);
        foreach (var hit in Physics2D.OverlapBoxAll(center, size + Vector2.one * 0.1f, 0f))
            if (hit.attachedRigidbody != null && hit.attachedRigidbody.GetComponent<PlayerController>() != null) return true;
        return false;
    }

    private void ResetPlatform()
    {
        CurrentPhase = Phase.Ready;
        elapsed = 0f;
        transform.position = origin;
        surface.enabled = true;
        animator.enabled = false;
        art.sprite = idleSprite;
        art.color = Color.white;
        if (warningLight != null) warningLight.color = new Color(0.2f, 1f, 0.9f);
    }
}
