using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra de vida del Boss Volcán (tarjeta Lab3 - MB - Generación del Boss)
// - Se vacía con cada impacto: 60 HP -> 0 HP.
// - Texto opcional "45 / 60 HP".
// - Cambia de color con la fase: amarillo (F1), naranja (F2), rojo (F3).
//
// Va en el objeto de la barra, dentro del Canvas del HUD.
// "Relleno" es la Image de color que se vacía. Vale de dos formas:
// - con un sprite y Image Type = Filled, Fill Method = Horizontal
// - sin sprite: se encoge en X (poner su Pivot X a 0)
public class BarraVidaBoss : MonoBehaviour
{
    [Header("Barra")]
    [Tooltip("Image de color que se vacía (Filled Horizontal, o sin sprite con Pivot X = 0)")]
    [SerializeField] private Image relleno;

    [Tooltip("Texto con la vida, por ejemplo 45 / 60 HP (opcional)")]
    [SerializeField] private TMP_Text texto;

    [Header("Color por fase")]
    [SerializeField] private Color colorFase1 = new Color(1f, 0.85f, 0f);
    [SerializeField] private Color colorFase2 = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color colorFase3 = new Color(0.9f, 0.1f, 0.1f);

    [Tooltip("Si se deja vacío, se busca solo en la escena")]
    [SerializeField] private VidaBoss boss;

    private void Start()
    {
        if (boss == null)
            boss = FindAnyObjectByType<VidaBoss>();

        if (boss == null)
        {
            Debug.LogWarning("BarraVidaBoss: no hay VidaBoss en la escena");
            gameObject.SetActive(false);
            return;
        }

        if (relleno == null)
            Debug.LogWarning("BarraVidaBoss: falta asignar el Relleno");

        boss.alCambiarVida.AddListener(Mostrar);
        boss.alCambiarFase.AddListener(CambiarColor);

        Mostrar(boss.GetVida(), boss.GetVidaMaxima());
        CambiarColor(boss.GetFase());
    }

    private void OnDestroy()
    {
        if (boss != null)
        {
            boss.alCambiarVida.RemoveListener(Mostrar);
            boss.alCambiarFase.RemoveListener(CambiarColor);
        }
    }

    private void Mostrar(int vida, int vidaMaxima)
    {
        if (relleno != null)
        {
            float parte = Fraccion(vida, vidaMaxima);

            if (relleno.type == Image.Type.Filled)
                relleno.fillAmount = parte;
            else
                relleno.rectTransform.localScale = new Vector3(parte, 1f, 1f);
        }

        if (texto != null)
            texto.text = vida + " / " + vidaMaxima + " HP";
    }

    private void CambiarColor(int fase)
    {
        if (relleno == null)
            return;

        relleno.color = fase >= 3 ? colorFase3
                      : fase == 2 ? colorFase2
                      : colorFase1;
    }

    // Parte de la barra que queda llena (0 = vacía, 1 = llena)
    public static float Fraccion(int vida, int vidaMaxima)
    {
        if (vidaMaxima <= 0)
            return 0f;

        return Mathf.Clamp01((float)vida / vidaMaxima);
    }
}