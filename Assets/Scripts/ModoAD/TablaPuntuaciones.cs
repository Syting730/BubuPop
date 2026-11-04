using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Tabla de puntuaciones del modo Asalto al Diamante:
// - cada jugador ve sus puntos y su puesto en su zona de la pantalla
//   (debajo o encima de sus corazones)
// - los puestos se calculan ordenando de mas a menos puntos;
//   si dos jugadores tienen los mismos puntos, comparten puesto
// - se actualiza sola cada vez que alguien gana puntos (PuntosAD)
// - opcional: la tabla completa ordenada en la pantalla de fin
public class TablaPuntuaciones : MonoBehaviour
{
    [Header("Texto de cada jugador (en orden: J1, J2, J3, J4)")]
    [SerializeField] private TMP_Text[] textosJugador =
        new TMP_Text[GameSettings.MaxPlayers];

    [Header("Texto de la tabla en la pantalla de fin (opcional)")]
    [SerializeField] private TMP_Text textoFinal;

    [Tooltip("Si se deja vacio, se busca solo en la escena")]
    [SerializeField] private PuntosAD puntos;

    [Tooltip("Si se deja vacio, se busca solo en la escena")]
    [SerializeField] private PartidaManager partidaManager;

    // ---------- Inicio ----------

    private void Start()
    {
        if (puntos == null)
            puntos = FindAnyObjectByType<PuntosAD>();

        if (partidaManager == null)
            partidaManager = FindAnyObjectByType<PartidaManager>();

        if (puntos == null)
        {
            Debug.LogWarning("TablaPuntuaciones: no hay PuntosAD en la escena");
            return;
        }

        // Cada vez que cambian los puntos, se repinta la tabla
        puntos.alCambiarPuntos.AddListener(AlCambiarPuntos);

        Actualizar();
    }

    private void OnDestroy()
    {
        if (puntos != null)
            puntos.alCambiarPuntos.RemoveListener(AlCambiarPuntos);
    }

    private void AlCambiarPuntos(int jugador, int total)
    {
        Actualizar();
    }

    // ---------- Pintar la tabla ----------

    public void Actualizar()
    {
        if (puntos == null)
            return;

        int[] lista = new int[GameSettings.MaxPlayers];
        for (int i = 0; i < lista.Length; i++)
            lista[i] = puntos.GetPuntos(i);

        List<int> orden = Ordenar(lista);
        int[] puestos = Puestos(lista, orden);

        // Zona de cada jugador: nombre, puntos y puesto
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

        // Tabla completa (pantalla de fin)
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

    // "Jugador 1 (Rojo)" pintado de su color
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

    // ---------- Logica (sin Unity: se puede probar con tests) ----------

    // Devuelve los jugadores (0 = J1 ... 3 = J4) de mas a menos puntos.
    // Con empate, va primero el de numero mas bajo.
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

    // Puesto de cada fila (1, 2, 2, 4...): con empate se comparte puesto
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
