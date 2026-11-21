using UnityEngine;
using UnityEngine.Events;

// Puntos del modo Boss Volcán (tarjeta "Lab3 - MB - Tabla de puntuaciones"):
// - impacto de tu burbuja al Boss: +3 puntos
// - golpe final (el impacto que deja al Boss a 0 HP): +15 puntos
//   (en lugar de los 3 de un impacto normal)
// - eliminar a otro jugador: +5 puntos
// - romper un obstaculo con tu burbuja: +1 punto
//   (los obstaculos NO sueltan power-ups: solo salen de los spawners)
// - recibir daño del Boss (fuego en cruz, erupcion): -3 puntos, minimo 0
//
// Quien lo usa:
// - VidaBoss llama a SumarImpactoBoss / SumarGolpeFinal sola
// - la burbuja (Boom) llama a SumarEliminacion y SumarObstaculos
// - el fuego del Boss llama a RestarDanoBoss(jugador)
// La tabla de puntuaciones solo tiene que leerlos:
// - GetPuntos(jugador) y el evento alCambiarPuntos
public class PuntosBoss : MonoBehaviour
{
    // Puntos fijos del modo Boss (los mismos que en el GDD)
    public const int PuntosImpacto = 3;
    public const int PuntosGolpeFinal = 15;
    public const int PuntosEliminar = 5;
    public const int PuntosObstaculo = 1;
    public const int PuntosDanoBoss = 3;

    [Tooltip("Se llama cada vez que cambian los puntos de un jugador. " +
             "Parametros: jugador (0 = J1 ... 3 = J4) y sus puntos totales")]
    public UnityEvent<int, int> alCambiarPuntos = new UnityEvent<int, int>();

    // [0] = J1, [1] = J2...
    private int[] puntos = new int[GameSettings.MaxPlayers];

    // ---------- Sumar y restar ----------

    // Suma (o resta, si es negativa) la cantidad que se quiera.
    // Nunca baja de 0.
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

    // Impacto normal al Boss: +3
    public void SumarImpactoBoss(int jugador)
    {
        SumarPuntos(jugador, PuntosImpacto);
    }

    // Golpe final al Boss: +15
    public void SumarGolpeFinal(int jugador)
    {
        SumarPuntos(jugador, PuntosGolpeFinal);
    }

    // El atacante alcanza a otro jugador con su burbuja: +5
    public void SumarEliminacion(int atacante, int victima)
    {
        if (atacante == victima)
            return;

        SumarPuntos(atacante, PuntosEliminar);
    }

    // Obstaculos rotos con una burbuja: +1 cada uno
    public void SumarObstaculos(int jugador, int cantidad)
    {
        if (cantidad > 0)
            SumarPuntos(jugador, PuntosObstaculo * cantidad);
    }

    // El fuego o la erupcion del Boss alcanza al jugador: -3 (minimo 0)
    public void RestarDanoBoss(int jugador)
    {
        SumarPuntos(jugador, -PuntosDanoBoss);
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
