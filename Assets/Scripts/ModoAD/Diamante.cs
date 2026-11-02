using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

// Diamante del modo Asalto al Diamante:
// - se coloca en la casilla central del tablero al empezar
// - un jugador lo coge al pasar por encima y el diamante le sigue
// - si el portador llega a SU base, el diamante vuelve al centro
// - el portador va mas lento y no puede poner bombas
// - si al portador le quitan una vida (o muere), el diamante cae
//   al suelo en esa casilla y cualquiera lo puede coger
// - el portador puede soltarlo cuando quiera:
//   J1 = Q, J2 = Ctrl derecho, mando = R / RB / R1
// (Puntos: tarjeta de otro companero)
public class Diamante : MonoBehaviour
{
    [Header("Tilemap del suelo (para calcular el centro)")]
    [SerializeField] private Tilemap suelo;

    [Header("Jugadores (en orden: J1, J2, J3, J4)")]
    [SerializeField] private GameObject[] jugadores = new GameObject[4];

    [Header("Posicion del diamante encima del portador")]
    [SerializeField] private Vector3 desplazamiento = new Vector3(0f, 0.5f, 0f);

    [Header("Efecto sobre el portador")]
    [Tooltip("Multiplica la velocidad del portador (0.5 = va a la mitad)")]
    [SerializeField, Range(0.1f, 1f)] private float factorVelocidad = 0.5f;

    [Header("Soltar el diamante (en orden: J1, J2, J3, J4)")]
    [Tooltip("Tecla para soltar. En un teclado caben 2 jugadores: " +
             "J1 (WASD + Espacio) usa Q y J2 (flechas + Enter) usa Ctrl derecho")]
    [SerializeField] private Key[] teclaSoltar =
        { Key.Q, Key.RightCtrl, Key.None, Key.None };

    [Tooltip("Numero de mando de cada jugador (0 = primer mando " +
             "conectado). -1 = ese jugador no usa mando")]
    [SerializeField] private int[] mandoDeJugador = { 0, 1, 2, 3 };

    private Vector3 posicionCentro;
    private GameObject portador;
    private int indicePortador = -1;
    private Base[] bases;

    // Para quitar los efectos al soltar el diamante
    private PlayerMovement movimientoPortador;
    private BombPlacer bombasPortador;
    private PlayerDamage vidaPortador;
    private float velocidadOriginal;
    private int vidasAlCoger;

    // Quien lo acaba de soltar a mano no lo vuelve a coger
    // hasta que se aparte de la casilla
    private GameObject ignorarHastaQueSalga;

    // ---------- Para otros scripts ----------

    // Posicion de la casilla central
    public Vector3 PosicionCentro
    {
        get { return posicionCentro; }
    }

    // Jugador que lleva el diamante (null si nadie)
    public GameObject Portador
    {
        get { return portador; }
    }

    // Numero del portador (0 = J1 ... 3 = J4), -1 si nadie
    public int IndicePortador
    {
        get { return indicePortador; }
    }

    // ---------- Colocar en el centro ----------

    private void Start()
    {
        bases = FindObjectsByType<Base>(FindObjectsSortMode.None);
        ColocarEnCentro();
    }

    public void ColocarEnCentro()
    {
        BoundsInt limites = suelo.cellBounds;

        Vector3Int centro = new Vector3Int(
            limites.xMin + limites.size.x / 2,
            limites.yMin + limites.size.y / 2,
            0);

        posicionCentro = suelo.GetCellCenterWorld(centro);
        transform.position = posicionCentro;

        Debug.Log("Diamante colocado en la casilla central " + centro);
    }

    // ---------- Coger ----------

    // Un jugador pasa por encima del diamante.
    // Se usa Stay (y no Enter) para que, si el diamante cae
    // encima de alguien, ese jugador tambien lo pueda coger.
    private void OnTriggerStay2D(Collider2D otro)
    {
        if (portador != null)
            return;

        PlayerMovement jugador = otro.GetComponentInParent<PlayerMovement>();
        if (jugador == null)
            return;

        if (jugador.gameObject == ignorarHastaQueSalga)
            return;

        Coger(jugador);
    }

    // El que lo solto se aparta: ya lo puede volver a coger
    private void OnTriggerExit2D(Collider2D otro)
    {
        PlayerMovement jugador = otro.GetComponentInParent<PlayerMovement>();
        if (jugador != null && jugador.gameObject == ignorarHastaQueSalga)
            ignorarHastaQueSalga = null;
    }

