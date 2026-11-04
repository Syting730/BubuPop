using NUnit.Framework;
using UnityEngine;

// Tests de la tarjeta "Lab2 - Modo AD - Tabla Puntuaciones".
// Se ejecutan en Window > General > Test Runner > EditMode > Run All.
public class PuntosADTests
{
    private GameObject objeto;
    private PuntosAD puntos;

    [SetUp]
    public void Preparar()
    {
        objeto = new GameObject("PuntosAD_Test");
        puntos = objeto.AddComponent<PuntosAD>();
    }

    [TearDown]
    public void Limpiar()
    {
        Object.DestroyImmediate(objeto);
    }

    [Test]
    public void Cristal_Suma2Puntos()
    {
        puntos.SumarCristal(0);
        Assert.AreEqual(2, puntos.GetPuntos(0));
    }

    [Test]
    public void Obstaculo_Suma1Punto()
    {
        puntos.SumarObstaculos(1, 1);
        Assert.AreEqual(1, puntos.GetPuntos(1));
    }

    [Test]
    public void TresObstaculos_Suman3Puntos()
    {
        puntos.SumarObstaculos(2, 3);
        Assert.AreEqual(3, puntos.GetPuntos(2));
    }

    [Test]
    public void EliminarAlPrimero_Da10()
    {
        int[] lista = { 30, 15, 5, 0 };
        Assert.AreEqual(10, PuntosAD.PuntosPorEliminar(lista, 0));
    }

    [Test]
    public void EliminarAlSegundo_Da6()
    {
        int[] lista = { 30, 15, 5, 0 };
        Assert.AreEqual(6, PuntosAD.PuntosPorEliminar(lista, 1));
    }

    [Test]
    public void EliminarAlTercero_Da4()
    {
        int[] lista = { 30, 15, 5, 0 };
        Assert.AreEqual(4, PuntosAD.PuntosPorEliminar(lista, 2));
    }

    [Test]
    public void AlEmpezarTodosA0_EliminarDa4()
    {
        int[] lista = { 0, 0, 0, 0 };
        Assert.AreEqual(4, PuntosAD.PuntosPorEliminar(lista, 0));
    }

    [Test]
    public void EliminarseAUnoMismo_NoDaPuntos()
    {
        puntos.SumarEliminacion(0, 0);
        Assert.AreEqual(0, puntos.GetPuntos(0));
    }

    [Test]
    public void Tabla_OrdenaPorPuntos()
    {
        int[] lista = { 5, 15, 30, 0 };
        var orden = TablaPuntuaciones.Ordenar(lista);
        Assert.AreEqual(new[] { 2, 1, 0, 3 }, orden.ToArray());
    }

    [Test]
    public void Tabla_EmpateCompartePuesto()
    {
        int[] lista = { 10, 10, 0, 0 };
        var orden = TablaPuntuaciones.Ordenar(lista);
        int[] puestos = TablaPuntuaciones.Puestos(lista, orden);
        Assert.AreEqual(new[] { 1, 1, 3, 3 }, puestos);
    }
}
