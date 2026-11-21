using UnityEngine;
using UnityEngine.Events;

public class PuntosAD : MonoBehaviour
{
    [Header("Puntos por llevar el diamante a tu base")]
    [SerializeField, Min(1)] private int puntosPorEntrega = 15;

    public const int PuntosCristal = 2;
    public const int PuntosObstaculo = 1;
    public const int PuntosEliminar1 = 10;
    public const int PuntosEliminar2 = 6;
    public const int PuntosEliminarResto = 4;

    [Tooltip("Se llama cada vez que un jugador gana puntos. " +
             "Parametros: jugador (0 = J1 ... 3 = J4) y sus puntos totales")]
    public UnityEvent<int, int> alCambiarPuntos = new UnityEvent<int, int>();

    private int[] puntos = new int[GameSettings.MaxPlayers];

    public void SumarPunto(int jugador)
    {
        SumarPuntos(jugador, puntosPorEntrega);
    }

    public void SumarPuntos(int jugador, int cantidad)
    {
        if (jugador < 0 || jugador >= puntos.Length)
            return;

        puntos[jugador] += cantidad;

        Debug.Log("Jugador " + (jugador + 1) + " tiene " +
                  puntos[jugador] + " puntos");

        if (alCambiarPuntos != null)
            alCambiarPuntos.Invoke(jugador, puntos[jugador]);
    }

    public void SumarCristal(int jugador)
    {
        SumarPuntos(jugador, PuntosCristal);
    }

    public void SumarObstaculos(int jugador, int cantidad)
    {
        if (cantidad > 0)
            SumarPuntos(jugador, PuntosObstaculo * cantidad);
    }

    public void SumarEliminacion(int atacante, int victima)
    {
        if (atacante == victima)
            return;

        SumarPuntos(atacante, PuntosPorEliminar(puntos, victima));
    }

    public static int PuntosPorEliminar(int[] lista, int victima)
    {
        if (victima < 0 || victima >= lista.Length)
            return 0;

        int porEncima = 0;
        bool empatada = false;

        for (int i = 0; i < lista.Length; i++)
        {
            if (i == victima)
                continue;

            if (lista[i] > lista[victima])
                porEncima++;
            else if (lista[i] == lista[victima])
                empatada = true;
        }

        if (empatada)
            return PuntosEliminarResto;

        if (porEncima == 0)
            return PuntosEliminar1;

        if (porEncima == 1)
            return PuntosEliminar2;

        return PuntosEliminarResto;
    }

    public int GetPuntos(int jugador)
    {
        if (jugador < 0 || jugador >= puntos.Length)
            return 0;

        return puntos[jugador];
    }

    public int Ganador()
    {
        int mejor = -1;
        int maxPuntos = -1;
        bool empate = false;

        for (int i = 0; i < puntos.Length; i++)
        {
            if (puntos[i] > maxPuntos)
            {
                maxPuntos = puntos[i];
                mejor = i;
                empate = false;
            }
            else if (puntos[i] == maxPuntos)
            {
                empate = true;
            }
        }

        return empate ? -1 : mejor;
    }
}