using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    public enum TipoPowerUp
    {
        Escudo,
        Guante,
        BombaExtra
    }

    [SerializeField] private TipoPowerUp tipo = TipoPowerUp.Escudo;

    private bool recogido;

    private void OnTriggerEnter2D(Collider2D other)
    {
        IntentarRecoger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        IntentarRecoger(other);
    }

    private void IntentarRecoger(Collider2D other)
    {
        if (recogido)
            return;

        bool recogidaCorrecta = false;

        if (tipo == TipoPowerUp.Escudo)
        {
            PlayerShieldPowerUps escudo =
                other.GetComponentInParent<PlayerShieldPowerUps>();

            if (escudo == null)
                return;

            escudo.ActivarEscudo();
            recogidaCorrecta = true;
        }
        else if (tipo == TipoPowerUp.Guante)
        {
            PlayerGlovePowerUp guante =
                other.GetComponentInParent<PlayerGlovePowerUp>();

            if (guante == null)
                return;

            recogidaCorrecta = guante.RecogerGuante();
        }
        else if (tipo == TipoPowerUp.BombaExtra)
        {
            PlayerExtraBombPowerUp bombaExtra =
                other.GetComponentInParent<PlayerExtraBombPowerUp>();

            if (bombaExtra == null)
                return;

            recogidaCorrecta = bombaExtra.RecogerBombaExtra();
        }

        if (recogidaCorrecta)
        {
            recogido = true;
            Destroy(gameObject);
        }
    }
}