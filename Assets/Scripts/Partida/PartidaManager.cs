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
        GameSettings.PlayerColor color = GameSettings.SettingsChosen
            ? GameSettings.PlayerColors[i]
            : (GameSettings.PlayerColor)(i % GameSettings.MaxPlayers);

        AspectoPersonaje aspecto = jugadores[i].GetComponent<AspectoPersonaje>();

        if (aspecto == null)
            aspecto = jugadores[i].AddComponent<AspectoPersonaje>();

        aspecto.Poner(color);
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

        for (int i = 0; i < jugadores.Length; i++)
        {
            if (jugadores[i] == null)
                continue;

            participantes++;

            if (jugadores[i].activeSelf)
            {
                vivos++;
                ultimoVivo = i;
            }
        }

        if (participantes < 2)
            return;

        if (vivos == 1)
            TerminarPartida("¡Gana " + NombreJugador(ultimoVivo) + "!");
        else if (vivos == 0)
            TerminarPartida("¡Empate!");
    }

    public void TerminarPorTiempo()
    {
        if (partidaTerminada)
            return;

        PuntosAD puntos = FindAnyObjectByType<PuntosAD>();

        if (puntos == null)
        {
            TerminarPartida("¡Se acabó el tiempo!");
            return;
        }

        int ganador = puntos.Ganador();

        if (ganador < 0)
            TerminarPartida("¡Se acabó el tiempo!\n¡Empate!");
        else
            TerminarPartida("¡Se acabó el tiempo!\n¡Gana " +
                            NombreJugador(ganador) + " con " +
                            puntos.GetPuntos(ganador) + " puntos!");
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
