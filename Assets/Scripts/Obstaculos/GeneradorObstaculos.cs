using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Genera obstaculos destructibles al azar al empezar la partida.
// Va en el objeto ObstaculosDestructibles (el Tilemap donde se pintan).
// Sirve para los dos tableros (Tablero y TableroAD):
// - solo pinta encima del Suelo y nunca encima de un Muro Fijo
// - deja libre la zona de salida de cada jugador (4 jugadores)
// - en el modo AD deja libres las bases y la casilla del diamante
// - modo Equilibrado: cada jugador recibe los mismos obstaculos
//   cerca y lejos de su salida (para que nadie tenga ventaja)
// - no pone mas de maxEnFila obstaculos en fila ni grupos 2x2,
//   para que ningun camino quede muy cerrado
// - todos los jugadores tienen que romper los mismos obstaculos
//   para llegar al centro del tablero (donde esta el diamante)
// - modo AD: el diamante queda encerrado por un anillo de obstaculos
//   y cada jugador tiene un camino libre, igual de largo, hasta el anillo
// Los obstaculos generados se rompen y reaparecen igual que los
// pintados a mano (eso lo hace DestructibleTilemap).
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
        Aleatorio,   // cada casilla al azar (puede salir injusto)
        Equilibrado  // misma cantidad de obstaculos para cada jugador
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

    // Obstaculos del anillo del diamante (no se quitan al igualar)
    private HashSet<Vector3Int> anillo = new HashSet<Vector3Int>();

    // Start: asi los jugadores y las bases ya estan colocados
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

        // Primero el anillo, para que el resto se reparta respetandolo
        if (rodearDiamante && FindAnyObjectByType<Diamante>() != null)
            PonerAnilloDiamante();
        List<Vector3Int> candidatas = new List<Vector3Int>();

        // Recorremos todo el suelo (asi no hay que escribir los limites a mano)
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

    // ---------- Modo Aleatorio ----------

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

    // ---------- Modo Equilibrado ----------
    // 1. Cada casilla es del jugador cuya salida esta mas cerca.
    //    Si esta igual de cerca de varios, es "neutral".
    // 2. Las casillas de cada jugador se separan en anillos
    //    segun lo lejos que estan de su salida.
    // 3. En cada anillo, todos los jugadores reciben la misma
    //    cantidad de obstaculos (la que corresponde al que menos
    //    casillas tiene en ese anillo), en sitios al azar.
    // 4. Las neutrales se rellenan al azar como en modo Aleatorio.

    private int GenerarEquilibrado(List<Vector3Int> candidatas)
    {
        // grupos[anillo][jugador] = casillas
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
            // El jugador con menos casillas en este anillo marca el limite
            int menos = int.MaxValue;
            foreach (List<Vector3Int> casillas in anillo)
                menos = Mathf.Min(menos, casillas.Count);

            int cantidad = Mathf.RoundToInt(menos * probabilidad);

            foreach (List<Vector3Int> casillas in anillo)
            {
                Barajar(casillas);

                // Se van probando casillas al azar hasta poner la
                // cantidad, saltando las que bloquearian un camino
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

    // ---------- Evitar caminos bloqueados ----------

    private bool PuedePoner(Vector3Int celda)
    {
        // Obstaculos seguidos en fila y en columna contando este
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

    // ¿Completaria un cuadrado de 2x2 obstaculos?
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

    // ---------- Mismo camino al centro para todos ----------
    // Para cada jugador se busca el camino hasta el centro en el que
    // hay que romper menos obstaculos. Si a alguno le cuesta mas que
    // a otro, se quita un obstaculo de su camino, y se repite hasta
    // que a todos les cueste lo mismo.

    private int IgualarCaminos()
    {
        Vector3Int centro = CentroDelSuelo();
        int quitados = 0;

        // Limite de seguridad para no quedarse en un bucle infinito
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

                // Quitar un obstaculo al azar de su camino
                // (nunca uno del anillo del diamante)
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
                break; // volver a calcular todos los caminos
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

    // Camino en el que hay que romper menos obstaculos (los muros
    // fijos no se pueden cruzar). Devuelve los obstaculos de ese
    // camino, o null si no se puede llegar.
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

        // Casillas libres cuestan 0 y obstaculos 1: las de coste 0
        // van delante en la cola y las de coste 1 detras
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

        // Volver hacia atras apuntando los obstaculos del camino
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

    // ---------- Anillo del diamante (modo AD) ----------
    // Obstaculos en todas las casillas justo fuera de la zona libre
    // del diamante. Asi cualquier camino hacia el diamante tiene que
    // romper uno, sea cual sea el jugador.

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

    // ---------- Camino libre hasta el anillo (modo AD) ----------
    // 1. Se calcula, solo con los muros fijos, a cuantos pasos esta
    //    cada casilla del anillo.
    // 2. Desde la salida de cada jugador se baja paso a paso hasta el
    //    anillo por uno de los caminos mas cortos (elegido al azar) y
    //    se quitan los obstaculos de ese camino.
    // Como en TableroAD todos estan a la misma distancia del diamante,
    // todos llegan al anillo con los mismos pasos y sin romper nada.

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

            // Bajar hasta la casilla que toca el anillo (pasos = 1)
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

    // Pasos desde cada casilla hasta el anillo, contando solo los muros
    // fijos (los obstaculos destructibles no cuentan)
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

                // No entrar en la zona de dentro del anillo
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

    // Mezcla la lista al azar (Fisher-Yates)
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

    // ---------- Zonas libres ----------

    private HashSet<Vector3Int> CasillasQueDebenQuedarLibres()
    {
        HashSet<Vector3Int> libres = new HashSet<Vector3Int>();

        // Salida de cada jugador (los que esten en la escena, sean 2 o 4)
        foreach (PlayerMovement jugador in
                 FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
        {
            Vector3Int salida = suelo.WorldToCell(jugador.transform.position);
            salidas.Add(salida);
            AnadirRombo(libres, salida, radioSalida);
        }

        // Bases del modo AD: toda la zona que ocupan
        foreach (Base b in FindObjectsByType<Base>(FindObjectsSortMode.None))
        {
            Collider2D zona = b.GetComponent<Collider2D>();
            if (zona != null)
                AnadirZona(libres, zona.bounds);
        }

        // Diamante del modo AD: va en la casilla central del suelo
        // (mismo calculo que hace el Diamante)
        if (FindAnyObjectByType<Diamante>() != null)
        {
            BoundsInt l = suelo.cellBounds;
            Vector3Int centro = new Vector3Int(
                l.xMin + l.size.x / 2,
                l.yMin + l.size.y / 2,
                0);

            AnadirRombo(libres, centro, radioDiamante);
        }

        return libres;
    }

    // Casillas a una distancia <= radio (moviendose en horizontal y vertical)
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

    // Todas las casillas cuyo centro cae dentro de una zona
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

                // Solo se mira x e y (en 2D la z no importa)
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