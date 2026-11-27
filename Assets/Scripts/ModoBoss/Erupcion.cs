using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Erupción del Boss Volcán (tarjeta Lab3 - MB - Erupción)
// Solo en la fase 3 (menos de 20 HP), cada cierto tiempo el volcán
// lanza 4 bolas de fuego a la vez:
// - Caen donde están los jugadores (una por jugador vivo). Si quedan
//   menos de 4 jugadores, el resto cae en casillas libres al azar.
// - Antes de caer, el suelo se pone rojo 1 s (AvisoSuelo): da tiempo
//   a apartarse. La bola cae en la casilla avisada, no persigue.
// - Solo hace daño en casillas avisadas.
// - Con el escudo activo no pasa nada.
// - A quien alcanza le resta puntos (PuntosBoss.RestarDanoBoss).
//
// Va en el mismo objeto que VidaBoss y Fuego (el volcán).
public class Erupcion : MonoBehaviour
{
    [Header("Erupción")]
    [Tooltip("Bolas de fuego que caen a la vez")]
    [SerializeField, Min(1)] private int cantidadBolas = 4;

    [Tooltip("Segundos entre una erupción y la siguiente (solo en fase 3)")]
    [SerializeField, Min(0.5f)] private float pausaErupcion = 4f;

    [Tooltip("Segundos que la bola quema la casilla al caer")]
    [SerializeField, Min(0.05f)] private float duracionImpacto = 0.5f;

    [Header("Referencias (si se dejan vacías, se buscan solas)")]
    [SerializeField] private VidaBoss boss;
    [SerializeField] private AvisoSuelo aviso;
    [SerializeField] private PuntosBoss puntos;
    [Tooltip("Lo que se ve en cada casilla al caer la bola (opcional)")]
    [SerializeField] private GameObject prefabBola;

    [Header("Volcán")]
    [Tooltip("Casillas que ocupa el volcán desde su centro (igual que en Fuego). " +
             "Las bolas no caen encima del volcán")]
    [SerializeField, Min(0)] private int radioVolcan = 0;

    private Grid grid;
    private Tilemap suelo;
    private Tilemap muros;
    private Tilemap obstaculos;
    private PartidaManager partida;

    private void Start()
    {
        if (boss == null)
            boss = GetComponent<VidaBoss>();
        if (boss == null)
            boss = FindAnyObjectByType<VidaBoss>();

        if (aviso == null)
            aviso = FindAnyObjectByType<AvisoSuelo>();

        if (puntos == null)
            puntos = FindAnyObjectByType<PuntosBoss>();

        grid = FindAnyObjectByType<Grid>();
        suelo = BuscarTilemap("Suelo");
        muros = BuscarTilemap("MurosFijos");
        obstaculos = BuscarTilemap("ObstaculosDestructibles");
        partida = FindAnyObjectByType<PartidaManager>();

        if (boss == null || aviso == null || grid == null || suelo == null || muros == null)
        {
            Debug.LogError("Erupcion: falta VidaBoss, AvisoSuelo, el Grid, el Suelo o los MurosFijos.");
            enabled = false;
            return;
        }

        StartCoroutine(Ciclo());
    }

    // Ciclo
    private IEnumerator Ciclo()
    {
        while (!boss.EstaMuerto())
        {
            yield return new WaitForSeconds(pausaErupcion);

            if (boss.EstaMuerto())
                break;

            // Solo en la fase 3
            if (!boss.HayErupcion())
                continue;

            yield return Erupcionar();
        }
    }

    // Una erupción completa: elegir casillas -> aviso rojo -> caen las bolas
    public IEnumerator Erupcionar()
    {
        // 1. Las casillas se eligen ahora: la bola cae donde estaba el jugador
        List<Vector3Int> casillas = ElegirCasillas(
            CasillasDeJugadores(), CasillasLibres(), cantidadBolas);

        Debug.Log("Erupción: " + casillas.Count + " bolas de fuego");

        // 2. Aviso en el suelo
        yield return aviso.Avisar(casillas);

        // 3. Caen las bolas: queman a quien esté en esas casillas
        List<GameObject> bolas = MostrarBolas(casillas);
        HashSet<PlayerDamage> alcanzados = new HashSet<PlayerDamage>();

        float fin = Time.time + duracionImpacto;
        while (Time.time < fin)
        {
            Quemar(casillas, alcanzados);
            yield return null;
        }

        // 4. Se apagan
        foreach (GameObject bola in bolas)
        {
            if (bola != null)
                Destroy(bola);
        }

        aviso.QuitarAviso(casillas);
    }

