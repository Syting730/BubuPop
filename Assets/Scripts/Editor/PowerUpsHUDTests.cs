using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PowerUpsHUDTests
{
    [UnityTearDown]
    public IEnumerator SalirDePlay()
    {
        if (Application.isPlaying)
            yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator Guante_ApareceSoloEnElHUDDelJugadorQueLoRecoge()
    {
        yield return new EnterPlayMode();

        GameObject jugador1 = new GameObject("Jugador 1");
        PlayerGlovePowerUp guante = jugador1.AddComponent<PlayerGlovePowerUp>();
        guante.usarTeclado = false;
        Transform hud1 = CrearHUD(jugador1);

        GameObject jugador2 = new GameObject("Jugador 2");
        jugador2.AddComponent<PlayerGlovePowerUp>().usarTeclado = false;
        Transform hud2 = CrearHUD(jugador2);
        yield return null;

        GameObject icono1 = hud1.Find("Power-ups/Guante").gameObject;
        GameObject icono2 = hud2.Find("Power-ups/Guante").gameObject;
        Assert.That(icono1.activeSelf, Is.False);
        Assert.That(icono2.activeSelf, Is.False);

        Assert.That(guante.RecogerGuante(), Is.True);
        yield return null;

        Assert.That(icono1.activeSelf, Is.True);
        Assert.That(icono2.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator Escudo_ApareceAlActivarloYSeOcultaAlExpirar()
    {
        yield return new EnterPlayMode();

        GameObject jugador = new GameObject("Jugador");
        PlayerShieldPowerUps escudo = jugador.AddComponent<PlayerShieldPowerUps>();
        SerializedObject datosEscudo = new SerializedObject(escudo);
        datosEscudo.FindProperty("duracionEscudo").floatValue = 0.75f;
        datosEscudo.ApplyModifiedPropertiesWithoutUndo();
        Transform hud = CrearHUD(jugador);
        yield return null;

        GameObject icono = hud.Find("Power-ups/Escudo").gameObject;
        Assert.That(icono.activeSelf, Is.False);

        Assert.That(escudo.ActivarEscudo(), Is.True);
        yield return null;
        Assert.That(icono.activeSelf, Is.True);

        float limite = Time.realtimeSinceStartup + 4f;
        while (icono.activeSelf && Time.realtimeSinceStartup < limite)
            yield return null;

        Assert.That(escudo.EscudoActivo, Is.False);
        Assert.That(icono.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator BurbujaExtra_SeOcultaAlColocarLaSegundaBurbuja()
    {
        yield return new EnterPlayMode();

        Scene escena = SceneManager.CreateScene("PowerUpsHUD_Test");
        SceneManager.SetActiveScene(escena);
        GameObject jugador = new GameObject("Jugador");
        BombPlacer colocador = jugador.AddComponent<BombPlacer>();
        colocador.usarTeclado = false;
        PlayerExtraBombPowerUp mejora = jugador.AddComponent<PlayerExtraBombPowerUp>();
        Grid tablero = new GameObject("Tablero").AddComponent<Grid>();
        GameObject burbujaDePrueba = new GameObject("Burbuja de prueba");
        SerializedObject datosColocador = new SerializedObject(colocador);
        datosColocador.FindProperty("grid").objectReferenceValue = tablero;
        datosColocador.FindProperty("bombPrefab").objectReferenceValue = burbujaDePrueba;
        datosColocador.ApplyModifiedPropertiesWithoutUndo();
        Transform hud = CrearHUD(jugador);
        yield return null;

        GameObject icono = hud.Find("Power-ups/Burbuja +1").gameObject;
        Assert.That(icono.activeSelf, Is.False);

        Assert.That(mejora.RecogerBombaExtra(), Is.True);
        yield return null;
        Assert.That(icono.activeSelf, Is.True);

        colocador.PlaceBomb();
        yield return null;
        Assert.That(icono.activeSelf, Is.True);

        jugador.transform.position = tablero.GetCellCenterWorld(Vector3Int.right);
        colocador.PlaceBomb();
        yield return null;
        Assert.That(icono.activeSelf, Is.False);
    }

    private static Transform CrearHUD(GameObject jugador)
    {
        GameObject canvas = new GameObject("Canvas", typeof(Canvas));
        GameObject panel = new GameObject("HUD", typeof(RectTransform));
        panel.transform.SetParent(canvas.transform, false);
        PowerUpsHUD hud = panel.AddComponent<PowerUpsHUD>();
        SerializedObject datosHud = new SerializedObject(hud);
        datosHud.FindProperty("jugador").objectReferenceValue = jugador;
        datosHud.ApplyModifiedPropertiesWithoutUndo();
        return panel.transform;
    }
}
