using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cenarioinfinito : MonoBehaviour

{
    public float velocidadeDoCenario;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
        MovimentarCenario();
    }

   private void MovimentarCenario()
    {

        UnityEngine.Vector2 deslocamento = new UnityEngine.Vector2(Time.time * velocidadeDoCenario, 0);
        GetComponent<Renderer>().material.mainTextureOffset = deslocamento;
        
    }
}
