using UnityEngine;
using UnityEngine.InputSystem;

public class BombPlacer : MonoBehaviour
{
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private Grid grid;

    [Header("Controles")]
    // Jugador 1: Space, Jugador 2: Enter
    [SerializeField] private Key bombKey = Key.E;

    private GameObject currentBomb;

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current[bombKey].wasPressedThisFrame)
        {
            PlaceBomb();
        }
    }

    void PlaceBomb()
    {
        if (bombPrefab == null || grid == null)
        {
            Debug.LogError("Falta asignar Bomb Prefab o Grid.");
            return;
        }

        if (currentBomb != null)
            return;

        // Obtener la casilla donde está el jugador
        Vector3Int cellPosition =
            grid.WorldToCell(transform.position);

        // Obtener el centro de esa casilla
        Vector3 bombPosition =
            grid.GetCellCenterWorld(cellPosition);

        bombPosition.z = 0;

        currentBomb = Instantiate(
            bombPrefab,
            bombPosition,
            Quaternion.identity
        );
    }
}