using UnityEngine;
using UnityEngine.Events;

// Puntos del modo Asalto al Diamante.
// Llevar el diamante a tu propia base da 15 puntos.
// Para otras formas de ganar puntos (por ejemplo, matar a otro
// jugador) usar SumarPuntos(jugador, cantidad).
// La tabla de puntuaciones solo tiene que leerlos:
// - GetPuntos(jugador) para saber cuantos tiene
// - alCambiarPuntos para enterarse cuando cambian
public class PuntosAD : MonoBehaviour
{
    [Header("Puntos por llevar el diamante a tu base")]
    [SerializeField, Min(1)] private int puntosPorEntrega = 15;

    [Tooltip("Se llama cada vez que un jugador gana puntos. " +
             "Parametros: jugador (0 = J1 ... 3 = J4) y sus puntos totales")]
    public UnityEvent<int, int> alCambiarPuntos;

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

        alCambiarPuntos.Invoke(jugador, puntos[jugador]);
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