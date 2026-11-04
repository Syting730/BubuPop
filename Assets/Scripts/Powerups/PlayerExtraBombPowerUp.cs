using UnityEngine;

public class PlayerExtraBombPowerUp : MonoBehaviour
{
    [Header("Bomba extra")]
    [SerializeField, Min(1)] private int bombasExtraPorRecogida = 1;
    [SerializeField, Min(1)] private int capacidadMaximaBombas = 3;

    private BombPlacer colocadorBombas;

    private void Awake()
    {
        colocadorBombas = GetComponent<BombPlacer>();
    }

    public bool RecogerBombaExtra()
    {
        if (colocadorBombas == null)
            return false;

        return colocadorBombas.AumentarCapacidadBombas(
            bombasExtraPorRecogida,
            capacidadMaximaBombas
        );
    }
}