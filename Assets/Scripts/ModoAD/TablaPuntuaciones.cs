using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TablaPuntuaciones : MonoBehaviour
{
    [Header("Texto de cada jugador (en orden: J1, J2, J3, J4)")]
    [SerializeField] private TMP_Text[] textosJugador =
        new TMP_Text[GameSettings.MaxPlayers];

    [Header("Texto de la tabla en la pantalla de fin (opcional)")]
    [SerializeField] private TMP_Text textoFinal;

    [Tooltip("Si se deja vacio, se busca solo en la escena")]
    [SerializeField] private PuntosAD puntos;

    [Tooltip("Modo Boss: si se deja vacio, se busca solo en la escena")]
    [SerializeField] private PuntosBoss puntosBoss;

    [Tooltip("Si se deja vacio, se busca solo en la escena")]
    [SerializeField] private PartidaManager partidaManager;

    private void Start()
    {
        if (puntos == null)
            puntos = FindAnyObjectByType<PuntosAD>();

        if (puntos == null && puntosBoss == null)
            puntosBoss = FindAnyObjectByType<PuntosBoss>();

        if (partidaManager == null)
            partidaManager = FindAnyObjectByType<PartidaManager>();

        if (puntos == null && puntosBoss == null)
        {
            Debug.LogWarning("TablaPuntuaciones: no hay PuntosAD ni PuntosBoss en la escena");
            return;
        }

        if (puntos != null)
            puntos.alCambiarPuntos.AddListener(AlCambiarPuntos);
        else
            puntosBoss.alCambiarPuntos.AddListener(AlCambiarPuntos);

        Actualizar();
    }

    private void OnDestroy()
    {
        if (puntos != null)
            puntos.alCambiarPuntos.RemoveListener(AlCambiarPuntos);

        if (puntosBoss != null)
            puntosBoss.alCambiarPuntos.RemoveListener(AlCambiarPuntos);
    }

    private void AlCambiarPuntos(int jugador, int total)
    {
        Actualizar();
    }

    public void Actualizar()
    {
        if (puntos == null && puntosBoss == null)
            return;

        int[] lista = new int[GameSettings.MaxPlayers];
        for (int i = 0; i < lista.Length; i++)
            lista[i] = puntos != null ? puntos.GetPuntos(i)
                                      : puntosBoss.GetPuntos(i);

        List<int> orden = Ordenar(lista);
        int[] puestos = Puestos(lista, orden);

        for (int k = 0; k < orden.Count; k++)
        {
            int j = orden[k];

            if (j >= textosJugador.Length || textosJugador[j] == null)
                continue;

            textosJugador[j].text =
                "<b>" + Nombre(j) + "</b>\n" +
                lista[j] + " pts\n" +
                puestos[k] + "º puesto";
        }

        if (textoFinal != null)
            textoFinal.text = CrearTexto(lista);
    }

    private string CrearTexto(int[] lista)
    {
        List<int> orden = Ordenar(lista);
        int[] puestos = Puestos(lista, orden);

        string tabla = "<b>PUNTUACIONES</b>\n";

        for (int k = 0; k < orden.Count; k++)
        {
            int j = orden[k];

            tabla += puestos[k] + ". " + Nombre(j) + "  " +
                     lista[j] + " pts\n";
        }

        return tabla;
    }

    private string Nombre(int j)
    {
        string nombre = partidaManager != null
            ? partidaManager.NombreDe(j)
            : "Jugador " + (j + 1);

        if (!GameSettings.SettingsChosen)
            return nombre;

        Color color = GameSettings.ToUnityColor(GameSettings.PlayerColors[j]);
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" +
               nombre + "</color>";
    }

    public static List<int> Ordenar(int[] lista)
    {
        List<int> orden = new List<int>();
        for (int i = 0; i < lista.Length; i++)
            orden.Add(i);

        orden.Sort((a, b) =>
        {
            if (lista[a] != lista[b])
                return lista[b].CompareTo(lista[a]);
            return a.CompareTo(b);
        });

        return orden;
    }

    public static int[] Puestos(int[] lista, List<int> orden)
    {
        int[] puestos = new int[orden.Count];

        for (int k = 0; k < orden.Count; k++)
        {
            if (k > 0 && lista[orden[k]] == lista[orden[k - 1]])
                puestos[k] = puestos[k - 1];
            else
                puestos[k] = k + 1;
        }

        return puestos;
    }
}
