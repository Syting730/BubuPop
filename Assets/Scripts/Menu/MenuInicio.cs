using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menu de inicio: pantalla principal con 3 elecciones + START
public class MenuInicio : MonoBehaviour
{
    [Header("Pantallas")]
    [SerializeField] private GameObject panelPrincipal;
    [SerializeField] private GameObject panelModo;
    [SerializeField] private GameObject panelJugadores;
    [SerializeField] private GameObject panelPersonaje;

    [Header("Textos de los botones de la pantalla principal")]
    [SerializeField] private TMP_Text textoModo;
    [SerializeField] private TMP_Text textoJugadores;
    [SerializeField] private TMP_Text textoPersonaje;

    [Header("Botones de la pantalla principal")]
    [SerializeField] private Button botonPersonaje;
    [SerializeField] private Button botonStart;

    [Header("Pantalla de color")]
    [SerializeField] private TMP_Text tituloColor;
    // En orden: Rojo, Amarillo, Azul, Verde
    [SerializeField] private Button[] botonesColor;

    // Que se ha elegido ya
    private bool modoElegido = false;
    private bool jugadoresElegidos = false;
    private bool personajeElegido = false;

    // Eleccion de colores por turnos
    private int jugadorEligiendo = 0;
    private bool[] colorUsado = new bool[GameSettings.MaxPlayers];

    private void Start()
    {
        MostrarPanel(panelPrincipal);
        ActualizarPrincipal();
    }

    // ---------- Pantalla principal ----------

    public void AbrirModo()
    {
        MostrarPanel(panelModo);
    }

    public void AbrirJugadores()
    {
        MostrarPanel(panelJugadores);
    }

    public void AbrirPersonaje()
    {
        EmpezarEleccionColores();
        MostrarPanel(panelPersonaje);
    }

    // Boton "Volver" de cualquier pantalla
    public void VolverAlPrincipal()
    {
        MostrarPanel(panelPrincipal);
        ActualizarPrincipal();
    }

    // Boton START (solo se puede pulsar cuando esta activado)
    public void PulsarStart()
    {
        string colores = "";
        for (int i = 0; i < GameSettings.MaxPlayers; i++)
            colores += GameSettings.PlayerColors[i] + " ";

        Debug.Log("START: " + GameSettings.SelectedMode + ", " +
                  GameSettings.HumanPlayers + " jugadores + " +
                  GameSettings.AIPlayers + " IAs. Colores: " + colores);

        // Aqui cargaremos la escena del juego (siguiente paso)
    }

    // ---------- Pantalla de Modo ----------

    public void ElegirClasico()
    {
        ElegirModo(GameSettings.GameMode.Clasico);
    }

    public void ElegirDiamante()
    {
        ElegirModo(GameSettings.GameMode.AsaltoDiamante);
    }

    public void ElegirBoss()
    {
        ElegirModo(GameSettings.GameMode.BossVolcan);
    }

    private void ElegirModo(GameSettings.GameMode modo)
    {
        GameSettings.SelectedMode = modo;
        modoElegido = true;
        VolverAlPrincipal();
    }

    // ---------- Pantalla de Nº de jugadores ----------
    // Los 4 botones llaman a esta funcion con su numero (1, 2, 3 o 4)

    public void ElegirJugadores(int numero)
    {
        // Si cambia el numero, hay que volver a elegir los colores
        if (numero != GameSettings.HumanPlayers)
            personajeElegido = false;

        GameSettings.HumanPlayers = numero;
        jugadoresElegidos = true;
        VolverAlPrincipal();
    }

    // ---------- Pantalla de Color ----------

    private void EmpezarEleccionColores()
    {
        personajeElegido = false;
        jugadorEligiendo = 0;

        for (int c = 0; c < colorUsado.Length; c++)
        {
            colorUsado[c] = false;
            botonesColor[c].interactable = true;
        }

        ActualizarTituloColor();
    }

    // Los 4 botones de color llaman a esta funcion:
    // 0 = Rojo, 1 = Amarillo, 2 = Azul, 3 = Verde
    public void ElegirColor(int color)
    {
        if (colorUsado[color])
            return;

        GameSettings.PlayerColors[jugadorEligiendo] =
            (GameSettings.PlayerColor)color;
        colorUsado[color] = true;

        // El color elegido se queda gris y no se puede volver a elegir
        botonesColor[color].interactable = false;

        jugadorEligiendo++;

        // Si ya han elegido todos los jugadores reales
        if (jugadorEligiendo >= GameSettings.HumanPlayers)
        {
            AsignarColoresIA();
            personajeElegido = true;
            VolverAlPrincipal();
        }
        else
        {
            ActualizarTituloColor();
        }
    }

    // Las IAs se quedan con los colores que sobran
    private void AsignarColoresIA()
    {
        for (int i = GameSettings.HumanPlayers; i < GameSettings.MaxPlayers; i++)
        {
            for (int c = 0; c < colorUsado.Length; c++)
            {
                if (!colorUsado[c])
                {
                    GameSettings.PlayerColors[i] = (GameSettings.PlayerColor)c;
                    colorUsado[c] = true;
                    break;
                }
            }
        }
    }

    private void ActualizarTituloColor()
    {
        tituloColor.text = "Jugador " + (jugadorEligiendo + 1) +
                           ": elige tu color";
    }

    // ---------- Actualizar la pantalla principal ----------

    private void ActualizarPrincipal()
    {
        // Modo
        if (modoElegido)
            textoModo.text = "Modo de juego: " +
                             NombreModo(GameSettings.SelectedMode);
        else
            textoModo.text = "Modo de juego: —";

        // Nº de jugadores
        if (jugadoresElegidos)
        {
            textoJugadores.text = "Nº de jugadores: " +
                                  GameSettings.HumanPlayers;

            if (GameSettings.AIPlayers > 0)
                textoJugadores.text += " (+" + GameSettings.AIPlayers + " IA)";
        }
        else
        {
            textoJugadores.text = "Nº de jugadores: —";
        }

        // Colores (solo los de los jugadores reales)
        if (personajeElegido)
        {
            string texto = "Colores:";
            for (int i = 0; i < GameSettings.HumanPlayers; i++)
            {
                if (i > 0)
                    texto += " ·";
                texto += " J" + (i + 1) + " " + GameSettings.PlayerColors[i];
            }
            textoPersonaje.text = texto;
        }
        else
        {
            textoPersonaje.text = "Color del personaje: —";
        }

        // El color solo se puede elegir despues del Nº de jugadores
        botonPersonaje.interactable = jugadoresElegidos;

        // START solo se activa con las 3 cosas elegidas (gris = desactivado)
        botonStart.interactable =
            modoElegido && jugadoresElegidos && personajeElegido;
    }

    private string NombreModo(GameSettings.GameMode modo)
    {
        switch (modo)
        {
            case GameSettings.GameMode.AsaltoDiamante:
                return "Asalto al Diamante";
            case GameSettings.GameMode.BossVolcan:
                return "Boss Volcán";
            default:
                return "Clásico";
        }
    }

    // ---------- Cambiar de pantalla ----------

    private void MostrarPanel(GameObject panel)
    {
        panelPrincipal.SetActive(panel == panelPrincipal);
        panelModo.SetActive(panel == panelModo);
        panelJugadores.SetActive(panel == panelJugadores);
        panelPersonaje.SetActive(panel == panelPersonaje);
    }
}