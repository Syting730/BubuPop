using UnityEngine;

// Efecto visual de una casilla de fuego (modo Boss Volcán).
// Va en el prefab del fuego. Solo es visual: el daño lo hace Fuego.cs.
// La llama cambia de color entre rojo, naranja y amarillo y "late" un poco,
// para que se distinga del aviso rojo del suelo.
public class LlamaFuego : MonoBehaviour
{
    [Header("Colores")]
    private Color colorA = new Color(1f, 0.25f, 0f);  // rojo anaranjado
    private Color colorB = new Color(1f, 0.85f, 0.1f); // amarillo

    // Cambia de color
    private float velocidad = 8f;
    [Tooltip("Cuánto crece y encoge (0.1 = un 10 %)")]
    [SerializeField, Range(0f, 0.3f)] private float latido = 0.1f;

    private SpriteRenderer sprite;
    private Vector3 escalaInicial;
    private float desfase;

    private void Start()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        escalaInicial = transform.localScale;

        // Cada casilla empieza en un momento distinto, así no parpadean todas a la vez
        desfase = Random.Range(0f, 10f);
    }

    private void Update()
    {
        // Valor que va de 0 a 1 y vuelve, una y otra vez
        float t = (Mathf.Sin((Time.time + desfase) * velocidad) + 1f) / 2f;

        if (sprite != null)
            sprite.color = Color.Lerp(colorA, colorB, t);

        transform.localScale = escalaInicial * (1f + latido * t);
    }
}
