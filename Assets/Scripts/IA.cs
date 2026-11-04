using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// IA de BubuPop. Se mueve de casilla en casilla y, al llegar al centro
// de cada una, decide qué hacer (por orden de prioridad):
//   1. Huir si está dentro del alcance de una burbuja
//   2. Atacar: si hay un enemigo en su cruz de explosión, pone burbuja
//   3. Si lleva el diamante -> volver a su base
//   4. Si otro lleva el diamante -> cazarle: emboscada en su base
//      (si llega antes que él) o perseguirle, atacando más a menudo
//   4b. Si no, ir a por un power-up o un cristal cercano
//   5. Ir a por su objetivo:
//        - Modo Clásico: ponerse a tiro del enemigo más cercano
//          (prefiere a los jugadores humanos)
//        - Modo Diamante: el diamante, o ponerse a tiro de quien lo lleve
//      Si hay obstáculos en medio, los rompe con burbujas
//   6. Si no tiene nada que hacer, pasea sin dar media vuelta
// Los demás jugadores cuentan como casillas ocupadas: no intenta
// atravesarlos (así no se quedan empujándose).
// Se activa sola en los jugadores que no son personas (GameSettings.HumanPlayers).
public class IA : MonoBehaviour
{
    [Tooltip("1 = J1, 2 = J2, 3 = J3, 4 = J4")]
    public int numeroJugador = 1;

    [Header("Burbujas")]
    [Tooltip("Tiene que ser igual que Explosion Range del prefab Bomb (Boom)")]
    public int alcanceExplosion = 2;
    public float tiempoEntreBurbujas = 2f;
    [Tooltip("Modo Diamante: tiempo entre burbujas cuando otro lleva el diamante (más agresiva)")]
    public float tiempoEntreBurbujasCaza = 1f;
    [Tooltip("Tiene que ser igual que Explosion Delay del prefab Bomb (Boom)")]
    public float tiempoExplosion = 2f;

    [Header("Movimiento")]
    [Tooltip("1 = igual que un jugador, 0.5 = la mitad de rápido. " +
             "Menos de 0.5 puede no darle tiempo a escapar de sus burbujas")]
    [Range(0.1f, 1f)]
    public float factorVelocidad = 0.6f;

    [Header("Objetivos")]
    [Tooltip("Distancia máxima (en casillas) para desviarse a por un power-up o cristal")]
    public int distanciaPowerUp = 5;
    [Tooltip("Cuántas casillas 'más cerca' le parece un humano que una IA. " +
             "0 = le da igual, más alto = va más a por los humanos")]
    public int preferenciaHumanos = 6;

    [Header("Depuración (solo lectura)")]
    [Tooltip("Qué está haciendo la IA ahora mismo. Míralo en el Inspector durante el Play")]
    public string estado;

    private Rigidbody2D rb;
    private Grid grid;
    private Tilemap muros;
    private Tilemap obstaculos;
    private PlayerMovement movimiento; // de aqui saca la velocidad (el Diamante la cambia)
    private BombPlacer bombPlacer;     // pone las burbujas (el Diamante lo desactiva)
    private Diamante diamante;         // null en el modo Clásico

    private Vector3Int casillaDestino;  // casilla a la que va
    private Vector3Int direccion;       // dirección actual
    private float tiempoUltimaBurbuja = -999f;
    private float tiempoAtascado;
    private float tiempoEsperando;

    // Casilla en la que se atascó: la evita durante un rato
    private Vector3Int casillaBloqueada;
    private float finBloqueo;

    // Las otras IA miran esto para no elegir la misma casilla (reserva)
    public Vector3Int CasillaDestino
    {
        get { return casillaDestino; }
    }

    // Datos que se recalculan en cada decisión
    private HashSet<Vector3Int> peligro = new HashSet<Vector3Int>();          // casillas que van a explotar
    private HashSet<Vector3Int> casillasBurbujas = new HashSet<Vector3Int>(); // casillas con burbuja
    private HashSet<Vector3Int> ocupadas = new HashSet<Vector3Int>();         // casillas con otro jugador
    private Dictionary<Vector3Int, int> distancia = new Dictionary<Vector3Int, int>();               // pasos hasta cada casilla
    private Dictionary<Vector3Int, Vector3Int> primerPaso = new Dictionary<Vector3Int, Vector3Int>(); // por dónde salir para llegar

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
        diamante = FindAnyObjectByType<Diamante>();

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

