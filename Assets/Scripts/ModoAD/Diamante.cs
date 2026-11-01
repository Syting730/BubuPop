using UnityEngine;
using UnityEngine.Tilemaps;

// Diamante del modo Asalto al Diamante:
// - se coloca en la casilla central del tablero al empezar
// - un jugador lo coge al pasar por encima y el diamante le sigue
// - si el portador llega a SU base, el diamante vuelve al centro
// (Ralentizacion, bloqueo de bombas y puntos:
//  tarjetas de otros companeros)
public class Diamante : MonoBehaviour
{
    [Header("Tilemap del suelo (para calcular el centro)")]
    [SerializeField] private Tilemap suelo;

    [Header("Jugadores (en orden: J1, J2, J3, J4)")]
    [SerializeField] private GameObject[] jugadores = new GameObject[4];

    [Header("Posicion del diamante encima del portador")]
    [SerializeField] private Vector3 desplazamiento = new Vector3(0f, 0.5f, 0f);

    private Vector3 posicionCentro;
    private GameObject portador;
    private int indicePortador = -1;
    private Base[] bases;

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

    // Un jugador pasa por encima del diamante
    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (portador != null)
            return;

        PlayerMovement jugador = otro.GetComponentInParent<PlayerMovement>();
        if (jugador == null)
            return;

        portador = jugador.gameObject;
        indicePortador = System.Array.IndexOf(jugadores, portador);

        Debug.Log("Jugador " + (indicePortador + 1) + " ha cogido el diamante");
    }

    // ---------- Llevar y llegar a la base ----------

    private void Update()
    {
        if (portador == null)
            return;

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

    // Suelta el diamante y lo devuelve al centro
    public void Soltar()
    {
        portador = null;
        indicePortador = -1;
        transform.position = posicionCentro;
    }
}