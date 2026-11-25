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

        tilemap.SetTile(cell, null);

        Debug.Log("Obstáculo destruido en: " + cell);

        StartCoroutine(RespawnBlock(cell, originalTile));

        return true;
    }

    private IEnumerator RespawnBlock(
        Vector3Int cell,
        TileBase originalTile)
    {
        yield return new WaitForSeconds(respawnTime);

        while (IsCellOccupied(cell))
        {
            yield return new WaitForSeconds(0.5f);
        }

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
            if (hit.GetComponentInParent<PlayerMovement>() != null)
                return true;

            if (hit.GetComponentInParent<Boom>() != null)
                return true;
        }

        return false;
    }
}
