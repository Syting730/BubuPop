using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Encargado de la partida en el tablero:
// - aplica lo elegido en el menu (colores, jugadores reales / IA)
// - pausa (boton en pantalla, Esc o Start del mando)
// - fin de partida del modo Clasico: gana el ultimo superviviente
public class PartidaManager : MonoBehaviour
{
    [Header("Jugadores (en orden: J1, J2, J3, J4)")]
    // De momento solo existen J1 y J2; J3 y J4 se dejan vacios
    [SerializeField]
    private GameObject[] jugadores =
        new GameObject[GameSettings.MaxPlayers];

    [Header("Pantalla de fin de partida")]
    [SerializeField] private GameObject panelFin;
    [SerializeField] private TMP_Text textoGanador;

    [Header("Pausa")]
    [SerializeField] private GameObject panelPausa;
    // Boton que se ve durante la partida para pausar
    [SerializeField] private GameObject botonPausa;

    private bool partidaTerminada = false;
    private bool enPausa = false;

    // ---------- Inicio ----------

    // Awake va antes que el Start de los demas scripts
    private void Awake()
    {
        Time.timeScale = 1f;

        // Si se abre el Tablero sin pasar por el menu, no tocamos nada
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
        // Al empezar: pantallas de fin y pausa ocultas, boton de pausa visible
        if (panelFin != null)
            panelFin.SetActive(false);

        if (panelPausa != null)
            panelPausa.SetActive(false);

        if (botonPausa != null)
            botonPausa.SetActive(true);
    }

    // Pinta el cuadrado del jugador con su color elegido
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

    // De momento la IA se queda quieta:
    // quitamos el movimiento y las bombas por teclado.
    // Cuando exista el script de IA, se activara aqui.
    private void ConvertirEnIA(int i)
    {
        PlayerMovement movimiento = jugadores[i].GetComponent<PlayerMovement>();
        if (movimiento != null)
            movimiento.enabled = false;

        BombPlacer bombas = jugadores[i].GetComponent<BombPlacer>();
        if (bombas != null)
            bombas.enabled = false;

        PlayerInput input = jugadores[i].GetComponent<PlayerInput>();
        if (input != null)
            input.enabled = false;

        Debug.Log("Jugador " + (i + 1) + " es IA (de momento quieta)");
    }

    // ---------- Pausa ----------

    private void Update()
    {
        // Cuando la partida ha terminado no se puede pausar
        if (partidaTerminada)
            return;

        // Esc / Start: si esta en pausa continua, si no, pausa
        if (PausaPulsada())
        {
            if (enPausa)
                Continuar();
            else
                Pausar();
        }
    }

    // Esc en el teclado o Start/Options en el mando
    private bool PausaPulsada()
    {
        bool teclado = Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame;

        bool mando = Gamepad.current != null &&
            Gamepad.current.startButton.wasPressedThisFrame;

        return teclado || mando;
    }

    // Boton de pausa de la pantalla (y Esc / Start)
    public void Pausar()
    {
        if (partidaTerminada || enPausa)
            return;

        enPausa = true;

        // Congelar el juego (movimiento, bombas, reaparicion de bloques)
        Time.timeScale = 0f;

        if (panelPausa != null)
            panelPausa.SetActive(true);

        if (botonPausa != null)
            botonPausa.SetActive(false);
    }

    // Opcion 1 de la pausa: "Continuar" (y Esc / Start otra vez)
    public void Continuar()
    {
        enPausa = false;
        Time.timeScale = 1f;

        if (panelPausa != null)
            panelPausa.SetActive(false);

        if (botonPausa != null)
            botonPausa.SetActive(true);
    }

    // ---------- Fin de partida (modo Clasico) ----------

    // LateUpdate: si una bomba alcanza a dos jugadores
    // en el mismo frame, se cuenta como empate
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

            // PlayerDamage desactiva al jugador cuando pierde las 3 vidas
            if (jugadores[i].activeSelf)
            {
                vivos++;
                ultimoVivo = i;
            }
        }

        // Hacen falta al menos 2 jugadores para que alguien gane
        if (participantes < 2)
            return;

        if (vivos == 1)
            TerminarPartida("¡Gana " + NombreJugador(ultimoVivo) + "!");
        else if (vivos == 0)
            TerminarPartida("¡Empate!");
    }

    private void TerminarPartida(string mensaje)
    {
        partidaTerminada = true;

        // Congelar el juego (movimiento y bombas)
        Time.timeScale = 0f;

        Debug.Log("Fin de la partida: " + mensaje);

        if (panelFin != null)
            panelFin.SetActive(true);

        if (textoGanador != null)
            textoGanador.text = mensaje;

        // Ya no se puede pausar
        if (botonPausa != null)
            botonPausa.SetActive(false);
    }

    // Todos se llaman igual, sean jugadores reales o IA:
    // "Jugador 3 (Azul)"
    private string NombreJugador(int i)
    {
        string nombre = "Jugador " + (i + 1);

        if (GameSettings.SettingsChosen)
            nombre += " (" + GameSettings.PlayerColors[i] + ")";

        return nombre;
    }

    // ---------- Botones (pantalla de fin y de pausa) ----------

    // Opcion 2 de la pausa: "Reiniciar"
    // y "Volver a jugar" de la pantalla de fin
    public void VolverAJugar()
    {
        Time.timeScale = 1f;

        // Se recarga el tablero con la misma configuracion
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Opcion 3 de la pausa: "Menu principal"
    // y "Menu principal" de la pantalla de fin
    public void MenuPrincipal()
    {
        Time.timeScale = 1f;
        GameSettings.SettingsChosen = false;
        SceneManager.LoadScene("Menu inicio");
    }

    // Numero (0-3) de un jugador, o -1 si no es un jugador
    public int IndiceDe(GameObject jugador)
    {
        for (int i = 0; i < jugadores.Length; i++)
        {
            if (jugadores[i] == jugador)
                return i;
        }

        return -1;
    }

    // "Jugador 1 (Rojo)"
    public string NombreDe(int i)
    {
        return NombreJugador(i);
    }
}