using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpsHUD : MonoBehaviour
{
    [SerializeField] private GameObject jugador;
    [SerializeField] private Vector2 desplazamiento;
    [SerializeField] private Sprite spriteGuante;
    [SerializeField] private Sprite spriteEscudo;
    [SerializeField] private Sprite spriteBurbujaExtra;

    private PlayerGlovePowerUp guante;
    private PlayerShieldPowerUps escudo;
    private BombPlacer colocador;
    private GameObject iconoGuante;
    private GameObject iconoEscudo;
    private GameObject iconoBurbujaExtra;

    private void Start()
    {
        if (jugador == null)
        {
            enabled = false;
            return;
        }

        guante = jugador.GetComponent<PlayerGlovePowerUp>();
        escudo = jugador.GetComponent<PlayerShieldPowerUps>();
        colocador = jugador.GetComponent<BombPlacer>();

        RectTransform fila = CrearRect("Power-ups", transform, desplazamiento, new Vector2(174, 48));
        iconoGuante = CrearIcono("Guante", spriteGuante, fila, -58);
        iconoEscudo = CrearIcono("Escudo", spriteEscudo, fila, 0);
        iconoBurbujaExtra = CrearIcono("Burbuja +1", spriteBurbujaExtra, fila, 58);

        RectTransform etiqueta = CrearRect("+1", iconoBurbujaExtra.transform,
            new Vector2(15, -15), new Vector2(30, 24));
        TextMeshProUGUI texto = etiqueta.gameObject.AddComponent<TextMeshProUGUI>();
        texto.text = "+1";
        texto.fontSize = 21;
        texto.fontStyle = FontStyles.Bold;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;

        LateUpdate();
    }

    private void LateUpdate()
    {
        bool jugadorActivo = jugador != null && jugador.activeInHierarchy;
        iconoGuante.SetActive(jugadorActivo && guante != null && guante.GuantesDisponibles > 0);
        iconoEscudo.SetActive(jugadorActivo && escudo != null && escudo.EscudoActivo);
        iconoBurbujaExtra.SetActive(jugadorActivo && colocador != null && colocador.BombaExtraPendiente);
    }

    private static GameObject CrearIcono(string nombre, Sprite sprite, RectTransform fila, float x)
    {
        RectTransform rect = CrearRect(nombre, fila, new Vector2(x, 0), new Vector2(48, 48));
        Image imagen = rect.gameObject.AddComponent<Image>();
        imagen.sprite = sprite;
        imagen.preserveAspect = true;
        imagen.raycastTarget = false;
        return rect.gameObject;
    }

    private static RectTransform CrearRect(string nombre, Transform padre, Vector2 posicion, Vector2 tamano)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        RectTransform rect = (RectTransform)objeto.transform;
        rect.SetParent(padre, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        return rect;
    }
}
