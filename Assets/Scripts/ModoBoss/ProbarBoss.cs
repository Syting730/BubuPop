using UnityEngine;
using UnityEngine.InputSystem;

public class ProbarBoss : MonoBehaviour
{
    private VidaBoss boss;
    private PuntosBoss puntos;
    private SpriteRenderer sprite;
    private Renderer render3D;

    private void Start()
    {
        boss = GetComponent<VidaBoss>();
        puntos = GetComponent<PuntosBoss>();
        sprite = GetComponent<SpriteRenderer>();
        render3D = GetComponent<Renderer>();

        boss.alCambiarFase.AddListener(Pintar);
        Pintar(boss.GetFase());
    }

    private void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null)
            return;

        if (teclado.digit1Key.wasPressedThisFrame) boss.RecibirImpacto(0);
        if (teclado.digit2Key.wasPressedThisFrame) boss.RecibirImpacto(1);
        if (teclado.digit3Key.wasPressedThisFrame) boss.RecibirImpacto(2);
        if (teclado.digit4Key.wasPressedThisFrame) boss.RecibirImpacto(3);

        if (teclado.fKey.wasPressedThisFrame && puntos != null)
            puntos.RestarDanoBoss(0);

        if (teclado.rKey.wasPressedThisFrame)
        {
            boss.Iniciar();
            Pintar(boss.GetFase());
        }
    }

    private void Pintar(int fase)
    {
        Color color = fase == 1 ? Color.yellow
                    : fase == 2 ? new Color(1f, 0.5f, 0f)
                    : Color.red;

        if (sprite != null)
            sprite.color = color;
        else if (render3D != null)
            render3D.material.color = color;

        Debug.Log("Fase " + fase + ": fuego " + boss.DuracionFuego() +
                  " s, cada " + boss.PausaFuego() + " s, erupcion: " +
                  boss.HayErupcion());
    }
}
