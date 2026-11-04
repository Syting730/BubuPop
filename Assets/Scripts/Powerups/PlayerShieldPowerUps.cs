using System.Collections;
using UnityEngine;

public class PlayerShieldPowerUps : MonoBehaviour
{
    [Header("Escudo")]
    [SerializeField] private float duracionEscudo = 5f;
    [SerializeField] private GameObject efectoVisualEscudo;

    private bool escudoActivo;
    private Coroutine temporizadorEscudo;

    public bool EscudoActivo => escudoActivo;

    private void Awake()
    {
        if (efectoVisualEscudo != null)
            efectoVisualEscudo.SetActive(false);
    }

    public void ActivarEscudo()
    {
        if (temporizadorEscudo != null)
            StopCoroutine(temporizadorEscudo);

        temporizadorEscudo = StartCoroutine(
            TemporizadorDelEscudo()
        );
    }

    private IEnumerator TemporizadorDelEscudo()
    {
        escudoActivo = true;

        if (efectoVisualEscudo != null)
            efectoVisualEscudo.SetActive(true);

        yield return new WaitForSeconds(duracionEscudo);

        escudoActivo = false;

        if (efectoVisualEscudo != null)
            efectoVisualEscudo.SetActive(false);

        temporizadorEscudo = null;
    }
}