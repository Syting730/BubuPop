using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

// Aspecto de un obstáculo al recibir un bombardeo
// (tarjeta Lab3 - MC - Arte de obstáculos)
// Cuando una explosión rompe un obstáculo, se parte en 4 trozos que
// salen volando hacia fuera, giran, se encogen y se desvanecen.
// Usa el dibujo del propio obstáculo, así que vale para los tres
// modos (caja de madera, bloque de hielo y roca volcánica).
//
// Va en el objeto ObstaculosDestructibles, junto a DestructibleTilemap.
// DestructibleTilemap llama a Romper() justo antes de quitar la casilla.
[RequireComponent(typeof(Tilemap))]
public class EfectoRomperObstaculo : MonoBehaviour
{
    [Tooltip("Segundos que tardan los trozos en desaparecer")]
    [SerializeField, Min(0.05f)] private float duracion = 0.45f;

    [Tooltip("Velocidad a la que salen volando los trozos (casillas por segundo)")]
    [SerializeField, Min(0f)] private float velocidad = 2.5f;

    [Tooltip("Grados por segundo que giran los trozos")]
    [SerializeField] private float giro = 360f;

    private Tilemap tilemap;
    private int ordenDibujo;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();

        TilemapRenderer render = GetComponent<TilemapRenderer>();
        ordenDibujo = render != null ? render.sortingOrder + 1 : 1;
    }

    // Lo llama DestructibleTilemap antes de quitar el obstáculo
    public void Romper(Vector3Int casilla)
    {
        Sprite dibujo = tilemap.GetSprite(casilla);
        if (dibujo == null)
            return;

        Color color = tilemap.GetColor(casilla) * tilemap.color;
        Vector3 centro = tilemap.GetCellCenterWorld(casilla);
        Vector3 tamanoCasilla = tilemap.layoutGrid != null
            ? tilemap.layoutGrid.cellSize
            : Vector3.one;

        // 4 trozos: una cuarta parte del dibujo cada uno
        Rect r = dibujo.rect;
        float mitadW = r.width / 2f;
        float mitadH = r.height / 2f;

        for (int i = 0; i < 4; i++)
        {
            int dx = i % 2 == 0 ? -1 : 1;
            int dy = i < 2 ? 1 : -1;

            Rect parte = new Rect(
                r.x + (dx < 0 ? 0f : mitadW),
                r.y + (dy < 0 ? 0f : mitadH),
                mitadW, mitadH);

            Sprite trozo = Sprite.Create(dibujo.texture, parte,
                new Vector2(0.5f, 0.5f), dibujo.pixelsPerUnit);

            Vector3 desplazamiento = new Vector3(
                dx * tamanoCasilla.x / 4f, dy * tamanoCasilla.y / 4f, 0f);

            StartCoroutine(Volar(trozo, color, centro + desplazamiento,
                new Vector2(dx, dy).normalized));
        }
    }

    private IEnumerator Volar(Sprite trozo, Color color, Vector3 inicio, Vector2 direccion)
    {
        GameObject obj = new GameObject("TrozoObstaculo");
        obj.transform.position = inicio;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = trozo;
        sr.color = color;
        sr.sortingOrder = ordenDibujo;

        float sentidoGiro = Random.value < 0.5f ? -1f : 1f;
        float t = 0f;

        while (t < duracion)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duracion);

            obj.transform.position = inicio + (Vector3)(direccion * velocidad * t);
            obj.transform.rotation = Quaternion.Euler(0f, 0f, sentidoGiro * giro * t);
            obj.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.4f, p);

            Color c = color;
            c.a = color.a * (1f - p);
            sr.color = c;

            yield return null;
        }

        Destroy(obj);
        Destroy(trozo);
    }
}
