using NUnit.Framework;
using UnityEngine;

// Tests de las tarjetas "Lab3 - MB - Tabla de puntuaciones"
// y "Lab3 - MB - Fases del Boss".
// Se ejecutan en Window > General > Test Runner > EditMode > Run All.
public class BossTests
{
    private GameObject objeto;
    private PuntosBoss puntos;
    private VidaBoss boss;

    [SetUp]
    public void Preparar()
    {
        objeto = new GameObject("Boss_Test");
        puntos = objeto.AddComponent<PuntosBoss>();
        boss = objeto.AddComponent<VidaBoss>();
    }

    [TearDown]
    public void Limpiar()
    {
        Object.DestroyImmediate(objeto);
    }

    // Da n impactos al Boss con el jugador indicado
    private void Impactos(int jugador, int n)
    {
        for (int i = 0; i < n; i++)
            boss.RecibirImpacto(jugador);
    }

    // ---------- Tabla de puntuaciones ----------

    [Test]
    public void ImpactoAlBoss_Da3Puntos()
    {
        boss.RecibirImpacto(0);
        Assert.AreEqual(3, puntos.GetPuntos(0));
    }

    [Test]
    public void GolpeFinal_Da15Puntos()
    {
        // 19 impactos de J2, el 20.º (golpe final) de J1
        Impactos(1, 19);
        boss.RecibirImpacto(0);

        Assert.AreEqual(15, puntos.GetPuntos(0));
        Assert.IsTrue(boss.EstaMuerto());
    }

    [Test]
    public void EliminarAOtroJugador_Da5Puntos()
    {
        puntos.SumarEliminacion(0, 1);
        Assert.AreEqual(5, puntos.GetPuntos(0));
    }

    [Test]
    public void EliminarseAUnoMismo_NoDaPuntos()
    {
        puntos.SumarEliminacion(2, 2);
        Assert.AreEqual(0, puntos.GetPuntos(2));
    }

    [Test]
    public void Obstaculos_Dan1PuntoCadaUno()
    {
        puntos.SumarObstaculos(3, 2);
        Assert.AreEqual(2, puntos.GetPuntos(3));
    }

    [Test]
    public void DanoDelBoss_Quita3Puntos()
    {
        puntos.SumarPuntos(0, 10);
        puntos.RestarDanoBoss(0);
        Assert.AreEqual(7, puntos.GetPuntos(0));
    }

    [Test]
    public void DanoDelBoss_NoBajaDe0()
    {
        puntos.SumarPuntos(0, 2);
        puntos.RestarDanoBoss(0);
        Assert.AreEqual(0, puntos.GetPuntos(0));
    }

    // ---------- Fases del Boss ----------

    [Test]
    public void Boss_Empieza60HP_Fase1()
    {
        Assert.AreEqual(60, boss.GetVida());
        Assert.AreEqual(1, boss.GetFase());
    }

    [Test]
    public void CambiaDeFase_A30y20HP()
    {
        Assert.AreEqual(1, VidaBoss.CalcularFase(30));
        Assert.AreEqual(2, VidaBoss.CalcularFase(29));
        Assert.AreEqual(2, VidaBoss.CalcularFase(20));
        Assert.AreEqual(3, VidaBoss.CalcularFase(19));
    }

    [Test]
    public void ConImpactos_PasaPorLas3Fases()
    {
        Impactos(0, 10);                 // 30 HP
        Assert.AreEqual(1, boss.GetFase());

        boss.RecibirImpacto(0);          // 27 HP
        Assert.AreEqual(2, boss.GetFase());

        Impactos(0, 3);                  // 18 HP
        Assert.AreEqual(3, boss.GetFase());
        Assert.IsTrue(boss.HayErupcion());
    }

    [Test]
    public void EventoCambioFase_SeLlamaDosVeces()
    {
        int veces = 0;
        boss.alCambiarFase.AddListener(f => veces++);

        Impactos(0, 20);                 // de 60 a 0 HP

        Assert.AreEqual(2, veces);       // fase 2 y fase 3
    }

    [Test]
    public void FuegoSegunFase()
    {
        Assert.AreEqual(1f, boss.DuracionFuego());
        Assert.AreEqual(5f, boss.PausaFuego());

        Impactos(0, 11);                 // fase 2
        Assert.AreEqual(2f, boss.DuracionFuego());
        Assert.AreEqual(3f, boss.PausaFuego());
        Assert.IsFalse(boss.HayErupcion());
    }

    [Test]
    public void BossMuerto_NoRecibeMasImpactos()
    {
        Impactos(0, 20);
        int antes = puntos.GetPuntos(0);

        boss.RecibirImpacto(0);

        Assert.AreEqual(0, boss.GetVida());
        Assert.AreEqual(antes, puntos.GetPuntos(0));
    }
}
