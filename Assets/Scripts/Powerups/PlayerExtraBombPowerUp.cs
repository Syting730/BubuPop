using UnityEngine;

public class PlayerExtraBombPowerUp : MonoBehaviour
{
    private BombPlacer colocadorBombas;

    private void Awake()
    {
        colocadorBombas = GetComponent<BombPlacer>();
    }

    public bool RecogerBombaExtra()
    {
        if (colocadorBombas == null)
            return false;

        return colocadorBombas.ActivarBombaDoble();
    }
}