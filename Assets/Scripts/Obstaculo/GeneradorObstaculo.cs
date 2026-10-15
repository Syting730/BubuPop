using UnityEngine;
using UnityEngine.Tilemaps;

public class GeneradorObstaculos : MonoBehaviour
{
    private Tilemap tilemap;

    [Header("El bloque que se va a pintar")]
    public TileBase tileObstaculo;

    [Header("Límites de tu mapa")]
    public int xMin = -7;
    public int xMax = 7;
    public int yMin = -5;
    public int yMax = 5;

    [Header("Zonas Seguras (Coordenadas de inicio)")]
    public Vector2Int spawnJugador1; // <--- NUEVO
    public Vector2Int spawnJugador2; // <--- NUEVO

    [Header("Cantidad aleatoria")]
    [Range(0f, 1f)]
    public float probabilidad = 0.3f;

    void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    void Start()
    {
        if (tileObstaculo != null)
        {
            GenerarBloques();
        }
    }

    void GenerarBloques()
    {
        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                // Evitar muros fijos (suelen estar en coordenadas impares)
                if (Mathf.Abs(x) % 2 == 1 && Mathf.Abs(y) % 2 == 1) continue;

                // Evitar encerrar a los jugadores
                if (EsZonaSegura(x, y)) continue;

                // Probabilidad aleatoria
                if (Random.value <= probabilidad)
                {
                    Vector3Int celda = new Vector3Int(x, y, 0);
                    if (!tilemap.HasTile(celda))
                    {
                        tilemap.SetTile(celda, tileObstaculo);
                    }
                }
            }
        }
    }

    bool EsZonaSegura(int x, int y)
    {
        // La suma de valores absolutos (distancia de Manhattan) <= 1
        // protege la casilla central y las 4 casillas adyacentes (arriba, abajo, izq, der).
        
        if (Mathf.Abs(x - spawnJugador1.x) + Mathf.Abs(y - spawnJugador1.y) <= 2) return true;
        if (Mathf.Abs(x - spawnJugador2.x) + Mathf.Abs(y - spawnJugador2.y) <= 2) return true;

        return false;
    }
}