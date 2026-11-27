using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Tests de la tarjeta Lab3 - MB - Erupción
// (Window > General > Test Runner > EditMode > Run All)
public class ErupcionTests
{
    private GameObject objetoBoss;
    private VidaBoss boss;
    private GameObject objetoAviso;
    private AvisoSuelo aviso;

    [SetUp]
    public void Preparar()
    {
        objetoBoss = new GameObject("Boss_Test");
        boss = objetoBoss.AddComponent<VidaBoss>();

        objetoAviso = new GameObject("Aviso_Test");
        aviso = objetoAviso.AddComponent<AvisoSuelo>();
    }

    [TearDown]
    public void Limpiar()
    {
        Object.DestroyImmediate(objetoBoss);
        Object.DestroyImmediate(objetoAviso);
    }

    private void Impactos(int n)
    {
        for (int i = 0; i < n; i++)
            boss.RecibirImpacto(0);
    }

    // ---------- Solo en la fase 3 ----------

    [Test]
    public void NoHayErupcion_EnLaFase1()
    {
        // 60 HP
        Assert.AreEqual(1, boss.GetFase());
        Assert.IsFalse(boss.HayErupcion());
    }

    [Test]
    public void NoHayErupcion_EnLaFase2()
    {
        Impactos(11); // 27 HP

        Assert.AreEqual(2, boss.GetFase());
        Assert.IsFalse(boss.HayErupcion());
    }

    [Test]
    public void HayErupcion_EnLaFase3_PorDebajoDe20HP()
    {
        Impactos(14); // 18 HP

        Assert.AreEqual(3, boss.GetFase());
        Assert.IsTrue(boss.HayErupcion());
    }

    // ---------- 4 bolas donde están los jugadores ----------

    [Test]
    public void CuatroJugadores_CaeUnaBolaEnCadaUno()
    {
        List<Vector3Int> jugadores = new List<Vector3Int>
        {
            new Vector3Int(-4, 4, 0), new Vector3Int(8, 4, 0),
            new Vector3Int(-4, -6, 0), new Vector3Int(8, -6, 0)
        };
        List<Vector3Int> libres = new List<Vector3Int> { new Vector3Int(0, 0, 0) };

        List<Vector3Int> bolas = Erupcion.ElegirCasillas(jugadores, libres, 4);

        Assert.AreEqual(4, bolas.Count);
        foreach (Vector3Int j in jugadores)
            Assert.Contains(j, bolas);
    }

    [Test]
    public void DosJugadores_LasOtras2BolasCaenEnCasillasLibres()
    {
        List<Vector3Int> jugadores = new List<Vector3Int>
        {
            new Vector3Int(1, 1, 0), new Vector3Int(5, 1, 0)
        };
        List<Vector3Int> libres = new List<Vector3Int>
        {
            new Vector3Int(1, 1, 0), // la de un jugador: no se repite
            new Vector3Int(2, 2, 0), new Vector3Int(3, 3, 0), new Vector3Int(4, 4, 0)
        };

        List<Vector3Int> bolas = Erupcion.ElegirCasillas(jugadores, libres, 4);

        Assert.AreEqual(4, bolas.Count);
        Assert.Contains(new Vector3Int(1, 1, 0), bolas);
        Assert.Contains(new Vector3Int(5, 1, 0), bolas);

        // Sin casillas repetidas
        Assert.AreEqual(4, new HashSet<Vector3Int>(bolas).Count);
    }

    // ---------- Cae donde se avisó ----------

    [Test]
    public void LaBola_SoloDana_EnLaCasillaAvisada()
    {
        Vector3Int jugador = new Vector3Int(2, 2, 0);
        List<Vector3Int> bolas = Erupcion.ElegirCasillas(
            new List<Vector3Int> { jugador }, new List<Vector3Int>(), 4);

        // Antes del aviso: no hace daño
        Assert.IsFalse(Erupcion.DebeDanar(jugador, bolas, aviso));

        // Empieza el aviso (pone el suelo en rojo en esas casillas)
        IEnumerator avisando = aviso.Avisar(bolas);
        avisando.MoveNext();

        // Cae donde se avisó, y solo ahí
        Assert.IsTrue(aviso.EstaAvisada(jugador));
        Assert.IsTrue(Erupcion.DebeDanar(jugador, bolas, aviso));
        Assert.IsFalse(Erupcion.DebeDanar(new Vector3Int(3, 2, 0), bolas, aviso));

        // Termina la erupción: ya no hace daño
        aviso.QuitarAviso(bolas);
        Assert.IsFalse(Erupcion.DebeDanar(jugador, bolas, aviso));
    }
}
