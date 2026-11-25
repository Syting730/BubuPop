using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Temporizador : MonoBehaviour
{
    [Header("Duracion de la partida (segundos)")]
    [SerializeField, Min(1f)] private float duracion = 180f;

    [Header("Texto donde se ve el tiempo")]
    [SerializeField] private TMP_Text texto;

    [Header("Aviso de que se acaba el tiempo")]
    [SerializeField, Min(0)] private int segundosAviso = 10;
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorAviso = new Color(1f, 0.25f, 0.25f);

    [Header("Fin de partida")]
    [Tooltip("Si se deja vacio, se busca solo en la escena")]
    [SerializeField] private PartidaManager partidaManager;

    [Tooltip("Para otros scripts (por ejemplo, los puntos). " +
             "Se llama una vez, cuando el tiempo llega a 0")]
    public UnityEvent alAcabarElTiempo;

    private float tiempoRestante;
    private bool terminado = false;
    private Vector3 escalaOriginal;

    public float TiempoRestante
    {
        get { return tiempoRestante; }
    }

    public bool Terminado
    {
        get { return terminado; }
    }

    private void Start()
    {
        tiempoRestante = duracion;

        if (partidaManager == null)
            partidaManager = FindAnyObjectByType<PartidaManager>();

        if (texto != null)
            escalaOriginal = texto.transform.localScale;

        MostrarTiempo();
    }

    private void Update()
    {
        if (terminado)
            return;

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            MostrarTiempo();
            TiempoAgotado();
            return;
        }

        MostrarTiempo();
    }

    private void MostrarTiempo()
    {
        if (texto == null)
            return;

        int segundos = Mathf.CeilToInt(tiempoRestante);
        int minutos = segundos / 60;
        segundos = segundos % 60;

        texto.text = minutos + ":" + segundos.ToString("00");

        bool aviso = tiempoRestante <= segundosAviso && tiempoRestante > 0f;
        texto.color = aviso ? colorAviso : colorNormal;

        if (aviso)
        {
            float latido = 1f + 0.15f * Mathf.Abs(Mathf.Sin(tiempoRestante * Mathf.PI));
            texto.transform.localScale = escalaOriginal * latido;
        }
        else
        {
            texto.transform.localScale = escalaOriginal;
        }
    }

    private void TiempoAgotado()
    {
        terminado = true;

        Debug.Log("¡Se acabo el tiempo!");

        alAcabarElTiempo.Invoke();

        if (partidaManager != null)
            partidaManager.TerminarPorTiempo();
        else
            Debug.LogWarning("Temporizador: no hay PartidaManager en la escena");
    }
}
