
using UnityEngine;

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

    private Vector3 spawnPosition;

    private void Start()
    {
        // Guardar la posicion inicial
        spawnPosition = transform.position;

        // Inicializar vidas
        currentLives = maxLives;

        // Mostrar las vidas iniciales
        if (livesUI != null)
        {
            livesUI.UpdateLives(currentLives);
        }

        Debug.Log("Vidas iniciales: " + currentLives);
    }

    public void TakeDamage()
    {
        if (isDead)
            return;

        // Restar una vida
        currentLives--;

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
