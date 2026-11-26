using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Tests de la tarjeta Lab3 - MB - Generación del Boss
// (Window > General > Test Runner > EditMode > Run All)
public class BossGeneracionTests
{
    private GameObject objeto;
    private VidaBoss boss;

    [SetUp]
    public void Preparar()
    {
        objeto = new GameObject("Boss_Test");
        boss = objeto.AddComponent<VidaBoss>();
    }

    [TearDown]
    public void Limpiar()
    {
        Object.DestroyImmediate(objeto);
    }

    private void Impactos(int n)
    {
        for (int i = 0; i < n; i++)
            boss.RecibirImpacto(0);
    }

    // ---------- Vida ----------

    [Test]
    public void Boss_Empieza_Con60HP()
    {
        Assert.AreEqual(60, boss.GetVida());
        Assert.AreEqual(60, boss.GetVidaMaxima());
        Assert.IsFalse(boss.EstaMuerto());
    }

    [Test]
    public void CadaBurbuja_Quita3HP()
    {
        boss.RecibirImpacto(0);
        Assert.AreEqual(57, boss.GetVida());

        boss.RecibirImpacto(1);
        Assert.AreEqual(54, boss.GetVida());
    }

    [Test]
    public void VeinteImpactos_DerrotanAlBoss()
    {
        Impactos(20);

        Assert.AreEqual(0, boss.GetVida());
        Assert.IsTrue(boss.EstaMuerto());
    }

    [Test]
    public void DiecinueveImpactos_NoDerrotanAlBoss()
    {
        Impactos(19);

        Assert.AreEqual(3, boss.GetVida());
        Assert.IsFalse(boss.EstaMuerto());
    }

    [Test]
    public void AlMorir_SeAvisaUnaSolaVez_ConQuienDioElGolpeFinal()
    {
        int veces = 0;
        int ganador = -1;
        boss.alMorir.AddListener(j => { veces++; ganador = j; });

        Impactos(19);
        boss.RecibirImpacto(2);
        boss.RecibirImpacto(1); // ya muerto: no cuenta

        Assert.AreEqual(1, veces);
        Assert.AreEqual(2, ganador);
    }

    // ---------- Barra de vida ----------

    [Test]
    public void Barra_LlenaAl100_YVaciaAl0()
    {
        Assert.AreEqual(1f, BarraVidaBoss.Fraccion(60, 60), 0.0001f);
        Assert.AreEqual(0.5f, BarraVidaBoss.Fraccion(30, 60), 0.0001f);
        Assert.AreEqual(0f, BarraVidaBoss.Fraccion(0, 60), 0.0001f);
    }

    [Test]
    public void Barra_RecibeLaVidaDespuesDeCadaImpacto()
    {
        int ultimaVida = -1;
        boss.alCambiarVida.AddListener((vida, maxima) => ultimaVida = vida);

        Impactos(5);

        Assert.AreEqual(45, ultimaVida);
        Assert.AreEqual(0.75f, BarraVidaBoss.Fraccion(ultimaVida, 60), 0.0001f);
    }

    // ---------- Posición en el tablero ----------

    [Test]
    public void Centro_DelTableroDeTableroBoss_EsLaCasilla2Menos1()
    {
        // Suelo de TableroBoss: x de -5 a 9, y de -7 a 5
        BoundsInt suelo = new BoundsInt(-5, -7, 0, 15, 13, 1);

        Assert.AreEqual(new Vector3Int(2, -1, 0), BossEnTablero.CentroDelSuelo(suelo));
    }

    [Test]
    public void BossRadio0_Ocupa1Casilla_YRadio1_Ocupa9()
    {
        Vector3Int centro = new Vector3Int(2, -1, 0);

        List<Vector3Int> pequeno = BossEnTablero.CalcularCasillas(centro, 0);
        List<Vector3Int> grande = BossEnTablero.CalcularCasillas(centro, 1);

        Assert.AreEqual(1, pequeno.Count);
        Assert.Contains(centro, pequeno);
        Assert.AreEqual(9, grande.Count);
        Assert.Contains(new Vector3Int(3, 0, 0), grande);
    }
}