using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PartidaManager : MonoBehaviour
{
    [Header("Jugadores (en orden: J1, J2, J3, J4)")]
    [SerializeField]
    private GameObject[] jugadores =
        new GameObject[GameSettings.MaxPlayers];

    [Header("Pantalla de fin de partida")]
    [SerializeField] private GameObject panelFin;
    [SerializeField] private TMP_Text textoGanador;

    [Header("Pausa")]
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private GameObject botonPausa;

    private bool partidaTerminada = false;
    private bool enPausa = false;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (!GameSettings.SettingsChosen)
            return;

        for (int i = 0; i < jugadores.Length; i++)
        {
            if (jugadores[i] == null)
                continue;

            PonerColor(i);

            if (EsIA(i))
                ConvertirEnIA(i);
        }
    }

    private void Start()
    {
        if (panelFin != null)
            panelFin.SetActive(false);

        if (panelPausa != null)
            panelPausa.SetActive(false);

        if (botonPausa != null)
            botonPausa.SetActive(true);
    }

    private void PonerColor(int i)
    {
        SpriteRenderer sr =
            jugadores[i].GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
            sr.color = GameSettings.ToUnityColor(GameSettings.PlayerColors[i]);
    }

    private bool EsIA(int i)
    {
        return GameSettings.SettingsChosen && i >= GameSettings.HumanPlayers;
    }

    private void ConvertirEnIA(int i)
    {
        PlayerInput input = jugadores[i].GetComponent<PlayerInput>();
        if (input != null)
            input.enabled = false;

        Debug.Log("Jugador " + (i + 1) + " es IA");
    }

    private void Update()
    {
        if (partidaTerminada)
            return;

        if (PausaPulsada())
        {
            if (enPausa)
                Continuar();
            else
                Pausar();
        }
    }

    private bool PausaPulsada()
    {
        bool teclado = Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame;

        bool mando = Gamepad.current != null &&
            Gamepad.current.startButton.wasPressedThisFrame;

        return teclado || mando;
    }

    public void Pausar()
    {
        if (partidaTerminada || enPausa)
            return;

        enPausa = true;

        Time.timeScale = 0f;

        if (panelPausa != null)
            panelPausa.SetActive(true);

        if (botonPausa != null)
            botonPausa.SetActive(false);
    }

    public void Continuar()
    {
        enPausa = false;
        Time.timeScale = 1f;

        if (panelPausa != null)
            panelPausa.SetActive(false);

        if (botonPausa != null)
            botonPausa.SetActive(true);
    }

    private void LateUpdate()
    {
        if (partidaTerminada)
            return;

        int participantes = 0;
        int vivos = 0;
        int ultimoVivo = -1;
        int realesVivos = 0;
        int iasVivas = 0;

        for (int i = 0; i < jugadores.Length; i++)
        {
            if (jugadores[i] == null)
                continue;

            participantes++;

            if (jugadores[i].activeSelf)
            {
                vivos++;
                ultimoVivo = i;

                if (EsIA(i))
                    iasVivas++;
                else
                    realesVivos++;
            }
        }

        if (participantes < 2)
            return;

        if (vivos == 1)
        {
            TerminarPartida("¡Gana " + NombreJugador(ultimoVivo) + "!");
        }
        else if (vivos == 0)
        {
            TerminarPartida("¡Empate!");
        }
        else if (GameSettings.SettingsChosen && realesVivos == 0 && iasVivas >= 2)
        {
            // Han muerto todos los jugadores reales: no esperamos a las IAs
            TerminarSinJugadoresReales();
        }
    }

    // Gana la IA con mas vidas; si varias tienen las mismas, empate entre ellas
    private void TerminarSinJugadoresReales()
    {
        int maxVidas = -1;

        // 1. Buscar el maximo de vidas entre los que siguen vivos
        for (int i = 0; i < jugadores.Length; i++)
        {
            if (jugadores[i] == null || !jugadores[i].activeSelf)
                continue;

            int vidas = VidasDe(i);
            if (vidas > maxVidas)
                maxVidas = vidas;
        }

        // 2. Juntar todos los que tienen ese maximo
        string nombres = "";
        int cuantos = 0;

        for (int i = 0; i < jugadores.Length; i++)
        {
            if (jugadores[i] == null || !jugadores[i].activeSelf)
                continue;

            if (VidasDe(i) != maxVidas)
                continue;

            if (cuantos > 0)
                nombres += " y ";
            nombres += NombreJugador(i);
            cuantos++;
        }

        string motivo = "No quedan jugadores reales";
        string vidasTexto = maxVidas == 1 ? " vida" : " vidas";

        if (cuantos == 1)
            TerminarPartida(motivo + "\n¡Gana " + nombres +
                            " con " + maxVidas + vidasTexto + "!");
        else
            TerminarPartida(motivo + "\n¡Empate entre " + nombres +
                            " con " + maxVidas + vidasTexto + "!");
    }

    private int VidasDe(int i)
    {
        PlayerDamage dano = jugadores[i].GetComponent<PlayerDamage>();
        return dano != null ? dano.GetCurrentLives() : 0;
    }

    // Fin por tiempo (modo Diamante: 3 min / modo Boss: 5 min)
    public void TerminarPorTiempo()
    {
        if (partidaTerminada)
            return;

        TerminarPorPuntos("¡Se acabó el tiempo!");
    }

    // Modo Boss: se llama cuando el Boss muere (evento alMorir de VidaBoss)
    public void TerminarPorBossDerrotado(int jugadorGolpeFinal)
    {
        if (partidaTerminada)
            return;

        TerminarPorPuntos("¡Boss derrotado por " +
                          NombreJugador(jugadorGolpeFinal) + "!");
    }

    // Gana quien tenga mas puntos (puntos del Boss o del Diamante)
    private void TerminarPorPuntos(string motivo)
    {
        int ganador;
        int puntosGanador;

        PuntosBoss puntosBoss = FindAnyObjectByType<PuntosBoss>();
        PuntosAD puntosAD = FindAnyObjectByType<PuntosAD>();

        if (puntosBoss != null)
        {
            ganador = puntosBoss.Ganador();
            puntosGanador = ganador >= 0 ? puntosBoss.GetPuntos(ganador) : 0;
        }
        else if (puntosAD != null)
        {
            ganador = puntosAD.Ganador();
            puntosGanador = ganador >= 0 ? puntosAD.GetPuntos(ganador) : 0;
        }
        else
        {
            TerminarPartida(motivo);
            return;
        }

        if (ganador < 0)
            TerminarPartida(motivo + "\n¡Empate!");
        else
            TerminarPartida(motivo + "\n¡Gana " + NombreJugador(ganador) +
                            " con " + puntosGanador + " puntos!");
    }

    private void TerminarPartida(string mensaje)
    {
        partidaTerminada = true;

        Time.timeScale = 0f;

        Debug.Log("Fin de la partida: " + mensaje);

        if (panelFin != null)
            panelFin.SetActive(true);

        if (textoGanador != null)
            textoGanador.text = mensaje;

        if (botonPausa != null)
            botonPausa.SetActive(false);
    }

    private string NombreJugador(int i)
    {
        string nombre = "Jugador " + (i + 1);

        if (GameSettings.SettingsChosen)
            nombre += " (" + GameSettings.PlayerColors[i] + ")";

        return nombre;
    }

    public void VolverAJugar()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MenuPrincipal()
    {
        Time.timeScale = 1f;
        GameSettings.SettingsChosen = false;
        SceneManager.LoadScene("Menu inicio");
    }

    public int IndiceDe(GameObject jugador)
    {
        for (int i = 0; i < jugadores.Length; i++)
        {
            if (jugadores[i] == jugador)
                return i;
        }

        return -1;
    }

    public string NombreDe(int i)
    {
        return NombreJugador(i);
    }
}
