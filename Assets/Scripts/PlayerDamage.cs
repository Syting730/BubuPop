
using UnityEngine;

public class PlayerDamage : MonoBehaviour
{
    private bool isDead = false;

    public void TakeDamage()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("El jugador ha muerto por la explosión");

        gameObject.SetActive(false);
    }
}
