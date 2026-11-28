using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public class PowerUpsTests
{
    private readonly List<GameObject> objetos = new List<GameObject>();

    [UnityTearDown]
    public IEnumerator Limpiar()
    {
        for (int i = objetos.Count - 1; i >= 0; i--)
        {
            if (objetos[i] != null)
                Object.DestroyImmediate(objetos[i]);
        }

        objetos.Clear();

        if (Application.isPlaying)
            yield return new ExitPlayMode();
    }

    [Test]
    public void Guante_SePuedeRecogerPeroNoAcumular()
    {
        PlayerGlovePowerUp guante = CrearObjeto("Jugador").AddComponent<PlayerGlovePowerUp>();

        Assert.That(guante.GuantesDisponibles, Is.Zero);
        Assert.That(guante.RecogerGuante(), Is.True);
        Assert.That(guante.RecogerGuante(), Is.False);
        Assert.That(guante.GuantesDisponibles, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator BombaExtra_AumentaLaCapacidadUnaVez()
    {
        yield return new EnterPlayMode();

        GameObject jugador = CrearObjeto("Jugador");
        BombPlacer colocador = jugador.AddComponent<BombPlacer>();
        PlayerExtraBombPowerUp mejora =
            jugador.AddComponent<PlayerExtraBombPowerUp>();

        Assert.That(colocador.CapacidadBombas, Is.EqualTo(1));
        Assert.That(mejora.RecogerBombaExtra(), Is.True);
        Assert.That(colocador.CapacidadBombas, Is.EqualTo(2));
        Assert.That(mejora.RecogerBombaExtra(), Is.False);
        Assert.That(colocador.CapacidadBombas, Is.EqualTo(2));
    }

    [Test]
    public void EmpujarBomba_MueveLaBombaDelOtroJugadorDosCasillas()
    {
        Grid tablero = CrearObjeto("Tablero").AddComponent<Grid>();
        tablero.transform.position = new Vector3(1000f, 1000f, 0f);
        GameObject muros = CrearObjeto("Muros");
        muros.transform.SetParent(tablero.transform, false);
        GameObject obstaculos = CrearObjeto("Obstáculos");
        obstaculos.transform.SetParent(tablero.transform, false);

        BombPlacer colocador = CrearObjeto("Jugador con guante").AddComponent<BombPlacer>();
        colocador.transform.position = tablero.GetCellCenterWorld(Vector3Int.zero);
        AsignarReferencia(colocador, "grid", tablero);

        GameObject rival = CrearObjeto("Otro jugador");
        Boom bomba = CrearObjeto("Bomba del otro jugador").AddComponent<Boom>();
        bomba.transform.position = tablero.GetCellCenterWorld(Vector3Int.right);
        bomba.SetDueno(rival);
        AsignarReferencia(bomba, "grid", tablero);
        AsignarReferencia(bomba, "fixedWallsTilemap", muros.AddComponent<Tilemap>());
        AsignarReferencia(bomba, "destructibleTilemap", obstaculos.AddComponent<Tilemap>());

        // La bomba de prueba carece de prefab visual para el aviso de explosión.
        LogAssert.Expect(LogType.Warning, "Boom: no se ha asignado ExplosionWarning.");

        Assert.That(colocador.EmpujarBomba(2), Is.True);
        Assert.That(tablero.WorldToCell(bomba.transform.position),
            Is.EqualTo(new Vector3Int(3, 0, 0)));
    }

    [UnityTest]
    public IEnumerator Escudo_AlRecogerloProtegeLasVidasDelJugador()
    {
        yield return new EnterPlayMode();

        Scene escena = SceneManager.CreateScene("Escudo_Test",
            new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        GameObject jugador = CrearObjeto("Jugador");
        SceneManager.MoveGameObjectToScene(jugador, escena);
        jugador.AddComponent<BoxCollider2D>();
        jugador.AddComponent<Rigidbody2D>().gravityScale = 0f;
        PlayerShieldPowerUps escudo = jugador.AddComponent<PlayerShieldPowerUps>();
        PlayerDamage dano = jugador.AddComponent<PlayerDamage>();
        yield return null;

        int vidasAntes = dano.GetCurrentLives();
        Assert.That(vidasAntes, Is.GreaterThan(0));
        Assert.That(escudo.EscudoActivo, Is.False);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/PowerUps/EscudoPrueba.prefab");
        Assert.That(prefab, Is.Not.Null);
        GameObject recogible = Object.Instantiate(prefab, jugador.transform.position, Quaternion.identity);
        objetos.Add(recogible);
        SceneManager.MoveGameObjectToScene(recogible, escena);

        escena.GetPhysicsScene2D().Simulate(Time.fixedDeltaTime);
        yield return null;

        Assert.That(recogible == null, Is.True);
        Assert.That(escudo.EscudoActivo, Is.True);
        dano.TakeDamage();
        Assert.That(dano.GetCurrentLives(), Is.EqualTo(vidasAntes));
    }

    private GameObject CrearObjeto(string nombre)
    {
        GameObject objeto = new GameObject(nombre);
        objetos.Add(objeto);
        return objeto;
    }

    private static void AsignarReferencia(Object componente, string campo, Object referencia)
    {
        SerializedObject objeto = new SerializedObject(componente);
        objeto.FindProperty(campo).objectReferenceValue = referencia;
        objeto.ApplyModifiedPropertiesWithoutUndo();
    }
}
