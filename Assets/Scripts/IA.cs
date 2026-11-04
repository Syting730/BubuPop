using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class IA : MonoBehaviour
{
    [Tooltip("1 = J1, 2 = J2, 3 = J3, 4 = J4")]
    public int numeroJugador = 1;

    [Header("Burbujas")]
    [Tooltip("Tiene que ser igual que Explosion Range del prefab Bomb (Boom)")]
    public int alcanceExplosion = 2;
    public float tiempoEntreBurbujas = 3f;

    [Header("Movimiento")]
    [Tooltip("1 = igual que un jugador, 0.5 = la mitad de rápido")]
    [Range(0.1f, 1f)]
    public float factorVelocidad = 0.6f;

    private Rigidbody2D rb;
    private Grid grid;
    private Tilemap muros;
    private Tilemap obstaculos;
    private PlayerMovement movimiento; // de aqui saca la velocidad (el Diamante la cambia)
    private BombPlacer bombPlacer;     // pone las burbujas (el Diamante lo desactiva)

    private Vector3Int casillaDestino; // casilla a la que va
    private Vector3Int direccion;      // dirección actual
    private float tiempoUltimaBurbuja;

    private Vector3Int[] direcciones =
        { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

    void Start()
    {
        // Si este jugador lo maneja una persona, la IA no hace nada
        if (numeroJugador <= GameSettings.HumanPlayers)
        {
            enabled = false;
            return;
        }

        rb = GetComponent<Rigidbody2D>();
        grid = FindAnyObjectByType<Grid>();

        // Los mismos tilemaps que usa Boom
        GameObject objMuros = GameObject.Find("MurosFijos");
        GameObject objObstaculos = GameObject.Find("ObstaculosDestructibles");

        if (objMuros == null || objObstaculos == null)
        {
            Debug.LogError("IA: no encuentro MurosFijos u ObstaculosDestructibles.");
            enabled = false;
            return;
        }

        muros = objMuros.GetComponent<Tilemap>();
        obstaculos = objObstaculos.GetComponent<Tilemap>();

        // La IA no usa el teclado ni el mando
        movimiento = GetComponent<PlayerMovement>();
        movimiento.enabled = false;

        PlayerInput input = GetComponent<PlayerInput>();
        if (input != null)
            input.enabled = false;

        bombPlacer = GetComponentInChildren<BombPlacer>();
        if (bombPlacer != null)
            bombPlacer.usarTeclado = false;

        // Empezar centrado en su casilla
        casillaDestino = grid.WorldToCell(rb.position);
        rb.position = Centro(casillaDestino);
    }

    void FixedUpdate()
    {
        // Moverse hacia el centro de la casilla destino
        Vector2 destino = Centro(casillaDestino);
        Vector2 nuevaPos = Vector2.MoveTowards(rb.position, destino,
                                       movimiento.speed * factorVelocidad * Time.fixedDeltaTime);
        rb.MovePosition(nuevaPos);
        rb.linearVelocity = Vector2.zero;

        // Al llegar, decide la siguiente casilla
        if (Vector2.Distance(nuevaPos, destino) < 0.01f)
        {
            Decidir();
        }
    }

    void Decidir()
    {
        Vector3Int casilla = casillaDestino;

        // 1. Huir si estoy en peligro
        if (EnPeligro(casilla))
        {
            Vector3Int huida = BuscarHuida(casilla);
            if (huida != Vector3Int.zero)
                Mover(casilla, huida);
            return;
        }

        // 2. Poner burbuja si hay un obstáculo al lado y tengo por dónde escapar
        //    (si lleva el Diamante, el BombPlacer está desactivado y no pone)
        if (bombPlacer != null && bombPlacer.enabled &&
            Time.time - tiempoUltimaBurbuja > tiempoEntreBurbujas &&
            HayObstaculoAlLado(casilla) && TieneEscapatoria(casilla))
        {
            bombPlacer.PlaceBomb();
            tiempoUltimaBurbuja = Time.time;
            return; // en la siguiente decisión verá el peligro y huirá
        }

        // 3. Seguir recto (si se puede) o girar al azar
        if (direccion != Vector3Int.zero && PuedeIr(casilla + direccion) && Random.value < 0.7f)
        {
            Mover(casilla, direccion);
            return;
        }

        List<Vector3Int> posibles = new List<Vector3Int>();
        foreach (Vector3Int d in direcciones)
        {
            if (PuedeIr(casilla + d))
                posibles.Add(d);
        }

        if (posibles.Count > 0)
            Mover(casilla, posibles[Random.Range(0, posibles.Count)]);
    }

    void Mover(Vector3Int casilla, Vector3Int dir)
    {
        direccion = dir;
        casillaDestino = casilla + dir;
    }

    // Se puede ir si la casilla está libre y no va a explotar
    bool PuedeIr(Vector3Int casilla)
    {
        return Libre(casilla) && !EnPeligro(casilla);
    }

    // ------------------ Huida ------------------

    Vector3Int BuscarHuida(Vector3Int casilla)
    {
        // Primero: una casilla vecina que ya sea segura
        foreach (Vector3Int d in direcciones)
        {
            if (PuedeIr(casilla + d))
                return d;
        }

        // Si no: la casilla libre que más me aleje de la burbuja más cercana
        Vector3Int mejor = Vector3Int.zero;
        float mejorDistancia = -1f;
        Vector3Int burbuja = BurbujaMasCercana(casilla);

        foreach (Vector3Int d in direcciones)
        {
            if (!Libre(casilla + d)) continue;

            float distancia = Vector3Int.Distance(casilla + d, burbuja);
            if (distancia > mejorDistancia)
            {
                mejorDistancia = distancia;
                mejor = d;
            }
        }
        return mejor;
    }

    // Antes de poner una burbuja comprueba que puede esconderse:
    // girar en una esquina o alejarse en línea recta más que el alcance
    bool TieneEscapatoria(Vector3Int casilla)
    {
        foreach (Vector3Int d in direcciones)
        {
            Vector3Int vecina = casilla + d;
            if (!Libre(vecina)) continue;

            // Esquina: desde la vecina se puede girar y salir de la cruz
            Vector3Int perpendicular = new Vector3Int(d.y, d.x, 0);
            if (Libre(vecina + perpendicular) || Libre(vecina - perpendicular))
                return true;

            // Línea recta: hay sitio de sobra
            bool recto = true;
            for (int i = 1; i <= alcanceExplosion + 1; i++)
            {
                if (!Libre(casilla + d * i)) { recto = false; break; }
            }
            if (recto) return true;
        }
        return false;
    }

    // ------------------ Comprobaciones ------------------

    // Misma cruz que calcula Boom: los muros fijos cortan la explosión
    // y los obstáculos se rompen pero la explosión no pasa detrás
    bool EnPeligro(Vector3Int casilla)
    {
        foreach (Boom b in FindObjectsByType<Boom>())
        {
            Vector3Int casillaBurbuja = grid.WorldToCell(b.transform.position);

            if (casillaBurbuja == casilla)
                return true;

            foreach (Vector3Int d in direcciones)
            {
                for (int i = 1; i <= alcanceExplosion; i++)
                {
                    Vector3Int c = casillaBurbuja + d * i;

                    if (muros.HasTile(c)) break;
                    if (c == casilla) return true;
                    if (obstaculos.HasTile(c)) break;
                }
            }
        }
        return false;
    }

    Vector3Int BurbujaMasCercana(Vector3Int casilla)
    {
        Vector3Int masCercana = casilla;
        float menorDistancia = Mathf.Infinity;

        foreach (Boom b in FindObjectsByType<Boom>())
        {
            Vector3Int c = grid.WorldToCell(b.transform.position);
            float distancia = Vector3Int.Distance(casilla, c);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                masCercana = c;
            }
        }
        return masCercana;
    }

    bool HayObstaculoAlLado(Vector3Int casilla)
    {
        foreach (Vector3Int d in direcciones)
        {
            if (obstaculos.HasTile(casilla + d))
                return true;
        }
        return false;
    }

    bool HayBurbuja(Vector3Int casilla)
    {
        foreach (Boom b in FindObjectsByType<Boom>())
        {
            if (grid.WorldToCell(b.transform.position) == casilla)
                return true;
        }
        return false;
    }

    bool Libre(Vector3Int casilla)
    {
        return !muros.HasTile(casilla) &&
               !obstaculos.HasTile(casilla) &&
               !HayBurbuja(casilla);
    }

    Vector2 Centro(Vector3Int casilla)
    {
        return grid.GetCellCenterWorld(casilla);
    }
}
