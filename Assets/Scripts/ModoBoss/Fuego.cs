using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Fuego del Boss Volcán (tarjeta Lab3 - MB - Fuego)
// Cada cierto tiempo el volcán lanza una cruz de fuego hacia los 4 lados,
// desde el volcán hasta el borde del tablero.
// - Los muros fijos y los obstáculos paran el fuego.
// - Antes de quemar, el suelo se pone rojo 1 s (AvisoSuelo).
// - Solo hace daño en casillas avisadas.
// - Cuánto dura y cada cuánto sale lo decide VidaBoss según la fase
//   (en la fase 2 sale más a menudo).
// - A quien quema le resta puntos (PuntosBoss.RestarDanoBoss).
//
// Va en el mismo objeto que VidaBoss (el volcán).
public class Fuego : MonoBehaviour
{
    [Header("Referencias (si se dejan vacías, se buscan solas)")]
    [SerializeField] private VidaBoss boss;
    [SerializeField] private AvisoSuelo aviso;
    [SerializeField] private PuntosBoss puntos;
    [Tooltip("Centro del volcán. Si se deja vacío, se usa el objeto del Boss")]
    [SerializeField] private Transform volcan;
    [Tooltip("Lo que se ve en cada casilla mientras quema (opcional)")]
    [SerializeField] private GameObject prefabFuego;

    [Header("Forma")]
    [Tooltip("Casillas que ocupa el volcán desde su centro hacia cada lado " +
             "(0 = 1 casilla, 1 = volcán de 3x3). El fuego sale desde su borde")]
    [SerializeField, Min(0)] private int radioVolcan = 0;

    private Grid grid;
    private Tilemap muros;
    private Tilemap obstaculos;
    private PartidaManager partida;

    private static readonly Vector3Int[] direcciones =
        { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

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

        if (volcan == null)
            volcan = boss != null ? boss.transform : transform;

        grid = FindAnyObjectByType<Grid>();
        muros = BuscarTilemap("MurosFijos");
        obstaculos = BuscarTilemap("ObstaculosDestructibles");
        partida = FindAnyObjectByType<PartidaManager>();

        if (boss == null || aviso == null || grid == null || muros == null)
        {
            Debug.LogError("Fuego: falta VidaBoss, AvisoSuelo, el Grid o los MurosFijos.");
            enabled = false;
            return;
        }

        StartCoroutine(Ciclo());
    }

    // ---------- Ataque ----------

    private IEnumerator Ciclo()
    {
        while (!boss.EstaMuerto())
        {
            // La pausa depende de la fase (en la fase 2 es más corta)
            yield return new WaitForSeconds(boss.PausaFuego());

            if (boss.EstaMuerto())
                break;

            yield return Atacar();
        }
    }

    // Un ataque completo: aviso rojo -> fuego -> se apaga
    public IEnumerator Atacar()
    {
        List<Vector3Int> casillas = CasillasDeFuego();

        // 1. Aviso en el suelo (1 s)
        yield return aviso.Avisar(casillas);

        // 2. Fuego: mientras dura, quema a quien esté dentro
        //    (a cada jugador solo una vez por ataque)
        List<GameObject> llamas = MostrarFuego(casillas);
        HashSet<PlayerDamage> quemados = new HashSet<PlayerDamage>();

        float fin = Time.time + boss.DuracionFuego();
        while (Time.time < fin)
        {
            Quemar(casillas, quemados);
            yield return null; // siguiente frame
        }

        // 3. Se apaga
        foreach (GameObject llama in llamas)
        {
            if (llama != null)
                Destroy(llama);
        }

        aviso.QuitarAviso(casillas);
    }

    // Cruz desde el borde del volcán hasta que choca con un muro o un obstáculo
    public List<Vector3Int> CasillasDeFuego()
    {
        List<Vector3Int> casillas = new List<Vector3Int>();
        Vector3Int centro = grid.WorldToCell(volcan.position);

        foreach (Vector3Int d in direcciones)
        {
            // Límite de seguridad por si el tablero no tuviera borde
            for (int i = radioVolcan + 1; i < 50; i++)
            {
                Vector3Int c = centro + d * i;

                if (muros.HasTile(c))
                    break;

                if (obstaculos != null && obstaculos.HasTile(c))
                    break;

                casillas.Add(c);
            }
        }

        return casillas;
    }

    // Quita una vida (y puntos) a los jugadores que estén en el fuego.
    // Solo en casillas avisadas: ningún ataque sin aviso.
    private void Quemar(List<Vector3Int> casillas, HashSet<PlayerDamage> quemados)
    {
        foreach (PlayerDamage jugador in FindObjectsByType<PlayerDamage>())
        {
            if (quemados.Contains(jugador))
                continue;

            Vector3Int c = grid.WorldToCell(jugador.transform.position);
            if (!casillas.Contains(c) || !aviso.EstaAvisada(c))
                continue;

            // Con el escudo activo no le pasa nada
            PlayerShieldPowerUps escudo = jugador.GetComponentInParent<PlayerShieldPowerUps>();
            if (escudo != null && escudo.EscudoActivo)
                continue;

            jugador.TakeDamage();
            quemados.Add(jugador);
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

    private List<GameObject> MostrarFuego(List<Vector3Int> casillas)
    {
        List<GameObject> llamas = new List<GameObject>();
        if (prefabFuego == null)
            return llamas;

        foreach (Vector3Int c in casillas)
        {
            Vector3 posicion = grid.GetCellCenterWorld(c);
            posicion.z = 0f;
            llamas.Add(Instantiate(prefabFuego, posicion, Quaternion.identity));
        }
        return llamas;
    }

    private Tilemap BuscarTilemap(string nombre)
    {
        GameObject obj = GameObject.Find(nombre);
        return obj != null ? obj.GetComponent<Tilemap>() : null;
    }
}
