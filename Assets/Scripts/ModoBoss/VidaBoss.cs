using UnityEngine;
using UnityEngine.Events;

// Vida y fases del Boss Volcán (tarjeta "Lab3 - MB - Fases del Boss"):
// - el Boss tiene 60 HP y cada impacto de burbuja le quita 3 (20 impactos)
// - Fase 1: de 60 a 30 HP  -> fuego en cruz 1 s, se repite 5 s despues
// - Fase 2: menos de 30 HP -> fuego en cruz 2 s, se repite 3 s despues
// - Fase 3: menos de 20 HP -> como la fase 2 + erupcion
// - cada impacto da puntos al jugador (PuntosBoss): +3, o +15 si es el
//   golpe final
//
// Como se engancha (para el fuego en cruz, la erupcion y el tablero):
// - la burbuja llama a RecibirImpacto(jugador) cuando su explosion
//   alcanza la casilla del Boss
// - el fuego escucha alCambiarFase y lee DuracionFuego(), PausaFuego()
//   y HayErupcion() para saber como atacar en la fase actual
// - alMorir avisa de que el Boss ha caido (fin de partida)
public class VidaBoss : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField, Min(1)] private int vidaMaxima = 60;
    [SerializeField, Min(1)] private int danoPorImpacto = 3;

    [Header("Cambio de fase (por debajo de estos HP)")]
    [SerializeField] private int vidaFase2 = 30;
    [SerializeField] private int vidaFase3 = 20;

    [Header("Fuego en cruz por fase (segundos). [0] = F1, [1] = F2, [2] = F3")]
    [SerializeField] private float[] duracionFuego = { 1f, 2f, 2f };
    [SerializeField] private float[] pausaFuego = { 5f, 3f, 3f };

    [Tooltip("Si se deja vacio, se busca solo en la escena")]
    [SerializeField] private PuntosBoss puntos;

    [Tooltip("Parametro: la fase nueva (1, 2 o 3)")]
    public UnityEvent<int> alCambiarFase = new UnityEvent<int>();

    [Tooltip("Parametros: vida actual y vida maxima (para una barra de vida)")]
    public UnityEvent<int, int> alCambiarVida = new UnityEvent<int, int>();

    [Tooltip("Parametro: jugador que dio el golpe final (0 = J1 ... 3 = J4)")]
    public UnityEvent<int> alMorir = new UnityEvent<int>();

    private int vida;
    private int fase;
    private bool iniciado;

    // ---------- Inicio ----------

    private void Awake()
    {
        Iniciar();
    }

    // Se llama sola; tambien sirve para reiniciar el Boss
    public void Iniciar()
    {
        vida = vidaMaxima;
        fase = CalcularFase(vida, vidaFase2, vidaFase3);
        iniciado = true;
    }

    // En los tests (EditMode) Awake no se ejecuta, por eso se comprueba aqui
    private void AsegurarIniciado()
    {
        if (!iniciado)
            Iniciar();
    }

    // ---------- Recibir golpes ----------

    // La burbuja del jugador (0 = J1 ... 3 = J4) alcanza al Boss.
    // Si no se sabe de quien es la burbuja, usar -1 (quita vida, no da puntos).
    public void RecibirImpacto(int jugador)
    {
        AsegurarIniciado();

        if (EstaMuerto())
            return;

        vida = Mathf.Max(0, vida - danoPorImpacto);

        if (puntos == null)
            puntos = GetComponent<PuntosBoss>();
        if (puntos == null)
            puntos = FindAnyObjectByType<PuntosBoss>();

        bool golpeFinal = vida == 0;

        if (puntos != null)
        {
            if (golpeFinal)
                puntos.SumarGolpeFinal(jugador);
            else
                puntos.SumarImpactoBoss(jugador);
        }

        Debug.Log("Boss: " + vida + "/" + vidaMaxima + " HP");

        if (alCambiarVida != null)
            alCambiarVida.Invoke(vida, vidaMaxima);

        int faseNueva = CalcularFase(vida, vidaFase2, vidaFase3);
        if (faseNueva != fase)
        {
            fase = faseNueva;
            Debug.Log("Boss: empieza la fase " + fase);

            if (alCambiarFase != null)
                alCambiarFase.Invoke(fase);
        }

        if (golpeFinal)
        {
            Debug.Log("Boss derrotado por el jugador " + (jugador + 1));

            if (alMorir != null)
                alMorir.Invoke(jugador);
        }
    }

    // ---------- Fases ----------

    // Fase segun la vida: 1 (30 HP o mas), 2 (menos de 30), 3 (menos de 20)
    public static int CalcularFase(int vida, int vidaFase2 = 30, int vidaFase3 = 20)
    {
        if (vida < vidaFase3)
            return 3;

        if (vida < vidaFase2)
            return 2;

        return 1;
    }

    // Cuanto dura el fuego en cruz en la fase actual (segundos)
    public float DuracionFuego()
    {
        return ValorFase(duracionFuego);
    }

    // Cuanto espera el Boss entre un fuego y el siguiente (segundos)
    public float PausaFuego()
    {
        return ValorFase(pausaFuego);
    }

    // En la fase 3 tambien hay erupcion (bolas de fuego sobre los jugadores)
    public bool HayErupcion()
    {
        return GetFase() >= 3;
    }

    private float ValorFase(float[] valores)
    {
        if (valores == null || valores.Length == 0)
            return 0f;

        int i = Mathf.Clamp(GetFase() - 1, 0, valores.Length - 1);
        return valores[i];
    }

    // ---------- Para otros scripts ----------

    public int GetVida()
    {
        AsegurarIniciado();
        return vida;
    }

    public int GetVidaMaxima()
    {
        return vidaMaxima;
    }

    public int GetFase()
    {
        AsegurarIniciado();
        return fase;
    }

    public bool EstaMuerto()
    {
        AsegurarIniciado();
        return vida <= 0;
    }
}