    private void Coger(PlayerMovement jugador)
    {
        ignorarHastaQueSalga = null;
        portador = jugador.gameObject;
        indicePortador = System.Array.IndexOf(jugadores, portador);

        // Ralentizar
        movimientoPortador = jugador;
        velocidadOriginal = jugador.speed;
        jugador.speed = velocidadOriginal * factorVelocidad;

        // Bloquear bombas (desactivando su BombPlacer)
        bombasPortador = portador.GetComponentInChildren<BombPlacer>();
        if (bombasPortador != null)
            bombasPortador.enabled = false;

        // Guardar vidas para saber si le dan mientras lo lleva
        vidaPortador = portador.GetComponentInChildren<PlayerDamage>();
        if (vidaPortador != null)
            vidasAlCoger = vidaPortador.GetCurrentLives();

        Debug.Log("Jugador " + (indicePortador + 1) + " ha cogido el diamante");
    }

    // ---------- Llevar y llegar a la base ----------

    private void Update()
    {
        if (portador == null)
            return;

        // ¿Le han quitado una vida o ha muerto?
        // (se comprueba antes de mover el diamante: asi aun esta
        //  en la casilla donde le dieron, no en su punto de inicio)
        if (PortadorHaSidoAlcanzado())
        {
            Debug.Log("Jugador " + (indicePortador + 1) +
                      " ha sido alcanzado y suelta el diamante");
            SoltarEnSuelo();
            return;
        }

        // ¿Quiere soltarlo?
        if (SoltarPulsado())
        {
            Debug.Log("Jugador " + (indicePortador + 1) + " suelta el diamante");
            ignorarHastaQueSalga = portador;
            transform.position = portador.transform.position + desplazamiento;
            SoltarEnSuelo();
            return;
        }

        // El diamante sigue al portador
        transform.position = portador.transform.position + desplazamiento;

        // ¿Ha llegado el portador a SU base?
        foreach (Base b in bases)
        {
            if (b.Jugador != indicePortador)
                continue;

            Collider2D zona = b.GetComponent<Collider2D>();
            if (zona != null && zona.OverlapPoint(portador.transform.position))
            {
                Debug.Log("¡Jugador " + (indicePortador + 1) +
                          " ha llevado el diamante a su base!");

                // Aqui otro companero puede sumar los puntos

                Soltar();
                return;
            }
        }
    }

    private bool PortadorHaSidoAlcanzado()
    {
        if (!portador.activeInHierarchy)
            return true;

        if (vidaPortador == null)
            return false;

        return vidaPortador.IsDead() ||
               vidaPortador.GetCurrentLives() < vidasAlCoger;
    }

    // ---------- Soltar ----------

    // Tecla del portador o boton R / RB / R1 de su mando
    private bool SoltarPulsado()
    {
        if (Time.timeScale == 0f)  // en pausa o partida terminada
            return false;

        if (indicePortador < 0)
            return false;

        // Teclado
        if (Keyboard.current != null &&
            indicePortador < teclaSoltar.Length &&
            teclaSoltar[indicePortador] != Key.None &&
            Keyboard.current[teclaSoltar[indicePortador]].wasPressedThisFrame)
        {
            return true;
        }

        // Mando
        Gamepad mando = MandoDelPortador();
        return mando != null && mando.rightShoulder.wasPressedThisFrame;
    }

    private Gamepad MandoDelPortador()
    {
        // Si el jugador usa PlayerInput con un mando, ese es el suyo
        PlayerInput input = portador.GetComponent<PlayerInput>();
        if (input != null && input.enabled)
        {
            foreach (InputDevice dispositivo in input.devices)
            {
                if (dispositivo is Gamepad mandoPropio)
                    return mandoPropio;
            }
        }

        // Si no, el numero de mando puesto en el Inspector
        if (indicePortador >= mandoDeJugador.Length)
            return null;

        int numero = mandoDeJugador[indicePortador];
        if (numero < 0 || numero >= Gamepad.all.Count)
            return null;

        return Gamepad.all[numero];
    }

    // Suelta el diamante y lo devuelve al centro
    public void Soltar()
    {
        QuitarEfectos();
        transform.position = posicionCentro;
    }

    // Suelta el diamante en la casilla donde esta ahora
    public void SoltarEnSuelo()
    {
        QuitarEfectos();

        Vector3Int casilla = suelo.WorldToCell(transform.position - desplazamiento);
        transform.position = suelo.GetCellCenterWorld(casilla);
    }

    // Devuelve la velocidad y las bombas al portador
    private void QuitarEfectos()
    {
        if (movimientoPortador != null)
            movimientoPortador.speed = velocidadOriginal;

        if (bombasPortador != null)
            bombasPortador.enabled = true;

        portador = null;
        indicePortador = -1;
        movimientoPortador = null;
        bombasPortador = null;
        vidaPortador = null;
    }
}