        PlayerGlovePowerUp guante = GetComponentInChildren<PlayerGlovePowerUp>();
        if (guante != null)
            guante.usarTeclado = false;

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

        if (Vector2.Distance(nuevaPos, destino) < 0.01f)
        {
            // Ha llegado: decide la siguiente casilla
            tiempoAtascado = 0f;
            Decidir();
        }
        else
        {
            // Si tarda demasiado es que algo le bloquea (otro jugador):
            // apunta esa casilla para evitarla 1,5 s, vuelve al centro
            // de la casilla en la que está y decide otra vez
            tiempoAtascado += Time.fixedDeltaTime;
            if (tiempoAtascado > 0.8f)
            {
                tiempoAtascado = 0f;
                casillaBloqueada = casillaDestino;
                finBloqueo = Time.time + 1.5f;
                casillaDestino = grid.WorldToCell(rb.position);
            }
        }
    }

    // ------------------ Decisión ------------------

    void Decidir()
    {
        Vector3Int casilla = casillaDestino;
        ActualizarMapa();

        // 1. Huir
        if (peligro.Contains(casilla))
        {
            estado = "Huyendo";
            Huir(casilla);
            return;
        }

        // Calcula a qué casillas puede llegar sin pasar por peligro
        Explorar(casilla, true);

        bool hayPortadorEnemigo = diamante != null && diamante.Portador != null &&
                                  diamante.Portador != gameObject;

        // Cuando otro lleva el diamante, ataca más a menudo
        float espera = hayPortadorEnemigo ? tiempoEntreBurbujasCaza : tiempoEntreBurbujas;

        bool puedePoner = bombPlacer != null && bombPlacer.enabled &&
                          Time.time - tiempoUltimaBurbuja > espera;

        // 2. Atacar: hay un enemigo dentro de la cruz de explosión
        if (puedePoner && HayEnemigoEnCruz(casilla) && TieneEscapatoria(casilla))
        {
            estado = "Atacando";
            PonerBurbuja();
            return;
        }

        if (diamante != null && diamante.Portador == gameObject)
        {
            // 3. Llevo el diamante: a mi base
            estado = "Llevando el diamante a la base";
            if (IrAlMasCercano(CasillasMiBase(), 100))
                return;
        }
        else if (hayPortadorEnemigo)
        {
            // 4. Otro lleva el diamante: cazarle (sin distraerse con power-ups)
            if (CazarPortador(casilla, puedePoner))
                return;
        }
        else
        {
            // 4. Power-up o cristal cercano
            estado = "Yendo a por un power-up o cristal";
            if (IrAlMasCercano(CasillasRecogibles(), distanciaPowerUp))
                return;

            // 5. Objetivo
            Vector3Int meta;
            bool metaEsJugador;
            if (BuscarObjetivo(out meta, out metaEsJugador))
            {
                // A un jugador no hace falta tocarlo: basta con ponerse
                // en una casilla donde la explosión le alcance
                List<Vector3Int> destinos = metaEsJugador
                    ? CasillasDeAtaque(meta)
                    : new List<Vector3Int> { meta };

                if (metaEsJugador && destinos.Contains(casilla))
                {
                    // Ya está a tiro pero aún no puede poner: espera un poco
                    estado = "Esperando para atacar";
                    if (Esperar()) return;
                }
                else
                {
                    estado = metaEsJugador ? "Persiguiendo a un enemigo" : "Yendo a por el diamante";
                    if (IrAlMasCercano(destinos, 100))
                        return;

                    // No se puede llegar: romper obstáculos en dirección a la meta
                    Vector3Int sitio;
                    if (BuscarSitioParaRomper(meta, out sitio))
                    {
                        if (sitio != casilla)
                        {
                            estado = "Yendo a romper un obstáculo";
                            Mover(casilla, primerPaso[sitio]);
                            return;
                        }

                        if (puedePoner && TieneEscapatoria(casilla))
                        {
                            estado = "Rompiendo obstáculo";
                            PonerBurbuja();
                            return;
                        }

                        estado = MotivoParaNoPoner(puedePoner);
                        if (Esperar()) return;
                    }
                }
            }
        }

        // 6. Pasear
        estado = "Paseando";
        Pasear(casilla);
    }

    // Devuelve true mientras deba seguir esperando (máximo 'maximo' segundos)
    bool Esperar(float maximo = 2f)
    {
        tiempoEsperando += Time.fixedDeltaTime;
        return tiempoEsperando < maximo;
    }

    // ------------------ Cazar al portador del diamante ------------------

    // Camino más corto del portador hasta su base (búsqueda en anchura).
    // Devuelve las casillas por las que pasará, en orden, sin contar la base.
    List<Vector3Int> CaminoDelPortador(Vector3Int desde, List<Vector3Int> baseRival)
    {
        List<Vector3Int> camino = new List<Vector3Int>();
        HashSet<Vector3Int> metas = new HashSet<Vector3Int>(baseRival);
        if (metas.Count == 0 || metas.Contains(desde))
            return camino;

        // Para cada casilla: de qué casilla venía
        Dictionary<Vector3Int, Vector3Int> padre = new Dictionary<Vector3Int, Vector3Int>();
        Queue<Vector3Int> cola = new Queue<Vector3Int>();
        cola.Enqueue(desde);
        padre[desde] = desde;

        Vector3Int llegada = desde;
        bool encontrado = false;

        while (cola.Count > 0)
        {
            Vector3Int actual = cola.Dequeue();
            if (metas.Contains(actual))
            {
                llegada = actual;
                encontrado = true;
                break;
            }

            foreach (Vector3Int d in direcciones)
            {
                Vector3Int vecina = actual + d;
                if (padre.ContainsKey(vecina)) continue;
                if (muros.HasTile(vecina) || obstaculos.HasTile(vecina) ||
                    casillasBurbujas.Contains(vecina)) continue;

                padre[vecina] = actual;
                cola.Enqueue(vecina);
            }
        }

        if (!encontrado)
            return camino;

        // Volver hacia atrás desde la base hasta el portador
        Vector3Int c = padre[llegada];
        while (c != desde)
        {
            camino.Insert(0, c);
            c = padre[c];
        }
        return camino;
    }

    // 1. Calcula por dónde va a ir el portador hasta su base.
    // 2. Trampa: si ese camino pasa por mi cruz justo cuando la burbuja
    //    va a explotar, la pone ya (sin esperar a tenerle a tiro).
    // 3. Emboscada: si puede llegar antes que él a algún punto de su camino,
    //    se coloca ahí y le espera.
    // 4. Si no, le persigue para ponerse a tiro.
    bool CazarPortador(Vector3Int casilla, bool puedePoner)
    {
        GameObject portador = diamante.Portador;
        Vector3Int casillaPortador = grid.WorldToCell(portador.transform.position);
        List<Vector3Int> baseRival = CasillasBaseDe(diamante.IndicePortador);
        List<Vector3Int> camino = CaminoDelPortador(casillaPortador, baseRival);

        // Velocidades en casillas por segundo (el portador va ralentizado por el Diamante)
        float miVelocidad = movimiento.speed * factorVelocidad;
        float suVelocidad = portador.GetComponent<PlayerMovement>().speed;
        IA iaPortador = portador.GetComponent<IA>();
        if (iaPortador != null && iaPortador.enabled)
            suVelocidad *= iaPortador.factorVelocidad;

        // Casillas que avanza el portador mientras la burbuja explota
        float pasosHastaExplosion = suVelocidad * tiempoExplosion;

        // --- Trampa ---
        if (puedePoner && camino.Count > 0)
        {
            HashSet<Vector3Int> miCruz = new HashSet<Vector3Int>();
            AñadirCruz(casilla, miCruz);

            for (int i = 0; i < camino.Count; i++)
            {
                int pasos = i + 1; // pasos que le faltan al portador para llegar a camino[i]
                bool llegaAlExplotar = Mathf.Abs(pasos - pasosHastaExplosion) <= 1.5f;

                if (llegaAlExplotar && miCruz.Contains(camino[i]) && TieneEscapatoria(casilla))
                {
                    estado = "Trampa en el camino del portador";
                    PonerBurbuja();
                    return true;
                }
            }
        }

        // --- Emboscada ---
        // Casillas (fuera de su base) desde las que alcanzo su camino
        // y a las que llego antes que él
        HashSet<Vector3Int> emboscada = new HashSet<Vector3Int>();
        for (int i = 0; i < camino.Count; i++)
        {
            float suTiempo = (i + 1) / suVelocidad;

            foreach (Vector3Int a in CasillasDeAtaque(camino[i]))
            {
                if (baseRival.Contains(a)) continue;

                int d;
                if (!distancia.TryGetValue(a, out d)) continue;

                float miTiempo = d / miVelocidad;
                if (miTiempo + 0.3f < suTiempo)
                    emboscada.Add(a);
            }
        }

        if (emboscada.Count > 0)
        {
            if (emboscada.Contains(casilla))
            {
                estado = "Emboscada: esperando al portador";
                return Esperar(6f);
            }

            estado = "Emboscada: yendo a cortarle el paso";
            if (IrAlMasCercano(new List<Vector3Int>(emboscada), 100))
                return true;
        }

        // --- Persecución ---
        estado = "Persiguiendo al portador";
        List<Vector3Int> destinos = CasillasDeAtaque(casillaPortador);

        if (destinos.Contains(casilla))
            return Esperar(2f); // a tiro: el paso 2 (Atacar) pondrá la burbuja

        if (IrAlMasCercano(destinos, 100))
            return true;

        // No se puede llegar: romper obstáculos hacia el portador
        Vector3Int sitio;
        if (BuscarSitioParaRomper(casillaPortador, out sitio))
        {
            estado = "Abriendo camino hacia el portador";

            if (sitio != casilla)
            {
                Mover(casilla, primerPaso[sitio]);
                return true;
            }

            if (puedePoner && TieneEscapatoria(casilla))
            {
                PonerBurbuja();
                return true;
            }
        }
        return false;
    }

    string MotivoParaNoPoner(bool puedePoner)
    {
        if (bombPlacer == null) return "Esperando: no tiene BombPlacer";
        if (!bombPlacer.enabled) return "Esperando: BombPlacer desactivado";
        if (!puedePoner) return "Esperando: tiempo entre burbujas";
        return "Esperando: no tiene escapatoria";
    }

    void Mover(Vector3Int casilla, Vector3Int dir)
    {
        direccion = dir;
        casillaDestino = casilla + dir;
        tiempoAtascado = 0f;
        tiempoEsperando = 0f;
    }

    void PonerBurbuja()
    {
        bombPlacer.PlaceBomb();
        tiempoUltimaBurbuja = Time.time;
        // En la siguiente decisión verá el peligro y huirá
    }

    // ------------------ Búsqueda de caminos (BFS) ------------------

    // Búsqueda en anchura: recorre las casillas por orden de distancia
    // (primero las de 1 paso, luego las de 2...) y apunta para cada una
    // cuántos pasos hay y por qué dirección hay que salir para llegar.
    void Explorar(Vector3Int inicio, bool evitarPeligro)
    {
        distancia.Clear();
        primerPaso.Clear();

        Queue<Vector3Int> cola = new Queue<Vector3Int>();
        cola.Enqueue(inicio);
        distancia[inicio] = 0;

        while (cola.Count > 0)
        {
            Vector3Int actual = cola.Dequeue();

            foreach (Vector3Int d in direcciones)
            {
                Vector3Int vecina = actual + d;

                if (distancia.ContainsKey(vecina)) continue;          // ya visitada
                if (!Libre(vecina)) continue;                         // muro, obstáculo, burbuja o jugador
                if (evitarPeligro && peligro.Contains(vecina)) continue;

                distancia[vecina] = distancia[actual] + 1;
                primerPaso[vecina] = (actual == inicio) ? d : primerPaso[actual];
                cola.Enqueue(vecina);
            }
        }
    }

    // Va hacia la casilla más cercana de la lista (si se puede llegar)
    bool IrAlMasCercano(List<Vector3Int> objetivos, int distanciaMax)
    {
        Vector3Int mejor = Vector3Int.zero;
        int mejorDistancia = int.MaxValue;

        foreach (Vector3Int o in objetivos)
        {
            int d;
            if (distancia.TryGetValue(o, out d) && d > 0 &&
                d <= distanciaMax && d < mejorDistancia)
            {
                mejorDistancia = d;
                mejor = o;
            }
        }

        if (mejorDistancia == int.MaxValue)
            return false;

        Mover(casillaDestino, primerPaso[mejor]);
        return true;
    }

    // ------------------ Huida ------------------

    void Huir(Vector3Int casilla)
    {
        // Para salir puede cruzar casillas peligrosas
        Explorar(casilla, false);

        // Busca la casilla segura más cercana
        Vector3Int mejor = Vector3Int.zero;
        int mejorDistancia = int.MaxValue;

        foreach (KeyValuePair<Vector3Int, int> par in distancia)
        {
            if (par.Value > 0 && !peligro.Contains(par.Key) && par.Value < mejorDistancia)
            {
                mejorDistancia = par.Value;
                mejor = par.Key;
            }
        }

        if (mejorDistancia != int.MaxValue)
            Mover(casilla, primerPaso[mejor]);
    }

    // Simula una burbuja en mi casilla: ¿llego a tiempo a una casilla segura?
    bool TieneEscapatoria(Vector3Int casilla)
    {
        HashSet<Vector3Int> simulado = new HashSet<Vector3Int>(peligro);
        AñadirCruz(casilla, simulado);

        foreach (KeyValuePair<Vector3Int, int> par in distancia)
        {
            if (par.Value > 0 && par.Value <= alcanceExplosion + 2 &&
                !simulado.Contains(par.Key))
                return true;
        }
        return false;
    }

    // ------------------ Objetivos ------------------

    bool BuscarObjetivo(out Vector3Int meta, out bool metaEsJugador)
    {
        meta = Vector3Int.zero;
        metaEsJugador = false;

        // Modo Diamante: el diamante si está libre, o cazar a quien lo lleve
        if (diamante != null)
        {
            if (diamante.Portador == null)
            {
                meta = grid.WorldToCell(diamante.transform.position);
            }
            else
            {
                meta = grid.WorldToCell(diamante.Portador.transform.position);
                metaEsJugador = true;
            }
            return true;
        }

        // Modo Clásico: el enemigo más cercano, prefiriendo a los humanos
        int mejorPuntuacion = int.MaxValue;
        foreach (PlayerMovement enemigo in Enemigos())
        {
            Vector3Int c = grid.WorldToCell(enemigo.transform.position);

            // Distancia hasta ponerse a tiro (o en línea recta si no se puede llegar)
            int d = 1000 + Manhattan(casillaDestino, c);
            foreach (Vector3Int ataque in CasillasDeAtaque(c))
            {
                int pasos;
                if (distancia.TryGetValue(ataque, out pasos) && pasos < d)
                    d = pasos;
            }

            if (EsHumano(enemigo))
                d -= preferenciaHumanos;

            if (d < mejorPuntuacion)
            {
                mejorPuntuacion = d;
                meta = c;
                metaEsJugador = true;
            }
        }
        return metaEsJugador;
    }

    // Casillas desde las que una burbuja alcanzaría a la casilla 'objetivo'
    // (las de su cruz, sin contar la suya)
    List<Vector3Int> CasillasDeAtaque(Vector3Int objetivo)
    {
        HashSet<Vector3Int> cruz = new HashSet<Vector3Int>();
        AñadirCruz(objetivo, cruz);
        cruz.Remove(objetivo);
        return new List<Vector3Int>(cruz);
    }

    // Si no puede llegar a la meta, busca una casilla a la que sí puede llegar,
    // pegada a un obstáculo, que esté lo más cerca posible de la meta
    bool BuscarSitioParaRomper(Vector3Int meta, out Vector3Int sitio)
    {
        sitio = Vector3Int.zero;
        int mejorPuntuacion = int.MaxValue;

        foreach (KeyValuePair<Vector3Int, int> par in distancia)
        {
            foreach (Vector3Int d in direcciones)
            {
                Vector3Int obstaculo = par.Key + d;
                if (!obstaculos.HasTile(obstaculo)) continue;

                // Cuanto más cerca de la meta esté el obstáculo y menos haya que andar, mejor
                int puntuacion = Manhattan(obstaculo, meta) * 2 + par.Value;
                if (puntuacion < mejorPuntuacion)
                {
                    mejorPuntuacion = puntuacion;
                    sitio = par.Key;
                }
            }
        }
        return mejorPuntuacion != int.MaxValue;
    }

    void Pasear(Vector3Int casilla)
    {
        // Direcciones posibles sin dar media vuelta
        List<Vector3Int> posibles = new List<Vector3Int>();
        foreach (Vector3Int d in direcciones)
        {
            if (d == -direccion) continue;
            if (Libre(casilla + d) && !peligro.Contains(casilla + d))
                posibles.Add(d);
        }

        // Callejón sin salida: entonces sí da media vuelta
        if (posibles.Count == 0 && direccion != Vector3Int.zero &&
            Libre(casilla - direccion) && !peligro.Contains(casilla - direccion))
            posibles.Add(-direccion);

        if (posibles.Count == 0)
            return;

        // Prefiere seguir recto
        if (posibles.Contains(direccion) && Random.value < 0.7f)
            Mover(casilla, direccion);
        else
            Mover(casilla, posibles[Random.Range(0, posibles.Count)]);
    }

    // ------------------ Jugadores ------------------

    // Los demás jugadores que siguen vivos
    List<PlayerMovement> Enemigos()
    {
        List<PlayerMovement> lista = new List<PlayerMovement>();

        foreach (PlayerMovement p in FindObjectsByType<PlayerMovement>())
        {
            if (p.gameObject == gameObject) continue;

            PlayerDamage vida = p.GetComponentInChildren<PlayerDamage>();
            if (vida != null && vida.IsDead()) continue;

            lista.Add(p);
        }
        return lista;
    }

    // Un jugador es humano si no tiene IA o si su IA está apagada
    bool EsHumano(PlayerMovement jugador)
    {
        IA otraIA = jugador.GetComponent<IA>();
        return otraIA == null || !otraIA.enabled;
    }

    bool HayEnemigoEnCruz(Vector3Int casilla)
    {
        HashSet<Vector3Int> cruz = new HashSet<Vector3Int>();
        AñadirCruz(casilla, cruz);

        foreach (PlayerMovement e in Enemigos())
        {
            if (cruz.Contains(grid.WorldToCell(e.transform.position)))
                return true;
        }
        return false;
    }

    // ------------------ Listas de casillas ------------------

    // Power-ups y cristales (todo lo que se recoge al pasar por encima)
    List<Vector3Int> CasillasRecogibles()
    {
        List<Vector3Int> lista = new List<Vector3Int>();

        foreach (PowerUpPickup p in FindObjectsByType<PowerUpPickup>())
            lista.Add(grid.WorldToCell(p.transform.position));

        foreach (Cristal c in FindObjectsByType<Cristal>())
            lista.Add(grid.WorldToCell(c.transform.position));

        return lista;
    }

    List<Vector3Int> CasillasMiBase()
    {
        return CasillasBaseDe(numeroJugador - 1);
    }

    // Todas las casillas que ocupa la base del jugador 'indice' (0 = J1)
    List<Vector3Int> CasillasBaseDe(int indice)
    {
        List<Vector3Int> lista = new List<Vector3Int>();

        foreach (Base b in FindObjectsByType<Base>())
        {
            if (b.Jugador != indice) continue;

            Collider2D zona = b.GetComponent<Collider2D>();
            if (zona == null)
            {
                lista.Add(grid.WorldToCell(b.transform.position));
                continue;
            }

            Vector3Int min = grid.WorldToCell(zona.bounds.min);
            Vector3Int max = grid.WorldToCell(zona.bounds.max);

            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    Vector3Int c = new Vector3Int(x, y, 0);
                    if (zona.OverlapPoint(Centro(c)))
                        lista.Add(c);
                }
            }
        }
        return lista;
    }

    // ------------------ Mapa: peligro, burbujas y jugadores ------------------

    void ActualizarMapa()
    {
        peligro.Clear();
        casillasBurbujas.Clear();
        ocupadas.Clear();

        foreach (Boom b in FindObjectsByType<Boom>())
        {
            Vector3Int c = grid.WorldToCell(b.transform.position);
            casillasBurbujas.Add(c);
            AñadirCruz(c, peligro);
        }

        foreach (PlayerMovement p in Enemigos())
            ocupadas.Add(grid.WorldToCell(p.transform.position));

        // Casillas reservadas por las otras IA (a las que van ahora mismo)
        foreach (IA otra in FindObjectsByType<IA>())
        {
            if (otra != this && otra.enabled)
                ocupadas.Add(otra.CasillaDestino);
        }

        // Casilla donde se atascó hace poco
        if (Time.time < finBloqueo)
            ocupadas.Add(casillaBloqueada);
    }

    // Misma cruz que calcula Boom: los muros fijos cortan la explosión
    // y los obstáculos se rompen pero la explosión no pasa detrás
    void AñadirCruz(Vector3Int origen, HashSet<Vector3Int> casillas)
    {
        casillas.Add(origen);

        foreach (Vector3Int d in direcciones)
        {
            for (int i = 1; i <= alcanceExplosion; i++)
            {
                Vector3Int c = origen + d * i;
                if (muros.HasTile(c)) break;
                casillas.Add(c);
                if (obstaculos.HasTile(c)) break;
            }
        }
    }

    // ------------------ Utilidades ------------------

    bool Libre(Vector3Int casilla)
    {
        return !muros.HasTile(casilla) &&
               !obstaculos.HasTile(casilla) &&
               !casillasBurbujas.Contains(casilla) &&
               !ocupadas.Contains(casilla);
    }

    int Manhattan(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    Vector2 Centro(Vector3Int casilla)
    {
        return grid.GetCellCenterWorld(casilla);
    }
}
