using UnityEngine;
using UnityEngine.Events;

// Puntos del modo Asalto al Diamante (el "ScoreManager"):
// - llevar el diamante a tu propia base: 15 puntos
// - coger un cristal: 2 puntos
// - romper un obstaculo con tu burbuja: 1 punto
// - eliminar a otro jugador: 10 si va 1.º, 6 si va 2.º y 4 si va
//   3.º o 4.º (si esta empatado con alguien tambien cuenta como 4)
// - morir no quita puntos
// Para otras formas de ganar puntos usar SumarPuntos(jugador, cantidad).
// La tabla de puntuaciones solo tiene que leerlos:
// - GetPuntos(jugador) para saber cuantos tiene
// - alCambiarPuntos para enterarse cuando cambian
public class PuntosAD : MonoBehaviour
{
    [Header("Puntos por llevar el diamante a tu base")]
    [SerializeField, Min(1)] private int puntosPorEntrega = 15;

    // Puntos fijos del modo AD (los mismos que en el GDD)
    public const int PuntosCristal = 2;
    public const int PuntosObstaculo = 1;
    public const int PuntosEliminar1 = 10;
    public const int PuntosEliminar2 = 6;
    public const int PuntosEliminarResto = 4;

    [Tooltip("Se llama cada vez que un jugador gana puntos. " +
             "Parametros: jugador (0 = J1 ... 3 = J4) y sus puntos totales")]
    public UnityEvent<int, int> alCambiarPuntos = new UnityEvent<int, int>();

    // [0] = J1, [1] = J2...
    private int[] puntos = new int[GameSettings.MaxPlayers];

    // ---------- Sumar ----------

    // La llama el Diamante cuando el portador llega a su base
    public void SumarPunto(int jugador)
    {
        SumarPuntos(jugador, puntosPorEntrega);
    }

    // Suma la cantidad que se quiera (para otras formas de puntuar)
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

    // Cristal: +2
    public void SumarCristal(int jugador)
    {
        SumarPuntos(jugador, PuntosCristal);
    }

    // Obstaculos rotos con una burbuja: +1 cada uno
    public void SumarObstaculos(int jugador, int cantidad)
    {
        if (cantidad > 0)
            SumarPuntos(jugador, PuntosObstaculo * cantidad);
    }

    // El atacante alcanza a la victima con su burbuja
    public void SumarEliminacion(int atacante, int victima)
    {
        if (atacante == victima)
            return;

        SumarPuntos(atacante, PuntosPorEliminar(puntos, victima));
    }

    // Puntos por eliminar a la victima segun su puesto:
    // 1.º = 10, 2.º = 6, 3.º o 4.º = 4.
    // Si la victima esta empatada con alguien, 4
    // (asi al empezar, con todos a 0, nadie vale mas).
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

    // ---------- Para otros scripts (tabla, fin de partida) ----------

    public int GetPuntos(int jugador)
    {
        if (jugador < 0 || jugador >= puntos.Length)
            return 0;

        return puntos[jugador];
    }

    // Jugador con mas puntos (0 = J1 ... 3 = J4).
    // Devuelve -1 si hay empate en el primer puesto.
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