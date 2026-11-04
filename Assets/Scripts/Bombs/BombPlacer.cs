using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BombPlacer : MonoBehaviour
{
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private Grid grid;

    [Header("Bombas")]
    [SerializeField, Min(1)] private int capacidadBombas = 1;

    [Header("Controles")]
    // Jugador 1: Space, Jugador 2: Enter
    [SerializeField] private Key bombKey = Key.E;

    // IA = false para no colocar bombas con el teclado.
    public bool usarTeclado = true;

    private readonly List<GameObject> bombasActivas =
        new List<GameObject>();

    public int CapacidadBombas => capacidadBombas;

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current[bombKey].wasPressedThisFrame)
        {
            PlaceBomb();
        }
    }

    public void PlaceBomb()
    {
        if (bombPrefab == null || grid == null)
        {
            Debug.LogError("Falta asignar Bomb Prefab o Grid.");
            return;
        }

        // Quita de la lista las bombas que ya explotaron y se destruyeron.
        bombasActivas.RemoveAll(bomba => bomba == null);

        if (bombasActivas.Count >= capacidadBombas)
            return;

        Vector3Int cellPosition =
            grid.WorldToCell(transform.position);

        Vector3 bombPosition =
            grid.GetCellCenterWorld(cellPosition);

        bombPosition.z = 0f;

        GameObject bombaNueva = Instantiate(
            bombPrefab,
            bombPosition,
            Quaternion.identity
        );

        bombasActivas.Add(bombaNueva);

        // La bomba recuerda quien la puso (para los puntos del modo AD)
        Boom boom = bombaNueva.GetComponent<Boom>();
        if (boom != null)
            boom.SetDueno(gameObject);
    }

    public bool AumentarCapacidadBombas(
        int cantidad,
        int capacidadMaxima
    )
    {
        if (cantidad <= 0 || capacidadBombas >= capacidadMaxima)
            return false;

        capacidadBombas = Mathf.Min(
            capacidadBombas + cantidad,
            capacidadMaxima
        );

        return true;
    }

    public bool EmpujarBomba(int distancia)
    {
        if (grid == null)
            return false;

        bombasActivas.RemoveAll(bomba => bomba == null);

        // Busca empezando por la bomba activa más reciente.
        for (int i = bombasActivas.Count - 1; i >= 0; i--)
        {
            GameObject bomba = bombasActivas[i];
            Boom boom = bomba.GetComponent<Boom>();

            if (boom == null)
                continue;

            Vector3Int celdaJugador =
                grid.WorldToCell(transform.position);

            Vector3Int celdaBomba =
                grid.WorldToCell(bomba.transform.position);

            Vector3Int diferencia = celdaBomba - celdaJugador;

            // En la misma casilla no se puede determinar
            // desde qué lado empujar.
            if (diferencia == Vector3Int.zero)
                return false;

            Vector2 direccion;

            if (Mathf.Abs(diferencia.x) > Mathf.Abs(diferencia.y))
            {
                direccion = new Vector2(
                    Mathf.Sign(diferencia.x),
                    0f
                );
            }
            else
            {
                direccion = new Vector2(
                    0f,
                    Mathf.Sign(diferencia.y)
                );
            }

            return boom.Empujar(direccion, distancia);
        }

        return false;
    }
}