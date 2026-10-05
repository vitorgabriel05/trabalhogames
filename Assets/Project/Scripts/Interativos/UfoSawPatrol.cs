using UnityEngine;

// The blade is lethal; the alien pilot and its tractor beam are visual only.
[RequireComponent(typeof(SawHazard), typeof(Rigidbody2D))]
public class UfoSawPatrol : MonoBehaviour
{
    public Vector2 travel = new Vector2(1.4f, 0f);
    [Min(1f)] public float period = 5f;
    public float phase;
    public Transform ship;
    public SpriteRenderer engine;
    private Rigidbody2D body;
    private Vector2 origin;
    private Vector3 shipOrigin;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        origin = body.position;
        if (ship != null) shipOrigin = ship.localPosition;
    }

    private void FixedUpdate()
    {
        // Start at the authored position, with smooth reversals at either end.
        float wave = Mathf.Sin(Time.time * Mathf.PI * 2f / period + phase);
        body.MovePosition(origin + travel * wave);
    }

    private void LateUpdate()
    {
        if (ship != null)
        {
            ship.localPosition = shipOrigin + Vector3.up * (Mathf.Sin(Time.time * 3f + phase) * 0.025f);
            ship.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2f + phase) * 3f);
        }
        if (engine != null)
            engine.color = new Color(0.35f, 0.9f, 1f, 0.55f + Mathf.Sin(Time.time * 9f) * 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)origin : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center - (Vector3)travel, center + (Vector3)travel);
        Gizmos.DrawWireSphere(center - (Vector3)travel, 0.12f);
        Gizmos.DrawWireSphere(center + (Vector3)travel, 0.12f);
    }
}
