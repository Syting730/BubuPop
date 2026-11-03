using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Hace aparecer cristales pequeños en casillas libres del tablero
// (modo Asalto al Diamante). Cada cristal da 2 puntos (ver Cristal).
// Va en un objeto vacio de la escena TableroAD.
// No hace falta ningun prefab: el cristal se dibuja como un
// rombo de color creado por codigo.
public class GeneradorCristales : MonoBehaviour
{
    [Header("Mapa (si se dejan vacios, se buscan por nombre)")]
    [SerializeField] private Tilemap suelo;
    [SerializeField] private Tilemap murosFijos;
    [SerializeField] private Tilemap obstaculos;

    [Header("Aparicion")]
    [Tooltip("Segundos entre un cristal y el siguiente")]
    [SerializeField, Min(0.5f)] private float intervalo = 6f;

    [Tooltip("Cristales que puede haber a la vez en el tablero")]
    [SerializeField, Min(1)] private int maximo = 3;

    [Header("Aspecto")]
    [SerializeField] private Color color = new Color(0.55f, 0.95f, 1f);
    [Tooltip("Tamaño respecto a una casilla")]
    [SerializeField, Range(0.1f, 1f)] private float tamano = 0.35f;

    private List<Cristal> cristales = new List<Cristal>();
    private float contador;
    private Sprite sprite;

    private void Start()
    {
        if (suelo == null) suelo = Buscar("Suelo");
        if (murosFijos == null) murosFijos = Buscar("MurosFijos");
        if (obstaculos == null) obstaculos = Buscar("ObstaculosDestructibles");

        if (suelo == null)
            Debug.LogError("GeneradorCristales: no encuentro el Suelo");

        // Cuadrado blanco de 1x1 (luego se gira 45º y se pinta)
        sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            Texture2D.whiteTexture.width);

        contador = intervalo;
    }

    private void Update()
    {
        if (suelo == null)
            return;

        // Time.deltaTime vale 0 en pausa: no salen cristales
        contador -= Time.deltaTime;
        if (contador > 0f)
            return;

        contador = intervalo;

        cristales.RemoveAll(c => c == null);
        if (cristales.Count < maximo)
            CrearCristal();
    }

    private void CrearCristal()
    {
        List<Vector3Int> libres = new List<Vector3Int>();

        foreach (Vector3Int celda in suelo.cellBounds.allPositionsWithin)
        {
            if (CasillaLibre(celda))
                libres.Add(celda);
        }

        if (libres.Count == 0)
            return;

        Vector3Int elegida = libres[Random.Range(0, libres.Count)];
        Vector3 posicion = suelo.GetCellCenterWorld(elegida);
        posicion.z = 0f;

        GameObject obj = new GameObject("Cristal");
        obj.transform.position = posicion;
        obj.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

        Vector3 casilla = suelo.layoutGrid != null ? suelo.layoutGrid.cellSize : Vector3.one;
        obj.transform.localScale = new Vector3(casilla.x * tamano, casilla.y * tamano, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 5;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        Cristal cristal = obj.AddComponent<Cristal>();
        cristal.SetGenerador(this);
        cristales.Add(cristal);
    }

    // Suelo sin muro, sin obstaculo, sin cristal y sin nadie encima
    private bool CasillaLibre(Vector3Int celda)
    {
        if (!suelo.HasTile(celda))
            return false;

        if (murosFijos != null && murosFijos.HasTile(celda))
            return false;

        if (obstaculos != null && obstaculos.HasTile(celda))
            return false;

        Vector3 centro = suelo.GetCellCenterWorld(celda);
        Vector3 tam = suelo.layoutGrid != null ? suelo.layoutGrid.cellSize : Vector3.one;
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            centro, new Vector2(tam.x * 0.8f, tam.y * 0.8f), 0f);

        foreach (Collider2D hit in hits)
        {
            if (hit.isTrigger && hit.GetComponent<Base>() != null)
                continue; // en las bases si pueden salir

            return false; // jugador, bomba, diamante u otro cristal
        }

        return true;
    }

    public void CristalCogido(Cristal c)
    {
        cristales.Remove(c);
    }

    private Tilemap Buscar(string nombre)
    {
        GameObject obj = GameObject.Find(nombre);
        return obj != null ? obj.GetComponent<Tilemap>() : null;
    }
}
