
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DestructibleTilemap : MonoBehaviour
{
    [SerializeField] private float respawnTime = 20f;

    private Tilemap tilemap;
    private Grid grid;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        grid = GetComponentInParent<Grid>();
    }

    public bool DestroyBlock(Vector3 worldPosition)
    {
        Vector3Int cell = tilemap.WorldToCell(worldPosition);

        TileBase originalTile = tilemap.GetTile(cell);

        if (originalTile == null)
            return false;

        // Eliminar el obstáculo
        tilemap.SetTile(cell, null);

        Debug.Log("Obstáculo destruido en: " + cell);

        // Comenzar la cuenta atrás
        StartCoroutine(RespawnBlock(cell, originalTile));

        return true;
    }

    private IEnumerator RespawnBlock(
        Vector3Int cell,
        TileBase originalTile)
    {
        // Esperar 20 segundos
        yield return new WaitForSeconds(respawnTime);

        // Si hay alguien en la casilla, esperar
        while (IsCellOccupied(cell))
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Restaurar el obstáculo
        if (!tilemap.HasTile(cell))
        {
            tilemap.SetTile(cell, originalTile);

            Debug.Log("Obstáculo reaparecido en: " + cell);
        }
    }

    private bool IsCellOccupied(Vector3Int cell)
    {
        if (grid == null)
            return true;

        Vector3 center = grid.GetCellCenterWorld(cell);
        Vector3 size = grid.cellSize;

        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            center,
            new Vector2(size.x * 0.8f, size.y * 0.8f),
            0f
        );

        foreach (Collider2D hit in colliders)
        {
            // No reaparecer si hay un jugador
            if (hit.GetComponentInParent<PlayerMovement>() != null)
                return true;

            // No reaparecer si hay una bomba
            if (hit.GetComponentInParent<Boom>() != null)
                return true;
        }

        return false;
    }
}
