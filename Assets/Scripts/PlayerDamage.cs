using System.Collections;
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

    [Header("Modo Asalto al Diamante")]
    [Tooltip("Segundos sin recibir daño despues de reaparecer")]
    [SerializeField, Min(0f)] private float invulnerableTrasReaparecer = 2f;

    private Vector3 spawnPosition;
    private PlayerShieldPowerUps powerUps;

    private bool vidasInfinitas = false;
    private bool invulnerable = false;

    private void Start()
    {
        spawnPosition = transform.position;

        powerUps = GetComponent<PlayerShieldPowerUps>();

        currentLives = maxLives;

        vidasInfinitas = FindAnyObjectByType<PuntosAD>() != null;

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

        if (powerUps != null && powerUps.EscudoActivo)
        {
            Debug.Log(playerName + " está protegido por el escudo.");
            return;
        }

        currentLives--;

        if (vidasInfinitas)
        {
            Debug.Log(playerName + " alcanzado: reaparece (modo AD)");
            Respawn();
            StartCoroutine(Invulnerable());
            return;
        }

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
        transform.position = spawnPosition;

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

        Debug.Log(
            "GAME OVER: " + playerName + " ha perdido sus vidas"
        );

        gameObject.SetActive(false);
    }

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
        if (invulnerable)
            return true;

        return powerUps != null && powerUps.EscudoActivo;
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