using UnityEngine;

// Base de un jugador en el modo Asalto al Diamante.
// Se pinta con el color del jugador al que pertenece.
public class Base : MonoBehaviour
{
    [Header("Jugador dueño de la base (0 = J1, 1 = J2, 2 = J3, 3 = J4)")]
    [SerializeField, Range(0, 3)] private int jugador;

    [Header("Transparencia del color (0 = invisible, 1 = opaco)")]
    [SerializeField, Range(0f, 1f)] private float transparencia = 0.4f;

    // Para que otros scripts (diamante, puntos) sepan de quien es
    public int Jugador
    {
        get { return jugador; }
    }

    private void Start()
    {
        // Si se abre la escena sin pasar por el menu, se queda
        // con el color que tenga puesto en el editor
        if (!GameSettings.SettingsChosen)
            return;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            return;

        Color color = GameSettings.ToUnityColor(
            GameSettings.PlayerColors[jugador]);
        color.a = transparencia;
        sr.color = color;
    }
}