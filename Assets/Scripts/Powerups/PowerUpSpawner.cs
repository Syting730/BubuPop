using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [FormerlySerializedAs("prefabEscudo")]
    [SerializeField] private GameObject prefabPowerUp;

    [Header("Mapa")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap suelo;
    [SerializeField] private Tilemap murosFijos;
    [SerializeField] private Tilemap obstaculosDestructibles;

    [Header("Aparición de power-ups")]
    [SerializeField, Min(1f)] private float intervaloSegundos = 20f;

    [FormerlySerializedAs("escudosPorCiclo")]
    [SerializeField, Min(1)] private int powerUpsPorCiclo = 1;

    [FormerlySerializedAs("maxEscudosEnMapa")]
    [SerializeField, Min(1)] private int maxPowerUpsEnMapa = 3;

    private readonly List<GameObject> powerUpsActivos =
        new List<GameObject>();

    private void Start()
    {
        if (prefabPowerUp == null ||
            grid == null ||
            suelo == null ||
            murosFijos == null ||
            obstaculosDestructibles == null)
        {
            Debug.LogError(
                "PowerUpSpawner: asigna el prefab, el Grid y los Tilemaps."
            );

            enabled = false;
            return;
        }

        StartCoroutine(CicloAparicion());
    }

    private IEnumerator CicloAparicion()
    {
        // Deja que el generador de obstáculos termine primero.
        yield return null;

        while (true)
        {
            powerUpsActivos.RemoveAll(powerUp => powerUp == null);

            int espaciosLibres =
                maxPowerUpsEnMapa - powerUpsActivos.Count;

            int cantidadACrear =
                Mathf.Min(powerUpsPorCiclo, espaciosLibres);

            for (int i = 0; i < cantidadACrear; i++)
            {
                if (!CrearPowerUpEnCasillaAleatoria())
                    break;
            }

            yield return new WaitForSeconds(intervaloSegundos);
        }
    }

    private bool CrearPowerUpEnCasillaAleatoria()
    {
        List<Vector3Int> casillasLibres = new List<Vector3Int>();
        BoundsInt limites = suelo.cellBounds;

        foreach (Vector3Int casilla in limites.allPositionsWithin)
        {
            if (!suelo.HasTile(casilla))
                continue;

            if (murosFijos.HasTile(casilla))
                continue;

            if (obstaculosDestructibles.HasTile(casilla))
                continue;

            if (HayJugadorBombaOEfectoEnCasilla(casilla))
                continue;

            casillasLibres.Add(casilla);
        }

        if (casillasLibres.Count == 0)
        {
            Debug.LogWarning(
                "PowerUpSpawner: no encontró una casilla libre."
            );

            return false;
        }

        int indice = Random.Range(0, casillasLibres.Count);

        Vector3 posicion =
            grid.GetCellCenterWorld(casillasLibres[indice]);

        posicion.z = 0f;

        GameObject powerUp = Instantiate(
            prefabPowerUp,
            posicion,
            Quaternion.identity
        );

        powerUpsActivos.Add(powerUp);
        return true;
    }

    private bool HayJugadorBombaOEfectoEnCasilla(Vector3Int casilla)
    {
        Vector3 centro = grid.GetCellCenterWorld(casilla);

        Vector2 tamano = new Vector2(
            grid.cellSize.x * 0.8f,
            grid.cellSize.y * 0.8f
        );

        Collider2D[] objetos = Physics2D.OverlapBoxAll(
            centro,
            tamano,
            0f
        );

        foreach (Collider2D objeto in objetos)
        {
            if (objeto.GetComponentInParent<PlayerMovement>() != null)
                return true;

            if (objeto.GetComponentInParent<Boom>() != null)
                return true;

            if (objeto.GetComponentInParent<PowerUpPickup>() != null)
                return true;
        }

        return false;
    }
}