    // Elegir casillas

    // Primero las casillas de los jugadores (sin repetir); si faltan
    // bolas, se completan con casillas libres al azar.
    // Pública y estática para poder probarla en los tests.
    public static List<Vector3Int> ElegirCasillas(
        List<Vector3Int> casillasJugadores, List<Vector3Int> casillasLibres, int cantidad)
    {
        List<Vector3Int> elegidas = new List<Vector3Int>();

        foreach (Vector3Int c in casillasJugadores)
        {
            if (elegidas.Count >= cantidad)
                break;

            if (!elegidas.Contains(c))
                elegidas.Add(c);
        }

        List<Vector3Int> resto = new List<Vector3Int>();
        foreach (Vector3Int c in casillasLibres)
        {
            if (!elegidas.Contains(c))
                resto.Add(c);
        }

        while (elegidas.Count < cantidad && resto.Count > 0)
        {
            int i = Random.Range(0, resto.Count);
            elegidas.Add(resto[i]);
            resto.RemoveAt(i);
        }

        return elegidas;
    }

    // Solo si cae ahí y la casilla está avisada.
    public static bool DebeDanar(Vector3Int casillaJugador,
                                 List<Vector3Int> casillasBolas, AvisoSuelo aviso)
    {
        return casillasBolas.Contains(casillaJugador) &&
               aviso != null && aviso.EstaAvisada(casillaJugador);
    }

    private List<Vector3Int> CasillasDeJugadores()
    {
        List<Vector3Int> casillas = new List<Vector3Int>();

        foreach (PlayerDamage jugador in FindObjectsByType<PlayerDamage>())
        {
            if (!jugador.gameObject.activeInHierarchy || jugador.IsDead())
                continue;

            casillas.Add(grid.WorldToCell(jugador.transform.position));
        }

        return casillas;
    }

    // Suelo sin muros, sin obstáculos y fuera del volcán
    private List<Vector3Int> CasillasLibres()
    {
        List<Vector3Int> libres = new List<Vector3Int>();
        Vector3Int centroVolcan = grid.WorldToCell(boss.transform.position);

        foreach (Vector3Int c in suelo.cellBounds.allPositionsWithin)
        {
            if (!suelo.HasTile(c) || muros.HasTile(c))
                continue;

            if (obstaculos != null && obstaculos.HasTile(c))
                continue;

            if (Mathf.Abs(c.x - centroVolcan.x) <= radioVolcan &&
                Mathf.Abs(c.y - centroVolcan.y) <= radioVolcan)
                continue;

            libres.Add(c);
        }

        return libres;
    }

    // Daño
    private void Quemar(List<Vector3Int> casillas, HashSet<PlayerDamage> alcanzados)
    {
        foreach (PlayerDamage jugador in FindObjectsByType<PlayerDamage>())
        {
            if (alcanzados.Contains(jugador))
                continue;

            Vector3Int c = grid.WorldToCell(jugador.transform.position);
            if (!DebeDanar(c, casillas, aviso))
                continue;

            // Con el escudo activo no le pasa nada
            PlayerShieldPowerUps escudo = jugador.GetComponentInParent<PlayerShieldPowerUps>();
            if (escudo != null && escudo.EscudoActivo)
                continue;

            jugador.TakeDamage();
            alcanzados.Add(jugador);
            RestarPuntos(jugador);
        }
    }

    private void RestarPuntos(PlayerDamage jugador)
    {
        if (puntos == null || partida == null)
            return;

        PlayerMovement movimiento = jugador.GetComponentInParent<PlayerMovement>();
        if (movimiento == null)
            return;

        int indice = partida.IndiceDe(movimiento.gameObject);
        if (indice >= 0)
            puntos.RestarDanoBoss(indice);
    }

    private List<GameObject> MostrarBolas(List<Vector3Int> casillas)
    {
        List<GameObject> bolas = new List<GameObject>();
        if (prefabBola == null)
            return bolas;

        foreach (Vector3Int c in casillas)
        {
            Vector3 posicion = grid.GetCellCenterWorld(c);
            posicion.z = 0f;
            bolas.Add(Instantiate(prefabBola, posicion, Quaternion.identity));
        }

        return bolas;
    }

    private Tilemap BuscarTilemap(string nombre)
    {
        GameObject obj = GameObject.Find(nombre);
        return obj != null ? obj.GetComponent<Tilemap>() : null;
    }
}