using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MatchManager : MonoBehaviour
{
    [Header("Interfaz (opcional)")]
    [SerializeField] private TMP_Text resultText;

    [Header("Reinicio")]
    [SerializeField] private Key restartKey = Key.R;

    private List<PlayerDamage> players = new List<PlayerDamage>();

    private bool matchOver = false;
    private string resultMessage = "";

    private void Start()
    {
        players.AddRange(FindObjectsByType<PlayerDamage>());

        if (players.Count < 2)
        {
            Debug.LogWarning(
                "MatchManager: hay menos de 2 jugadores en la escena."
            );
        }

        if (resultText != null)
            resultText.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (matchOver)
        {
            if (Keyboard.current != null &&
                Keyboard.current[restartKey].wasPressedThisFrame)
            {
                RestartMatch();
            }

            return;
        }

        List<PlayerDamage> alivePlayers = new List<PlayerDamage>();

        foreach (PlayerDamage player in players)
        {
            if (player != null && !player.IsDead())
                alivePlayers.Add(player);
        }

        if (players.Count >= 2 && alivePlayers.Count <= 1)
        {
            if (alivePlayers.Count == 1)
                EndMatch("¡Gana " + alivePlayers[0].GetPlayerName() + "!");
            else
                EndMatch("¡Empate!");
        }
    }

    private void EndMatch(string message)
    {
        matchOver = true;
        resultMessage = message;

        Debug.Log("Fin de la partida: " + message);

        Time.timeScale = 0f;

        if (resultText != null)
        {
            resultText.text =
                message + "\nPulsa " + restartKey + " para reiniciar";
            resultText.gameObject.SetActive(true);
        }
    }

    private void RestartMatch()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    private void OnGUI()
    {
        if (!matchOver || resultText != null)
            return;

        int bigSize = Mathf.Max(24, Screen.height / 10);
        int smallSize = bigSize / 2;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = bigSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.richText = true;
        style.normal.textColor = Color.white;

        Rect area = new Rect(0, Screen.height * 0.3f,
                             Screen.width, Screen.height * 0.4f);
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(area, Texture2D.whiteTexture);
        GUI.color = previous;

        GUI.Label(
            area,
            resultMessage + "\n<size=" + smallSize + ">Pulsa " +
            restartKey + " para reiniciar</size>",
            style
        );
    }
}
