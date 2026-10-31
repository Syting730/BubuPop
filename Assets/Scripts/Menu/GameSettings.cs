using UnityEngine;

// Guarda lo que se elige en el menu.
// Static: no va en ningun objeto y se conserva al cambiar de escena.
public static class GameSettings
{
    public enum GameMode
    {
        Clasico,
        AsaltoDiamante,
        BossVolcan
    }

    public enum PlayerColor
    {
        Rojo,
        Amarillo,
        Azul,
        Verde
    }

    // Cada partida tiene siempre 4 jugadores
    public const int MaxPlayers = 4;

    public static GameMode SelectedMode = GameMode.Clasico;

    // Jugadores reales (1-4). El resto son IAs.
    public static int HumanPlayers = 1;

    public static int AIPlayers
    {
        get { return MaxPlayers - HumanPlayers; }
    }

    // Color de cada jugador: [0] = J1, [1] = J2...
    // Primero los jugadores reales y despues las IAs
    public static PlayerColor[] PlayerColors = new PlayerColor[MaxPlayers];

    // Pasar de nuestro color al color de Unity (para pintar los cuadros)
    public static Color ToUnityColor(PlayerColor color)
    {
        switch (color)
        {
            case PlayerColor.Rojo: return new Color(1f, 0.2f, 0.2f);
            case PlayerColor.Amarillo: return new Color(1f, 0.85f, 0f);
            case PlayerColor.Azul: return new Color(0.2f, 0.4f, 1f);
            default: return new Color(0.2f, 0.8f, 0.2f); // Verde
        }
    }
}