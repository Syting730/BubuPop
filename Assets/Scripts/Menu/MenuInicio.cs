using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    [SerializeField] private Button[] botonesColor;

    private bool modoElegido = false;
    private bool jugadoresElegidos = false;
    private bool personajeElegido = false;

    private int jugadorEligiendo = 0;
    private bool[] colorUsado = new bool[GameSettings.MaxPlayers];
    private Image[] dibujosColor;

    private void Start()
    {
        PonerDibujosEnBotones();
        MostrarPanel(panelPrincipal);
        ActualizarPrincipal();
    }

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

    public void VolverAlPrincipal()
    {
        MostrarPanel(panelPrincipal);
        ActualizarPrincipal();
    }

    public void PulsarStart()
    {
        string colores = "";
        for (int i = 0; i < GameSettings.MaxPlayers; i++)
            colores += GameSettings.PlayerColors[i] + " ";

        Debug.Log("START: " + GameSettings.SelectedMode + ", " +
                  GameSettings.HumanPlayers + " jugadores + " +
                  GameSettings.AIPlayers + " IAs. Colores: " + colores);

        GameSettings.SettingsChosen = true;

        if (GameSettings.SelectedMode == GameSettings.GameMode.AsaltoDiamante)
            SceneManager.LoadScene("TableroAD");
        else
            SceneManager.LoadScene("Tablero");
    }

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

    public void ElegirJugadores(int numero)
    {
        if (numero != GameSettings.HumanPlayers)
            personajeElegido = false;

        GameSettings.HumanPlayers = numero;
        jugadoresElegidos = true;
        VolverAlPrincipal();
    }

    private void EmpezarEleccionColores()
    {
        personajeElegido = false;
        jugadorEligiendo = 0;

        for (int c = 0; c < colorUsado.Length; c++)
        {
            colorUsado[c] = false;
            botonesColor[c].interactable = true;
            AtenuarDibujo(c, false);
        }

        ActualizarTituloColor();
    }

    public void ElegirColor(int color)
    {
        if (colorUsado[color])
            return;

        GameSettings.PlayerColors[jugadorEligiendo] =
            (GameSettings.PlayerColor)color;
        colorUsado[color] = true;

        botonesColor[color].interactable = false;
        AtenuarDibujo(color, true);

        jugadorEligiendo++;

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

    private void ActualizarPrincipal()
    {
        if (modoElegido)
            textoModo.text = "Modo de juego: " +
                             NombreModo(GameSettings.SelectedMode);
        else
            textoModo.text = "Modo de juego: —";

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

        botonPersonaje.interactable = jugadoresElegidos;

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

    private void PonerDibujosEnBotones()
    {
        dibujosColor = new Image[botonesColor.Length];

        for (int c = 0; c < botonesColor.Length; c++)
        {
            Sprite dibujo = AspectoPersonaje.Cargar((GameSettings.PlayerColor)c, "frente", 1);

            if (dibujo == null)
                continue;

            TMP_Text etiqueta = botonesColor[c].GetComponentInChildren<TMP_Text>();

            if (etiqueta != null)
            {
                RectTransform rtTexto = etiqueta.rectTransform;
                rtTexto.anchorMin = new Vector2(0f, 0f);
                rtTexto.anchorMax = new Vector2(1f, 0.25f);
                rtTexto.offsetMin = Vector2.zero;
                rtTexto.offsetMax = Vector2.zero;
                etiqueta.fontSizeMax = etiqueta.fontSize;
                etiqueta.enableAutoSizing = true;
            }

            GameObject obj = new GameObject("Personaje", typeof(RectTransform), typeof(Image));
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.SetParent(botonesColor[c].transform, false);
            rt.SetAsFirstSibling();
            rt.anchorMin = new Vector2(0.1f, 0.22f);
            rt.anchorMax = new Vector2(0.9f, 0.98f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image imagen = obj.GetComponent<Image>();
            imagen.sprite = dibujo;
            imagen.preserveAspect = true;
            imagen.raycastTarget = false;

            dibujosColor[c] = imagen;
        }
    }

    private void AtenuarDibujo(int c, bool atenuar)
    {
        if (dibujosColor == null || c >= dibujosColor.Length || dibujosColor[c] == null)
            return;

        dibujosColor[c].color = atenuar ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
    }

    private void MostrarPanel(GameObject panel)
    {
        panelPrincipal.SetActive(panel == panelPrincipal);
        panelModo.SetActive(panel == panelModo);
        panelJugadores.SetActive(panel == panelJugadores);
        panelPersonaje.SetActive(panel == panelPersonaje);
    }
}