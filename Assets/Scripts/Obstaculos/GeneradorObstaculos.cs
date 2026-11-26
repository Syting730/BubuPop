using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class GeneradorObstaculos : MonoBehaviour
{
    [Header("Bloque que se pinta")]
    [SerializeField] private TileBase tileObstaculo;

    [Header("Mapa (si se dejan vacios, se buscan por nombre)")]
    [SerializeField] private Tilemap suelo;
    [SerializeField] private Tilemap murosFijos;

    public enum Modo
    {
        Aleatorio,
        Equilibrado
    }

    [Header("Reparto")]
    [SerializeField] private Modo modo = Modo.Equilibrado;

    [Tooltip("Equilibrado: anchura (en casillas) de cada anillo de distancia " +
             "alrededor de la salida. En cada anillo todos reciben lo mismo")]
    [SerializeField, Min(1)] private int anchoAnillo = 2;

    [Header("Cantidad")]
    [Tooltip("Probabilidad de que una casilla libre tenga obstaculo")]
    [SerializeField, Range(0f, 1f)] private float probabilidad = 0.4f;

    [Header("Evitar caminos bloqueados")]
    [Tooltip("Maximo de obstaculos juntos en fila o en columna " +
             "(1 = nunca dos seguidos)")]
    [SerializeField, Min(1)] private int maxEnFila = 1;

    [Tooltip("Todos los jugadores tienen que romper el mismo numero " +
             "de obstaculos para llegar al centro")]
    [SerializeField] private bool igualarCaminoAlCentro = true;

    [Tooltip("No dejar grupos de 2x2 obstaculos (muy dificiles de abrir)")]
    [SerializeField] private bool evitarCuadrados = true;

    [Header("Zonas libres")]
    [Tooltip("Casillas libres alrededor de la salida de cada jugador " +
             "(2 = la casilla, sus vecinas y las vecinas de estas)")]
    [SerializeField, Min(0)] private int radioSalida = 2;

    [Tooltip("Casillas libres alrededor del diamante (modo AD)")]
    [SerializeField, Min(0)] private int radioDiamante = 1;

    [Tooltip("Modo AD: rodear la zona del diamante con un anillo de " +
             "obstaculos. Todos tienen que romperlo para llegar")]
    [SerializeField] private bool rodearDiamante = true;

    [Tooltip("Modo Boss: casillas libres alrededor del Boss")]
    [SerializeField, Min(0)] private int margenBoss = 1;

    [Tooltip("Modo AD: cada jugador tiene un camino sin obstaculos desde " +
             "su salida hasta el anillo, igual de largo para todos. " +
             "Asi lo unico que hay que romper es el anillo")]
    [SerializeField] private bool caminoLibreHastaAnillo = true;

    [Header("Otros")]
    [Tooltip("Borrar los obstaculos pintados a mano antes de generar")]
    [SerializeField] private bool borrarObstaculosPintados = false;

    [Tooltip("0 = mapa distinto cada partida. " +
             "Otro numero = siempre el mismo mapa (util para probar)")]
    [SerializeField] private int semilla = 0;

    private Tilemap obstaculos;
    private List<Vector3Int> salidas = new List<Vector3Int>();

    private HashSet<Vector3Int> anillo = new HashSet<Vector3Int>();

    private void Start()
    {
        obstaculos = GetComponent<Tilemap>();

        if (suelo == null)
            suelo = BuscarTilemap("Suelo");

        if (murosFijos == null)
            murosFijos = BuscarTilemap("MurosFijos");

        if (tileObstaculo == null || suelo == null || murosFijos == null)
        {
            Debug.LogError("GeneradorObstaculos: falta el tile, el Suelo o los MurosFijos");
            return;
        }

        if (semilla != 0)
            Random.InitState(semilla);

        if (borrarObstaculosPintados)
            obstaculos.ClearAllTiles();

        Generar();
    }

    private void Generar()
    {
        HashSet<Vector3Int> libres = CasillasQueDebenQuedarLibres();

        if (rodearDiamante && FindAnyObjectByType<Diamante>() != null)
            PonerAnilloDiamante();
        List<Vector3Int> candidatas = new List<Vector3Int>();

        BoundsInt limites = suelo.cellBounds;

        foreach (Vector3Int celda in limites.allPositionsWithin)
        {
            if (!suelo.HasTile(celda))
                continue;

            if (murosFijos.HasTile(celda))
                continue;

            if (obstaculos.HasTile(celda))
                continue;

            if (libres.Contains(celda))
                continue;

            candidatas.Add(celda);
        }

        int pintados;

        if (modo == Modo.Equilibrado && salidas.Count >= 2)
            pintados = GenerarEquilibrado(candidatas);
        else
            pintados = GenerarAleatorio(candidatas);

        if (anillo.Count > 0 && caminoLibreHastaAnillo)
            pintados -= AbrirCaminosAlAnillo();
        else if (igualarCaminoAlCentro && salidas.Count >= 2)
            pintados -= IgualarCaminos();

        Debug.Log("GeneradorObstaculos (" + modo + "): " +
                  pintados + " obstaculos generados");
    }

    private int GenerarAleatorio(List<Vector3Int> casillas)
    {
        int pintados = 0;

        foreach (Vector3Int celda in casillas)
        {
            if (Random.value < probabilidad && PuedePoner(celda))
            {
                obstaculos.SetTile(celda, tileObstaculo);
                pintados++;
            }
        }

        return pintados;
    }

    private int GenerarEquilibrado(List<Vector3Int> candidatas)
    {
        Dictionary<int, List<Vector3Int>[]> grupos =
            new Dictionary<int, List<Vector3Int>[]>();
        List<Vector3Int> neutrales = new List<Vector3Int>();

        foreach (Vector3Int celda in candidatas)
        {
            int duenio = -1;
            int mejorDistancia = int.MaxValue;
            bool empate = false;

            for (int i = 0; i < salidas.Count; i++)
            {
                int d = Distancia(celda, salidas[i]);

                if (d < mejorDistancia)
                {
                    mejorDistancia = d;
                    duenio = i;
                    empate = false;
                }
                else if (d == mejorDistancia)
                {
                    empate = true;
                }
            }

            if (empate)
            {
                neutrales.Add(celda);
                continue;
            }

            int anillo = mejorDistancia / anchoAnillo;

            if (!grupos.ContainsKey(anillo))
            {
                grupos[anillo] = new List<Vector3Int>[salidas.Count];
                for (int i = 0; i < salidas.Count; i++)
                    grupos[anillo][i] = new List<Vector3Int>();
            }

            grupos[anillo][duenio].Add(celda);
        }

        int pintados = 0;

        foreach (List<Vector3Int>[] anillo in grupos.Values)
        {
            int menos = int.MaxValue;
            foreach (List<Vector3Int> casillas in anillo)
                menos = Mathf.Min(menos, casillas.Count);

            int cantidad = Mathf.RoundToInt(menos * probabilidad);

            foreach (List<Vector3Int> casillas in anillo)
            {
                Barajar(casillas);

                int puestos = 0;

                foreach (Vector3Int celda in casillas)
                {
                    if (puestos >= cantidad)
                        break;

                    if (!PuedePoner(celda))
                        continue;

                    obstaculos.SetTile(celda, tileObstaculo);
                    puestos++;
                    pintados++;
                }
            }
        }

        pintados += GenerarAleatorio(neutrales);
        return pintados;
    }

    private bool PuedePoner(Vector3Int celda)
    {
        int fila = 1 + Seguidos(celda, Vector3Int.left) + Seguidos(celda, Vector3Int.right);
        int columna = 1 + Seguidos(celda, Vector3Int.up) + Seguidos(celda, Vector3Int.down);

        if (fila > maxEnFila || columna > maxEnFila)
            return false;

        if (evitarCuadrados && FormaCuadrado(celda))
            return false;

        return true;
    }

    private int Seguidos(Vector3Int celda, Vector3Int direccion)
    {
        int cuenta = 0;
        Vector3Int siguiente = celda + direccion;

        while (obstaculos.HasTile(siguiente))
        {
            cuenta++;
            siguiente += direccion;
        }

        return cuenta;
    }

    private bool FormaCuadrado(Vector3Int celda)
    {
        for (int dx = -1; dx <= 0; dx++)
        {
            for (int dy = -1; dy <= 0; dy++)
            {
                Vector3Int esquina = celda + new Vector3Int(dx, dy, 0);
                int ocupadas = 0;

                for (int x = 0; x <= 1; x++)
                {
                    for (int y = 0; y <= 1; y++)
                    {
                        Vector3Int c = esquina + new Vector3Int(x, y, 0);
                        if (c == celda || obstaculos.HasTile(c))
                            ocupadas++;
                    }
                }

                if (ocupadas == 4)
                    return true;
            }
        }

        return false;
    }

    private int IgualarCaminos()
    {
        Vector3Int centro = CentroDelSuelo();
        int quitados = 0;

        for (int vuelta = 0; vuelta < 200; vuelta++)
        {
            List<Vector3Int>[] caminos = new List<Vector3Int>[salidas.Count];
            int menor = int.MaxValue;

            for (int i = 0; i < salidas.Count; i++)
            {
                caminos[i] = ObstaculosEnMejorCamino(salidas[i], centro);
                if (caminos[i] != null)
                    menor = Mathf.Min(menor, caminos[i].Count);
            }

            bool todosIguales = true;

            for (int i = 0; i < salidas.Count; i++)
            {
                if (caminos[i] == null || caminos[i].Count == menor)
                    continue;

                List<Vector3Int> quitables = new List<Vector3Int>();
                foreach (Vector3Int o in caminos[i])
                {
                    if (!anillo.Contains(o))
                        quitables.Add(o);
                }

                if (quitables.Count == 0)
                    continue;

                todosIguales = false;
                Vector3Int celda = quitables[Random.Range(0, quitables.Count)];
                obstaculos.SetTile(celda, null);
                quitados++;
                break;
            }

            if (todosIguales)
            {
                Debug.Log("GeneradorObstaculos: todos rompen " + menor +
                          " obstaculo(s) para llegar al centro");
                break;
            }
        }

        return quitados;
    }

    private List<Vector3Int> ObstaculosEnMejorCamino(Vector3Int desde, Vector3Int hasta)
    {
        Dictionary<Vector3Int, int> coste = new Dictionary<Vector3Int, int>();
        Dictionary<Vector3Int, Vector3Int> anterior = new Dictionary<Vector3Int, Vector3Int>();
        LinkedList<Vector3Int> cola = new LinkedList<Vector3Int>();

        coste[desde] = 0;
        cola.AddFirst(desde);

        Vector3Int[] direcciones =
        {
            Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right
        };

        while (cola.Count > 0)
        {
            Vector3Int actual = cola.First.Value;
            cola.RemoveFirst();

            if (actual == hasta)
                break;

            foreach (Vector3Int dir in direcciones)
            {
                Vector3Int vecina = actual + dir;

                if (!suelo.HasTile(vecina) || murosFijos.HasTile(vecina))
                    continue;

                int paso = obstaculos.HasTile(vecina) ? 1 : 0;
                int nuevo = coste[actual] + paso;

                if (coste.TryGetValue(vecina, out int viejo) && viejo <= nuevo)
                    continue;

                coste[vecina] = nuevo;
                anterior[vecina] = actual;

                if (paso == 0)
                    cola.AddFirst(vecina);
                else
                    cola.AddLast(vecina);
            }
        }

        if (!coste.ContainsKey(hasta))
            return null;

        List<Vector3Int> enCamino = new List<Vector3Int>();
        Vector3Int c = hasta;

        while (c != desde)
        {
            if (obstaculos.HasTile(c))
                enCamino.Add(c);

            c = anterior[c];
        }

        return enCamino;
    }

    private void PonerAnilloDiamante()
    {
        Vector3Int centro = CentroDelSuelo();
        int distancia = radioDiamante + 1;

        for (int dx = -distancia; dx <= distancia; dx++)
        {
            for (int dy = -distancia; dy <= distancia; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) != distancia)
                    continue;

                Vector3Int celda = centro + new Vector3Int(dx, dy, 0);

                if (!suelo.HasTile(celda) || murosFijos.HasTile(celda))
                    continue;

                obstaculos.SetTile(celda, tileObstaculo);
                anillo.Add(celda);
            }
        }

        Debug.Log("GeneradorObstaculos: diamante rodeado con " +
                  anillo.Count + " obstaculos");
    }

    private int AbrirCaminosAlAnillo()
    {
        Dictionary<Vector3Int, int> pasos = PasosHastaAnillo();
        int quitados = 0;

        Vector3Int[] direcciones =
        {
            Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right
        };

        for (int i = 0; i < salidas.Count; i++)
        {
            Vector3Int actual = salidas[i];

            if (!pasos.ContainsKey(actual))
            {
                Debug.LogWarning("GeneradorObstaculos: el jugador " + (i + 1) +
                                 " no puede llegar al anillo del diamante");
                continue;
            }

            Debug.Log("GeneradorObstaculos: jugador " + (i + 1) + " esta a " +
                      pasos[actual] + " pasos del anillo");

            while (pasos[actual] > 1)
            {
                List<Vector3Int> siguientes = new List<Vector3Int>();

                foreach (Vector3Int dir in direcciones)
                {
                    Vector3Int vecina = actual + dir;
                    if (pasos.TryGetValue(vecina, out int p) && p == pasos[actual] - 1)
                        siguientes.Add(vecina);
                }

                actual = siguientes[Random.Range(0, siguientes.Count)];

                if (obstaculos.HasTile(actual))
                {
                    obstaculos.SetTile(actual, null);
                    quitados++;
                }
            }
        }

        return quitados;
    }

    private Dictionary<Vector3Int, int> PasosHastaAnillo()
    {
        Dictionary<Vector3Int, int> pasos = new Dictionary<Vector3Int, int>();
        Queue<Vector3Int> cola = new Queue<Vector3Int>();

        foreach (Vector3Int celda in anillo)
        {
            pasos[celda] = 0;
            cola.Enqueue(celda);
        }

        Vector3Int centro = CentroDelSuelo();

        while (cola.Count > 0)
        {
            Vector3Int actual = cola.Dequeue();

            foreach (Vector3Int dir in new[] { Vector3Int.up, Vector3Int.down,
                                               Vector3Int.left, Vector3Int.right })
            {
                Vector3Int vecina = actual + dir;

                if (pasos.ContainsKey(vecina))
                    continue;

                if (!suelo.HasTile(vecina) || murosFijos.HasTile(vecina))
                    continue;

                if (Distancia(vecina, centro) <= radioDiamante)
                    continue;

                pasos[vecina] = pasos[actual] + 1;
                cola.Enqueue(vecina);
            }
        }

        return pasos;
    }

    private Vector3Int CentroDelSuelo()
    {
        BoundsInt l = suelo.cellBounds;
        return new Vector3Int(l.xMin + l.size.x / 2, l.yMin + l.size.y / 2, 0);
    }

    private int Distancia(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private void Barajar(List<Vector3Int> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector3Int tmp = lista[i];
            lista[i] = lista[j];
            lista[j] = tmp;
        }
    }

    private HashSet<Vector3Int> CasillasQueDebenQuedarLibres()
    {
        HashSet<Vector3Int> libres = new HashSet<Vector3Int>();

        foreach (PlayerMovement jugador in
                 FindObjectsByType<PlayerMovement>())
        {
            Vector3Int salida = suelo.WorldToCell(jugador.transform.position);
            salidas.Add(salida);
            AnadirRombo(libres, salida, radioSalida);
        }

        foreach (Base b in FindObjectsByType<Base>())
        {
            Collider2D zona = b.GetComponent<Collider2D>();
            if (zona != null)
                AnadirZona(libres, zona.bounds);
        }

        if (FindAnyObjectByType<Diamante>() != null)
        {
            BoundsInt l = suelo.cellBounds;
            Vector3Int centro = new Vector3Int(
                l.xMin + l.size.x / 2,
                l.yMin + l.size.y / 2,
                0);

            AnadirRombo(libres, centro, radioDiamante);
        }

        // Modo Boss: el Boss y las casillas de alrededor quedan libres,
        // para que se le pueda atacar desde cualquier lado
        BossEnTablero boss = FindAnyObjectByType<BossEnTablero>();
        if (boss != null)
        {
            foreach (Vector3Int casilla in boss.CasillasOcupadas)
                AnadirRombo(libres, casilla, margenBoss);
        }

        return libres;
    }

    private void AnadirRombo(HashSet<Vector3Int> libres, Vector3Int centro, int radio)
    {
        for (int dx = -radio; dx <= radio; dx++)
        {
            for (int dy = -radio; dy <= radio; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= radio)
                    libres.Add(centro + new Vector3Int(dx, dy, 0));
            }
        }
    }

    private void AnadirZona(HashSet<Vector3Int> libres, Bounds zona)
    {
        Vector3Int min = suelo.WorldToCell(zona.min);
        Vector3Int max = suelo.WorldToCell(zona.max);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector3Int celda = new Vector3Int(x, y, 0);
                Vector3 c = suelo.GetCellCenterWorld(celda);

                if (c.x >= zona.min.x && c.x <= zona.max.x &&
                    c.y >= zona.min.y && c.y <= zona.max.y)
                {
                    libres.Add(celda);
                }
            }
        }
    }

    private Tilemap BuscarTilemap(string nombre)
    {
        GameObject obj = GameObject.Find(nombre);
        return obj != null ? obj.GetComponent<Tilemap>() : null;
    }
}