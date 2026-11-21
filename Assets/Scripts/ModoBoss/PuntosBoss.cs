using UnityEngine;
using UnityEngine.Events;

public class PuntosBoss : MonoBehaviour
{
    public const int PuntosImpacto = 3;
    public const int PuntosGolpeFinal = 15;
    public const int PuntosEliminar = 5;
    public const int PuntosObstaculo = 1;
    public const int PuntosDanoBoss = 3;

    [Tooltip("Se llama cada vez que cambian los puntos de un jugador. " +
             "Parametros: jugador (0 = J1 ... 3 = J4) y sus puntos totales")]
    public UnityEvent<int, int> alCambiarPuntos = new UnityEvent<int, int>();

    private int[] puntos = new int[GameSettings.MaxPlayers];

    public void SumarPuntos(int jugador, int cantidad)
    {
        if (jugador < 0 || jugador >= puntos.Length)
            return;

        int antes = puntos[jugador];
        puntos[jugador] = Mathf.Max(0, puntos[jugador] + cantidad);

        if (puntos[jugador] == antes)
            return;

        Debug.Log("Jugador " + (jugador + 1) + " tiene " +
                  puntos[jugador] + " puntos (Boss)");

        if (alCambiarPuntos != null)
            alCambiarPuntos.Invoke(jugador, puntos[jugador]);
    }

    public void SumarImpactoBoss(int jugador)
    {
        SumarPuntos(jugador, PuntosImpacto);
    }

    public void SumarGolpeFinal(int jugador)
    {
        SumarPuntos(jugador, PuntosGolpeFinal);
    }

    public void SumarEliminacion(int atacante, int victima)
    {
        if (atacante == victima)
            return;

        SumarPuntos(atacante, PuntosEliminar);
    }

    public void SumarObstaculos(int jugador, int cantidad)
    {
        if (cantidad > 0)
            SumarPuntos(jugador, PuntosObstaculo * cantidad);
    }

    public void RestarDanoBoss(int jugador)
    {
        SumarPuntos(jugador, -PuntosDanoBoss);
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
