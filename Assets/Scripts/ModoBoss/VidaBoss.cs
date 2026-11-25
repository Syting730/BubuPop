using UnityEngine;
using UnityEngine.Events;

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

    private void Awake()
    {
        Iniciar();
    }

    public void Iniciar()
    {
        vida = vidaMaxima;
        fase = CalcularFase(vida, vidaFase2, vidaFase3);
        iniciado = true;
    }

    private void AsegurarIniciado()
    {
        if (!iniciado)
            Iniciar();
    }

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

    public static int CalcularFase(int vida, int vidaFase2 = 30, int vidaFase3 = 20)
    {
        if (vida < vidaFase3)
            return 3;

        if (vida < vidaFase2)
            return 2;

        return 1;
    }

    public float DuracionFuego()
    {
        return ValorFase(duracionFuego);
    }

    public float PausaFuego()
    {
        return ValorFase(pausaFuego);
    }

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
