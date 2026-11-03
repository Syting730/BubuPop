using System.Collections;
using UnityEngine;

// Vidas del jugador.
// - Modo Clasico: 3 vidas; con 0 vidas el jugador queda eliminado.
// - Modo Asalto al Diamante (hay PuntosAD en la escena):
//   reaparece siempre en su salida, sin limite de vidas,
//   y durante 2 s es invulnerable (parpadea).

public class PlayerDamage : MonoBehaviour
{
    [Header("Jugador")]
    [SerializeField] private string playerName = "Jugador 1";

    [Header("Sistema de vidas")]
    [SerializeField] private int maxLives = 3;

    [Header("Interfaz de vidas")]
    [SerializeField] private LivesUI livesUI;

    private int currentLives;
    private bool isDead = false;

    [Header("Modo Asalto al Diamante")]
    [Tooltip("Segundos sin recibir daño despues de reaparecer")]
    [SerializeField, Min(0f)] private float invulnerableTrasReaparecer = 2f;

    private Vector3 spawnPosition;

    // true en el modo AD: nunca se queda sin vidas
    private bool vidasInfinitas = false;
    private bool invulnerable = false;

    private void Start()
    {
        // Guardar la posicion inicial
        spawnPosition = transform.position;

        // Inicializar vidas
        currentLives = maxLives;

        // Si hay puntos del modo AD en la escena, se reaparece siempre
        vidasInfinitas = FindAnyObjectByType<PuntosAD>() != null;

        // Mostrar las vidas iniciales
        if (livesUI != null)
        {
            livesUI.UpdateLives(currentLives);
        }

        Debug.Log("Vidas iniciales: " + currentLives);
    }

    public void TakeDamage()
    {
        if (isDead || invulnerable)
            return;

        // Restar una vida
        // (en el modo AD tambien se resta, aunque no se muere:
        //  asi el Diamante sabe que han alcanzado a su portador)
        currentLives--;

        if (vidasInfinitas)
        {
            Debug.Log(playerName + " alcanzado: reaparece (modo AD)");
            Respawn();
            StartCoroutine(Invulnerable());
            return;
        }

        // Actualizar corazones
        if (livesUI != null)
        {
            livesUI.UpdateLives(currentLives);
        }

        Debug.Log("Jugador alcanzado. Vidas: " + currentLives);

        if (currentLives <= 0)
        {
            Die();
        }
        else
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        // Regresar a la posicion inicial
        transform.position = spawnPosition;

        // Detener el movimiento del jugador
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Debug.Log(
            "Jugador reaparecido. Vidas restantes: " +
            currentLives
        );
    }

    private void Die()
    {
        isDead = true;

        Debug.Log("GAME OVER: " + playerName + " ha perdido sus vidas");

        gameObject.SetActive(false);
    }

    // Parpadea y no recibe daño durante unos segundos.
    // WaitForSeconds usa el tiempo del juego: en pausa se para.
    private IEnumerator Invulnerable()
    {
        invulnerable = true;

        SpriteRenderer[] dibujos = GetComponentsInChildren<SpriteRenderer>();
        float tiempo = 0f;
        float paso = 0.1f;

        while (tiempo < invulnerableTrasReaparecer)
        {
            foreach (SpriteRenderer sr in dibujos)
                sr.enabled = !sr.enabled;

            yield return new WaitForSeconds(paso);
            tiempo += paso;
        }

        foreach (SpriteRenderer sr in dibujos)
            sr.enabled = true;

        invulnerable = false;
    }

    public bool IsInvulnerable()
    {
        return invulnerable;
    }

    public bool IsDead()
    {
        return isDead;
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public int GetCurrentLives()
    {
        return currentLives;
    }
}
