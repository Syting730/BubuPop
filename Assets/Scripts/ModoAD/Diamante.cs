using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class Diamante : MonoBehaviour
{
    [Header("Tilemap del suelo (para calcular el centro)")]
    [SerializeField] private Tilemap suelo;

    [Header("Jugadores (en orden: J1, J2, J3, J4)")]
    [SerializeField] private GameObject[] jugadores = new GameObject[4];

    [Header("Posicion del diamante encima del portador")]
    [SerializeField] private Vector3 desplazamiento = new Vector3(0f, 0.5f, 0f);

    [Header("Reaparicion tras entregarlo en la base")]
    [Tooltip("Segundos que tarda en volver al centro (0 = al instante)")]
    [SerializeField, Min(0f)] private float tiempoReaparicion = 10f;

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
    private PuntosAD puntos;

    private PlayerMovement movimientoPortador;
    private BombPlacer bombasPortador;
    private PlayerDamage vidaPortador;
    private float velocidadOriginal;
    private int vidasAlCoger;

    private GameObject ignorarHastaQueSalga;

    public Vector3 PosicionCentro
    {
        get { return posicionCentro; }
    }

    public GameObject Portador
    {
        get { return portador; }
    }

    public int IndicePortador
    {
        get { return indicePortador; }
    }

    private void Start()
    {
        bases = FindObjectsByType<Base>();
        puntos = FindAnyObjectByType<PuntosAD>();
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

        movimientoPortador = jugador;
        velocidadOriginal = jugador.speed;
        jugador.speed = velocidadOriginal * factorVelocidad;

        bombasPortador = portador.GetComponentInChildren<BombPlacer>();
        if (bombasPortador != null)
            bombasPortador.enabled = false;

        vidaPortador = portador.GetComponentInChildren<PlayerDamage>();
        if (vidaPortador != null)
            vidasAlCoger = vidaPortador.GetCurrentLives();

        Debug.Log("Jugador " + (indicePortador + 1) + " ha cogido el diamante");
    }

    private void Update()
    {
        if (portador == null)
            return;

        if (PortadorHaSidoAlcanzado())
        {
            Debug.Log("Jugador " + (indicePortador + 1) +
                      " ha sido alcanzado y suelta el diamante");
            SoltarEnSuelo();
            return;
        }

        if (SoltarPulsado())
        {
            Debug.Log("Jugador " + (indicePortador + 1) + " suelta el diamante");
            ignorarHastaQueSalga = portador;
            transform.position = portador.transform.position + desplazamiento;
            SoltarEnSuelo();
            return;
        }

        transform.position = portador.transform.position + desplazamiento;

        foreach (Base b in bases)
        {
            if (b.Jugador != indicePortador)
                continue;

            Collider2D zona = b.GetComponent<Collider2D>();
            if (zona != null && zona.OverlapPoint(portador.transform.position))
            {
                Debug.Log("¡Jugador " + (indicePortador + 1) +
                          " ha llevado el diamante a su base!");

                if (puntos != null)
                    puntos.SumarPunto(indicePortador);

                QuitarEfectos();
                StartCoroutine(Reaparecer());
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

    private bool SoltarPulsado()
    {
        if (Time.timeScale == 0f)
            return false;

        if (indicePortador < 0)
            return false;

        if (Keyboard.current != null &&
            indicePortador < teclaSoltar.Length &&
            teclaSoltar[indicePortador] != Key.None &&
            Keyboard.current[teclaSoltar[indicePortador]].wasPressedThisFrame)
        {
            return true;
        }

        Gamepad mando = MandoDelPortador();
        return mando != null && mando.rightShoulder.wasPressedThisFrame;
    }

    private Gamepad MandoDelPortador()
    {
        PlayerInput input = portador.GetComponent<PlayerInput>();
        if (input != null && input.enabled)
        {
            foreach (InputDevice dispositivo in input.devices)
            {
                if (dispositivo is Gamepad mandoPropio)
                    return mandoPropio;
            }
        }

        if (indicePortador >= mandoDeJugador.Length)
            return null;

        int numero = mandoDeJugador[indicePortador];
        if (numero < 0 || numero >= Gamepad.all.Count)
            return null;

        return Gamepad.all[numero];
    }

    public bool Esperando
    {
        get { return esperando; }
    }

    private bool esperando = false;

    private IEnumerator Reaparecer()
    {
        esperando = true;
        Mostrar(false);
        transform.position = posicionCentro;

        Debug.Log("El diamante reaparecera en " + tiempoReaparicion + " s");

        yield return new WaitForSeconds(tiempoReaparicion);

        Mostrar(true);
        esperando = false;

        Debug.Log("El diamante ha vuelto al centro");
    }

    private void Mostrar(bool visible)
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = visible;

        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
            c.enabled = visible;
    }

    public void Soltar()
    {
        QuitarEfectos();
        transform.position = posicionCentro;
    }

    public void SoltarEnSuelo()
    {
        QuitarEfectos();

        Vector3Int casilla = suelo.WorldToCell(transform.position - desplazamiento);
        transform.position = suelo.GetCellCenterWorld(casilla);
    }

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
