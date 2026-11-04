using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGlovePowerUp : MonoBehaviour
{
    [Header("Guante")]
    [SerializeField, Min(1)] private int maxGuantesAlmacenados = 1;
    [SerializeField, Min(1)] private int distanciaEmpujeGuante = 2;
    [SerializeField] private Key teclaUsarGuante = Key.Q;

    private int guantesDisponibles;
    private BombPlacer colocadorBombas;

    public int GuantesDisponibles => guantesDisponibles;

    private void Awake()
    {
        colocadorBombas = GetComponent<BombPlacer>();
    }

    private void Update()
    {
        if (guantesDisponibles <= 0 || Keyboard.current == null)
            return;

        if (!Keyboard.current[teclaUsarGuante].wasPressedThisFrame)
            return;

        if (colocadorBombas == null)
            return;

        bool bombaEmpujada =
            colocadorBombas.EmpujarBomba(distanciaEmpujeGuante);

        if (bombaEmpujada)
            guantesDisponibles--;
    }

    public bool RecogerGuante()
    {
        if (guantesDisponibles >= maxGuantesAlmacenados)
            return false;

        guantesDisponibles++;
        return true;
    }
}