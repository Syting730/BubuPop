using UnityEngine;

public class AspectoPersonaje : MonoBehaviour
{
    private const float TiempoPaso = 0.15f;

    private SpriteRenderer sr;
    private Sprite[] frente;
    private Sprite[] izquierda;
    private Sprite[] derecha;
    private Sprite[] espalda;
    private Vector3 ultimaPosicion;
    private float reloj;
    private int paso;

    public static Sprite Cargar(GameSettings.PlayerColor color, string direccion, int numero)
    {
        return Resources.Load<Sprite>("Personajes/" + color + "_" + direccion + "_" + numero);
    }

    private static Sprite[] CargarDireccion(GameSettings.PlayerColor color, string direccion)
    {
        Sprite uno = Cargar(color, direccion, 1);
        Sprite dos = Cargar(color, direccion, 2);

        if (uno == null)
            return null;

        if (dos == null)
            dos = uno;

        return new Sprite[] { uno, dos };
    }

    public void Poner(GameSettings.PlayerColor color)
    {
        sr = GetComponent<SpriteRenderer>();
        frente = CargarDireccion(color, "frente");
        izquierda = CargarDireccion(color, "izquierda");
        derecha = CargarDireccion(color, "derecha");
        espalda = CargarDireccion(color, "espalda");

        if (sr == null || frente == null)
        {
            enabled = false;
            return;
        }

        if (derecha == null)
            derecha = frente;

        if (espalda == null)
            espalda = frente;

        sr.color = Color.white;
        Mostrar(frente[0], false);
        ultimaPosicion = transform.position;
    }

    private void OnEnable()
    {
        ultimaPosicion = transform.position;
    }

    private void Update()
    {
        if (sr == null || Time.deltaTime <= 0f)
            return;

        Vector3 movimiento = transform.position - ultimaPosicion;
        ultimaPosicion = transform.position;

        float x = Mathf.Abs(movimiento.x);
        float y = Mathf.Abs(movimiento.y);

        if (x < 0.001f && y < 0.001f)
        {
            reloj = 0f;
            paso = 0;
            Mostrar(frente[0], false);
            return;
        }

        reloj += Time.deltaTime;

        if (reloj >= TiempoPaso)
        {
            reloj = 0f;
            paso = 1 - paso;
        }

        if (x > y)
        {
            if (movimiento.x > 0f)
                Mostrar(derecha[paso], false);
            else if (izquierda != null)
                Mostrar(izquierda[paso], false);
            else
                Mostrar(derecha[paso], true);
        }
        else
        {
            if (movimiento.y > 0f)
                Mostrar(espalda[paso], false);
            else
                Mostrar(frente[paso], false);
        }
    }

    private void Mostrar(Sprite dibujo, bool girar)
    {
        sr.sprite = dibujo;
        sr.flipX = girar;
    }
}
