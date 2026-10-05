using UnityEngine;

// The original Espeto component still handles death. Its collider is armed
// only while the blades are extended; the housing is never lethal.
[RequireComponent(typeof(Espeto), typeof(SpriteRenderer))]
public class SpaceRetractingSpikes : MonoBehaviour
{
    [Min(0.8f)] public float safeTime = 2.2f;
    [Min(0.4f)] public float warningTime = 0.65f;
    [Min(0.5f)] public float activeTime = 1.3f;
    public float phaseOffset;
    public Transform blades;
    public SpriteRenderer warningLight;
    public bool IsArmed { get; private set; }
    private Collider2D[] lethalColliders;
    private Vector3 extendedPosition;
    private Vector3 extendedScale;

    private void Awake()
    {
        lethalColliders = GetComponents<Collider2D>();
        if (blades == null) blades = transform;
        extendedPosition = blades.localPosition;
        extendedScale = blades.localScale;
        SetArmed(false);
        AnimateAt(0f);
    }

    private void Update() { AnimateAt(Time.time); }

    public void AnimateAt(float time)
    {
        float movementTime = 0.22f;
        float cycle = safeTime + warningTime + activeTime + movementTime * 2f;
        float t = Mathf.Repeat(time + phaseOffset, cycle);
        float extension;
        Color light;
        if (t < safeTime)
        {
            extension = 0f;
            light = new Color(0.2f, 1f, 0.85f);
        }
        else if (t < safeTime + warningTime)
        {
            extension = 0f;
            light = Color.Lerp(new Color(1f, 0.35f, 0f), Color.yellow, (Mathf.Sin(t * 28f) + 1f) * 0.5f);
        }
        else
        {
            float action = t - safeTime - warningTime;
            extension = action < movementTime ? Mathf.SmoothStep(0f, 1f, action / movementTime)
                : action < movementTime + activeTime ? 1f
                : 1f - Mathf.SmoothStep(0f, 1f, (action - movementTime - activeTime) / movementTime);
            light = new Color(1f, 0.15f, 0.1f);
        }
        // Anchor the bottom of the blades to the mechanical housing.
        float height = GetComponent<SpriteRenderer>().sprite.bounds.size.y * extendedScale.y;
        blades.localScale = new Vector3(extendedScale.x, extendedScale.y * Mathf.Max(0.025f, extension), extendedScale.z);
        blades.localPosition = extendedPosition - Vector3.up * (height * (1f - extension) * 0.5f);
        SetArmed(extension >= 0.98f);
        if (warningLight != null) warningLight.color = light;
    }

    private void SetArmed(bool armed)
    {
        IsArmed = armed;
        if (lethalColliders == null) return;
        foreach (var collider in lethalColliders) collider.enabled = armed;
    }
}
