using UnityEngine;

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

    public const int MaxPlayers = 4;

    public static GameMode SelectedMode = GameMode.Clasico;

    public static bool SettingsChosen = false;

    public static int HumanPlayers = 1;

    public static int AIPlayers
    {
        get { return MaxPlayers - HumanPlayers; }
    }

    public static PlayerColor[] PlayerColors = new PlayerColor[MaxPlayers];

    public static Color ToUnityColor(PlayerColor color)
    {
        switch (color)
        {
            case PlayerColor.Rojo: return new Color(1f, 0.2f, 0.2f);
            case PlayerColor.Amarillo: return new Color(1f, 0.85f, 0f);
            case PlayerColor.Azul: return new Color(0.2f, 0.4f, 1f);
            default: return new Color(0.2f, 0.8f, 0.2f);
        }
    }
}