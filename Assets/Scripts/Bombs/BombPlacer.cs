using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BombPlacer : MonoBehaviour
{
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private Grid grid;

    [Header("Bombas")]
    [SerializeField, Min(1)] private int capacidadBombas = 1;
    private bool bombaDobleActiva;
    private bool segundaBombaPendiente;

    [Header("Controles")]
    [SerializeField] private Key bombKey = Key.E;
    [Tooltip("Mando de este jugador (0 = primer mando). -1 = sin mando")]
    [SerializeField] private int numeroMando = 0;

    public bool usarTeclado = true;

    private readonly List<GameObject> bombasActivas =
        new List<GameObject>();

    public bool BombaExtraPendiente => bombaDobleActiva || segundaBombaPendiente;

    public int CapacidadBombas => capacidadBombas + (BombaExtraPendiente ? 1 : 0);

    private void Update()
    {
       bool teclado = Keyboard.current != null &&
               bombKey != Key.None &&
               Keyboard.current[bombKey].wasPressedThisFrame;

        bool mando = numeroMando >= 0 &&
             numeroMando < Gamepad.all.Count &&
             Gamepad.all[numeroMando].buttonSouth.wasPressedThisFrame;

        if (usarTeclado && (teclado || mando))
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

        bombasActivas.RemoveAll(bomba => bomba == null);

        if (bombasActivas.Count >= CapacidadBombas)
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
        if (bombaDobleActiva)
        {
            bombaDobleActiva = false;
            segundaBombaPendiente = true;
        }
        else if (segundaBombaPendiente)
        {
            segundaBombaPendiente = false;
        }

        Boom boom = bombaNueva.GetComponent<Boom>();
        if (boom != null)
            boom.SetDueno(gameObject);
    }

    public bool ActivarBombaDoble()
    {
        if (BombaExtraPendiente)
            return false;

        bombaDobleActiva = true;
        return true;
    }

    public bool EmpujarBomba(int distancia)
    {
        if (grid == null)
            return false;

        Vector3Int celdaJugador = grid.WorldToCell(transform.position);
        Boom bombaMasCercana = null;
        Vector3Int diferenciaElegida = Vector3Int.zero;
        int distanciaMinima = int.MaxValue;

        foreach (Boom bomba in FindObjectsByType<Boom>())
        {
            Vector3Int diferencia =
                grid.WorldToCell(bomba.transform.position) - celdaJugador;
            int distanciaEnCeldas = Mathf.Abs(diferencia.x) + Mathf.Abs(diferencia.y);

            if (distanciaEnCeldas == 0 || distanciaEnCeldas >= distanciaMinima)
                continue;

            bombaMasCercana = bomba;
            diferenciaElegida = diferencia;
            distanciaMinima = distanciaEnCeldas;
        }

        if (bombaMasCercana == null)
            return false;

        Vector2 direccion;
        if (Mathf.Abs(diferenciaElegida.x) > Mathf.Abs(diferenciaElegida.y))
            direccion = new Vector2(Mathf.Sign(diferenciaElegida.x), 0f);
        else
            direccion = new Vector2(0f, Mathf.Sign(diferenciaElegida.y));

        return bombaMasCercana.Empujar(direccion, distancia);
    }
}
