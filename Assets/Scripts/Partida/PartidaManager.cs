using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Encargado de la partida en el tablero:
// - aplica lo elegido en el menu (colores, jugadores reales / IA)
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

    private bool partidaTerminada = false;

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
        // La pantalla de fin empieza oculta
        if (panelFin != null)
            panelFin.SetActive(false);
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

    // ---------- Botones de la pantalla de fin ----------

    public void VolverAJugar()
    {
        Time.timeScale = 1f;

        // Se recarga el tablero con la misma configuracion
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MenuPrincipal()
    {
        Time.timeScale = 1f;
        GameSettings.SettingsChosen = false;
        SceneManager.LoadScene("Menu inicio");
    }
}