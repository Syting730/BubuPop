
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Boom : MonoBehaviour
{
    [Header("Configuracion de la bomba")]
    [SerializeField] private float explosionDelay = 2f;

    // Casillas que alcanza la explosion en cada direccion
    [SerializeField] private int explosionRange = 2;

    [Header("Referencias del mapa")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap destructibleTilemap;
    [SerializeField] private Tilemap fixedWallsTilemap;

    [Header("Aura de peligro")]
    [SerializeField] private GameObject warningPrefab;

    private bool exploded = false;

    // Jugador que ha puesto la bomba (para los puntos del modo AD)
    private GameObject dueno;

    // La llama BombPlacer al poner la bomba
    public void SetDueno(GameObject jugador)
    {
        dueno = jugador;
    }

    private List<GameObject> warningObjects =
        new List<GameObject>();

    private static readonly Vector3Int[] directions =
    {
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.left,
        Vector3Int.right
    };

    private void Start()
    {
        // Buscar las referencias del mapa
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();

        if (destructibleTilemap == null)
        {
            GameObject obj =
                GameObject.Find("ObstaculosDestructibles");

            if (obj != null)
                destructibleTilemap = obj.GetComponent<Tilemap>();
        }

        if (fixedWallsTilemap == null)
        {
            GameObject obj =
                GameObject.Find("MurosFijos");

            if (obj != null)
                fixedWallsTilemap = obj.GetComponent<Tilemap>();
        }

        // Comprobar que todas las referencias existen
        if (grid == null ||
            destructibleTilemap == null ||
            fixedWallsTilemap == null)
        {
            Debug.LogError(
                "Boom: faltan referencias del mapa."
            );

            Destroy(gameObject);
            return;
        }

        // Mostrar el aura inmediatamente
        ShowWarning();

        // Iniciar temporizador de explosion
        StartCoroutine(ExplosionTimer());
    }

    private IEnumerator ExplosionTimer()
    {
        yield return new WaitForSeconds(explosionDelay);

        Explode();
    }

    // Calcula las casillas afectadas por la bomba
    private List<Vector3Int> GetAffectedCells()
    {
        List<Vector3Int> affectedCells =
            new List<Vector3Int>();

        Vector3Int bombCell =
            grid.WorldToCell(transform.position);

        // Casilla central
        affectedCells.Add(bombCell);

        // Hasta explosionRange casillas en cada direccion
        foreach (Vector3Int direction in directions)
        {
            for (int i = 1; i <= explosionRange; i++)
            {
                Vector3Int targetCell = bombCell + direction * i;

                // Los muros fijos bloquean la explosion
                if (fixedWallsTilemap.HasTile(targetCell))
                    break;

                affectedCells.Add(targetCell);

                // Un obstaculo destructible se rompe pero
                // la explosion no sigue detras de el
                if (destructibleTilemap.HasTile(targetCell))
                    break;
            }
        }

        return affectedCells;
    }

    // Mostrar el aura de peligro
    private void ShowWarning()
    {
        if (warningPrefab == null)
        {
            Debug.LogWarning(
                "Boom: no se ha asignado ExplosionWarning."
            );
            return;
        }

        foreach (Vector3Int cell in GetAffectedCells())
        {
            CreateWarning(cell);
        }
    }

    private void CreateWarning(Vector3Int cell)
    {
        Vector3 position =
            grid.GetCellCenterWorld(cell);

        position.z = 0f;

        GameObject warning = Instantiate(
            warningPrefab,
            position,
            Quaternion.identity
        );

        // Ajustar el aura al tamaño de la casilla
        Vector3 cellSize = grid.cellSize;

        warning.transform.localScale =
            new Vector3(cellSize.x, cellSize.y, 1f);

        warningObjects.Add(warning);
    }

    // Explosion de la bomba
    public void Explode()
    {
        if (exploded)
            return;

        exploded = true;

        // Comprobacion de seguridad
        if (grid == null ||
            destructibleTilemap == null ||
            fixedWallsTilemap == null)
        {
            Debug.LogError(
                "Boom: no se puede explotar porque faltan referencias."
            );

            RemoveWarnings();
            Destroy(gameObject);
            return;
        }

        // Obtener una sola vez las casillas afectadas
        List<Vector3Int> affectedCells =
            GetAffectedCells();

        // Aplicar daño al jugador
        DamagePlayers(affectedCells);

        // Destruir obstaculos alcanzados
        DestructibleTilemap manager =
            destructibleTilemap.GetComponent<DestructibleTilemap>();

        int obstaculosRotos = 0;

        if (manager != null)
        {
            foreach (Vector3Int cell in affectedCells)
            {
                if (destructibleTilemap.HasTile(cell))
                {
                    Vector3 worldPosition =
                        grid.GetCellCenterWorld(cell);

                    if (manager.DestroyBlock(worldPosition))
                        obstaculosRotos++;
                }
            }
        }
        else
        {
            Debug.LogWarning(
                "Boom: falta el componente DestructibleTilemap."
            );
        }

        // Modo AD: +1 punto por cada obstaculo roto
        if (obstaculosRotos > 0)
        {
            PuntosAD puntos = FindAnyObjectByType<PuntosAD>();
            int indiceDueno = IndiceJugador(dueno);

            if (puntos != null && indiceDueno >= 0)
                puntos.SumarObstaculos(indiceDueno, obstaculosRotos);
        }

        // Eliminar el aura
        RemoveWarnings();

        // Eliminar la bomba
        Destroy(gameObject);
    }

    // Eliminar todos los cuadrados del aura
    private void RemoveWarnings()
    {
        foreach (GameObject warning in warningObjects)
        {
            if (warning != null)
                Destroy(warning);
        }

        warningObjects.Clear();
    }

    // Comprobar si hay jugadores dentro de la explosion
    private void DamagePlayers(List<Vector3Int> affectedCells)
    {
        PlayerDamage[] players =
            FindObjectsByType<PlayerDamage>();

        // Modo AD: puntos por eliminar a otro jugador
        PuntosAD puntos = FindAnyObjectByType<PuntosAD>();
        int indiceDueno = IndiceJugador(dueno);

        foreach (PlayerDamage player in players)
        {
            Vector3Int playerCell =
                grid.WorldToCell(player.transform.position);

            if (affectedCells.Contains(playerCell))
            {
                // Recien reaparecido: no recibe daño ni da puntos
                if (player.IsInvulnerable())
                    continue;

                // Los puntos se calculan antes del golpe.
                // Darse a uno mismo no da puntos (ni los quita).
                int indiceVictima = IndiceJugador(player.gameObject);

                if (puntos != null && indiceDueno >= 0 &&
                    indiceVictima >= 0 && indiceVictima != indiceDueno)
                {
                    puntos.SumarEliminacion(indiceDueno, indiceVictima);
                }

                player.TakeDamage();
            }
        }
    }

    // Numero del jugador (0 = J1 ... 3 = J4), -1 si no se sabe
    private int IndiceJugador(GameObject jugador)
    {
        if (jugador == null)
            return -1;

        PartidaManager partida = FindAnyObjectByType<PartidaManager>();
        if (partida == null)
            return -1;

        return partida.IndiceDe(jugador);
    }

    private void OnDestroy()
    {
        // Limpieza adicional si la bomba se elimina
        RemoveWarnings();
    }
}
