using UnityEngine;

public class Cristal : MonoBehaviour
{
    private GeneradorCristales generador;
    private bool cogido = false;

    public void SetGenerador(GeneradorCristales g)
    {
        generador = g;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (cogido)
            return;

        PlayerMovement jugador = otro.GetComponentInParent<PlayerMovement>();
        if (jugador == null)
            return;

        PartidaManager partida = FindAnyObjectByType<PartidaManager>();
        PuntosAD puntos = FindAnyObjectByType<PuntosAD>();

        int indice = partida != null ? partida.IndiceDe(jugador.gameObject) : -1;
        if (indice < 0 || puntos == null)
            return;

        cogido = true;
        puntos.SumarCristal(indice);

        Debug.Log("Jugador " + (indice + 1) + " ha cogido un cristal (+" +
                  PuntosAD.PuntosCristal + ")");

        if (generador != null)
            generador.CristalCogido(this);

        Destroy(gameObject);
    }
}
