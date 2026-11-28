using UnityEngine;

public class AspectoPersonaje : MonoBehaviour
{
    private const float TiempoPaso = 0.15f;
    private const float TiempoParaQuieto = 0.12f;
    private const float MovimientoMinimo = 0.0005f;

    private SpriteRenderer sr;
    private Sprite[] frente;
    private Sprite[] izquierda;
    private Sprite[] derecha;
    private Sprite[] espalda;
    private Vector3 ultimaPosicion;
    private Vector2 ultimaDireccion;
    private float tiempoQuieto = 1f;
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
        Sprite tres = Cargar(color, direccion, 3);

        if (uno == null)
            return null;

        if (dos == null)
            dos = uno;

        if (tres == null)
            tres = dos;

        return new Sprite[] { uno, dos, uno, tres };
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
        tiempoQuieto = 1f;
    }

    private void OnEnable()
    {
        ultimaPosicion = transform.position;
        tiempoQuieto = 1f;
    }

    private void LateUpdate()
    {
        if (sr == null || Time.deltaTime <= 0f)
            return;

        Vector2 movimiento = transform.position - ultimaPosicion;
        ultimaPosicion = transform.position;

        if (movimiento.magnitude > MovimientoMinimo)
        {
            if (tiempoQuieto >= TiempoParaQuieto)
            {
                reloj = 0f;
                paso = 1;
            }

            tiempoQuieto = 0f;

            if (Mathf.Abs(movimiento.x) > Mathf.Abs(movimiento.y) * 1.5f)
                ultimaDireccion = new Vector2(Mathf.Sign(movimiento.x), 0f);
            else if (Mathf.Abs(movimiento.y) > Mathf.Abs(movimiento.x) * 1.5f)
                ultimaDireccion = new Vector2(0f, Mathf.Sign(movimiento.y));
            else if (ultimaDireccion == Vector2.zero)
                ultimaDireccion = new Vector2(0f, Mathf.Sign(movimiento.y));
        }
        else
        {
            tiempoQuieto += Time.deltaTime;
        }

        if (tiempoQuieto >= TiempoParaQuieto)
        {
            reloj = 0f;
            paso = 0;
            Mostrar(frente[0], false);
            return;
        }

        reloj += Time.deltaTime;

        if (reloj >= TiempoPaso)
        {
            reloj -= TiempoPaso;
            paso = (paso + 1) % 4;
        }

        if (ultimaDireccion.x > 0f)
            Mostrar(derecha[paso], false);
        else if (ultimaDireccion.x < 0f)
        {
            if (izquierda != null)
                Mostrar(izquierda[paso], false);
            else
                Mostrar(derecha[paso], true);
        }
        else if (ultimaDireccion.y > 0f)
            Mostrar(espalda[paso], false);
        else
            Mostrar(frente[paso], false);
    }

    private void Mostrar(Sprite dibujo, bool girar)
    {
        sr.sprite = dibujo;
        sr.flipX = girar;
    }
}
