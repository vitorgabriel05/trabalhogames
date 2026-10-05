using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class PlayerMorte : MonoBehaviour
{
    [Header("Áudio")]
    [SerializeField] private AudioClip somMorte;

    [SerializeField]
    private float tempoAntesDeReiniciar = 1f;

    private AudioSource audioSource;
    private bool morto;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void Morrer()
    {
        if (morto)
            return;

        morto = true;

        Debug.Log("PLAYER MORREU");

        if (somMorte != null)
        {
            audioSource.PlayOneShot(somMorte);
        }

        StartCoroutine(ReiniciarFase());
    }

    IEnumerator ReiniciarFase()
    {
        yield return new WaitForSeconds(tempoAntesDeReiniciar);

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}