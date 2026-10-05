using UnityEngine;

public class Cenarioinfinito : MonoBehaviour
{
    public float velocidadeDoCenario;
    private void Update() { RenderAt(null, Time.time); }
    // Preserve the original map positions and sizes, scrolling horizontally only.
    public void RenderAt(Camera camera, float time)
    {
        GetComponent<Renderer>().material.mainTextureOffset = new Vector2(time * velocidadeDoCenario, 0f);
    }
}
