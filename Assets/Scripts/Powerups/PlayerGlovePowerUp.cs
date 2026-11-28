using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class PlayerGlovePowerUp : MonoBehaviour
{
    [Header("Guante")]
    [SerializeField, Min(1)] private int distanciaEmpujeGuante = 2;
    [SerializeField] private Key teclaUsarGuante = Key.Q;
    [SerializeField] private GamepadButton botonMandoUsarGuante = GamepadButton.West;

    private int guantesDisponibles;
    private BombPlacer colocadorBombas;
    private PlayerMovement movimiento;

    public int GuantesDisponibles => guantesDisponibles;

    public bool usarTeclado = true;

    private void Awake()
    {
        colocadorBombas = GetComponent<BombPlacer>();
        movimiento = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (!usarTeclado || guantesDisponibles <= 0 || colocadorBombas == null)
            return;

        Keyboard teclado = Keyboard.current;
        bool teclaPulsada = teclado != null &&
            teclaUsarGuante != Key.None &&
            (movimiento == null || movimiento.controls != PlayerMovement.ControlScheme.SoloMando) &&
            teclado[teclaUsarGuante].wasPressedThisFrame;

        Gamepad mando = movimiento != null ? movimiento.MiMando() : null;
        bool botonPulsado = mando != null &&
            mando[botonMandoUsarGuante].wasPressedThisFrame;

        if ((teclaPulsada || botonPulsado) && colocadorBombas.EmpujarBomba(distanciaEmpujeGuante))
            guantesDisponibles--;
    }

    public bool RecogerGuante()
    {
        if (guantesDisponibles > 0)
            return false;

        guantesDisponibles++;
        return true;
    }
}