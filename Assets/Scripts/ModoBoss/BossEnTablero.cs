using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Boss Volcán quieto en el centro del tablero
// - No se mueve: es un obstáculo sólido que los jugadores no atraviesan.
// - Ocupa 1 casilla (radio 0) o un cuadrado de 3x3 (radio 1).
// - Boom pregunta aquí si una explosión alcanza al Boss (Ocupa).
//
// Va en el objeto Boss, junto a VidaBoss.

[RequireComponent(typeof(VidaBoss))]
public class BossEnTablero : MonoBehaviour
{
    [Header("Posición")]
    [Tooltip("Colocar el Boss en el centro del Suelo al empezar")]
    [SerializeField] private bool colocarEnElCentro = true;

    [Header("Tamaño")]
    [Tooltip("Casillas que ocupa desde su centro hacia cada lado " +
             "(0 = 1 casilla, 1 = 3x3)")]
    [SerializeField, Min(0)] private int radio = 0;

    [Header("Mapa (si se dejan vacíos, se buscan solos)")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap suelo;

    private Vector3Int casillaCentral;
    private HashSet<Vector3Int> ocupadas = new HashSet<Vector3Int>();

    // ---------- Para otros scripts ----------

    public Vector3Int Casilla
    {
        get { return casillaCentral; }
    }

    public int Radio
    {
        get { return radio; }
    }

    public IEnumerable<Vector3Int> CasillasOcupadas
    {
        get { return ocupadas; }
    }

    // ¿El Boss está en esta casilla?
    public bool Ocupa(Vector3Int casilla)
    {
        return ocupadas.Contains(casilla);
    }


    // Awake: así el Fuego y el generador de obstáculos (que usan Start)
    // ya ven al Boss en su sitio
    private void Awake()
    {
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();

        if (suelo == null)
        {
            GameObject obj = GameObject.Find("Suelo");
            if (obj != null)
                suelo = obj.GetComponent<Tilemap>();
        }

        if (grid == null)
        {
            Debug.LogError("BossEnTablero: no hay Grid en la escena");
            return;
        }

        if (colocarEnElCentro && suelo != null)
            casillaCentral = CentroDelSuelo(suelo.cellBounds);
        else
            casillaCentral = grid.WorldToCell(transform.position);

        // Quieto en el centro de su casilla
        Vector3 posicion = grid.GetCellCenterWorld(casillaCentral);
        posicion.z = transform.position.z;
        transform.position = posicion;

        foreach (Vector3Int c in CalcularCasillas(casillaCentral, radio))
            ocupadas.Add(c);

        AjustarCollider();

        Debug.Log("Boss en la casilla " + casillaCentral +
                  " (" + ocupadas.Count + " casilla(s))");
    }

    // El collider tapa justo las casillas del Boss, para que nadie lo atraviese
    private void AjustarCollider()
    {
        BoxCollider2D caja = GetComponent<BoxCollider2D>();
        if (caja == null)
            caja = gameObject.AddComponent<BoxCollider2D>();

        caja.isTrigger = false;
        caja.offset = Vector2.zero;

        int lado = 2 * radio + 1;
        Vector3 escala = transform.lossyScale;
        caja.size = new Vector2(
            lado * grid.cellSize.x / Mathf.Max(0.0001f, Mathf.Abs(escala.x)),
            lado * grid.cellSize.y / Mathf.Max(0.0001f, Mathf.Abs(escala.y)));
    }

    // Casilla central de un rectángulo de casillas
    public static Vector3Int CentroDelSuelo(BoundsInt limites)
    {
        return new Vector3Int(
            limites.xMin + limites.size.x / 2,
            limites.yMin + limites.size.y / 2,
            0);
    }

    // Todas las casillas de un cuadrado de lado 2 * radio + 1
    public static List<Vector3Int> CalcularCasillas(Vector3Int centro, int radio)
    {
        List<Vector3Int> casillas = new List<Vector3Int>();

        for (int dx = -radio; dx <= radio; dx++)
        {
            for (int dy = -radio; dy <= radio; dy++)
                casillas.Add(centro + new Vector3Int(dx, dy, 0));
        }

        return casillas;
    }
}