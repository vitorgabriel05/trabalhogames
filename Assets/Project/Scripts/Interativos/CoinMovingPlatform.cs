using UnityEngine;
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class CoinMovingPlatform : MonoBehaviour
{
    public float amplitude = .45f;
    public float period = 4f;
    public float phase;
    private Rigidbody2D body;
    private Vector2 origin;
    private void Awake() { body = GetComponent<Rigidbody2D>(); origin = body.position; body.bodyType = RigidbodyType2D.Kinematic; body.interpolation = RigidbodyInterpolation2D.Interpolate; }
    private void FixedUpdate() { body.MovePosition(origin + Vector2.up * (Mathf.Sin(Time.time * 2f * Mathf.PI / period + phase) * amplitude)); }
}
