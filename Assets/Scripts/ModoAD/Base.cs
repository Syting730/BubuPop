using UnityEngine;

public class Base : MonoBehaviour
{
    [Header("Jugador dueño de la base (0 = J1, 1 = J2, 2 = J3, 3 = J4)")]
    [SerializeField, Range(0, 3)] private int jugador;

    [Header("Transparencia del color (0 = invisible, 1 = opaco)")]
    [SerializeField, Range(0f, 1f)] private float transparencia = 0.4f;

    public int Jugador
    {
        get { return jugador; }
    }

    private void Start()
    {
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