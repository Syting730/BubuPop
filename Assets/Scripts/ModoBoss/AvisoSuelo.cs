using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Aviso en el suelo (modo Boss Volcán)
// Antes de cada ataque del Boss (fuego, erupción...) las casillas que
// van a recibir el ataque se ponen rojas durante 1 segundo.
//
// Cómo lo usa un ataque:
//     yield return aviso.Avisar(casillas);   // suelo en rojo 1 s
//     ... hacer el daño en esas casillas ...
//     aviso.QuitarAviso(casillas);            // termina el ataque
//
// Va en un objeto vacío de la escena del Boss (por ejemplo "AvisoSuelo").
public class AvisoSuelo : MonoBehaviour
{
    [Header("Aspecto")]
    [Tooltip("Cuadrado que se pinta en cada casilla (vale el prefab ExplosionWarning)")]
    [SerializeField] private GameObject prefabAviso;
    [SerializeField] private Color color = new Color(1f, 0f, 0f, 0.6f);

    [Header("Tiempo")]
    [Tooltip("Segundos que el suelo está en rojo antes del ataque")]
    [SerializeField] private float duracion = 1f;

    [Header("Mapa (si se deja vacío, se busca solo)")]
    [SerializeField] private Grid grid;

    // Cuántos avisos tiene cada casilla ahora mismo
    // (puede haber dos ataques avisando la misma casilla a la vez)
    private Dictionary<Vector3Int, int> avisadas = new Dictionary<Vector3Int, int>();

    public float Duracion
    {
        get { return duracion; }
    }

    private void Awake()
    {
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();
    }

    // ¿Esta casilla está avisada? Los ataques solo hacen daño aquí
    public bool EstaAvisada(Vector3Int casilla)
    {
        return avisadas.ContainsKey(casilla);
    }

    // Pone las casillas en rojo y espera 'duracion' segundos.
    // Las casillas siguen contando como avisadas hasta QuitarAviso.
    public IEnumerator Avisar(List<Vector3Int> casillas)
    {
        List<GameObject> cuadros = new List<GameObject>();

        foreach (Vector3Int c in casillas)
        {
            if (avisadas.ContainsKey(c))
                avisadas[c]++;
            else
                avisadas[c] = 1;

            GameObject cuadro = CrearCuadro(c);
            if (cuadro != null)
                cuadros.Add(cuadro);
        }

        yield return new WaitForSeconds(duracion);

        // Se quita el rojo; el ataque llega justo ahora
        foreach (GameObject cuadro in cuadros)
        {
            if (cuadro != null)
                Destroy(cuadro);
        }
    }

    // El ataque ha terminado: las casillas dejan de estar avisadas
    public void QuitarAviso(List<Vector3Int> casillas)
    {
        foreach (Vector3Int c in casillas)
        {
            if (!avisadas.ContainsKey(c))
                continue;

            avisadas[c]--;
            if (avisadas[c] <= 0)
                avisadas.Remove(c);
        }
    }

    private GameObject CrearCuadro(Vector3Int casilla)
    {
        if (prefabAviso == null)
        {
            Debug.LogWarning("AvisoSuelo: falta asignar Prefab Aviso.");
            return null;
        }

        Vector3 posicion = grid.GetCellCenterWorld(casilla);
        posicion.z = 0f;

        GameObject cuadro = Instantiate(prefabAviso, posicion, Quaternion.identity);

        // Del tamaño de la casilla (igual que hace Boom)
        cuadro.transform.localScale = new Vector3(grid.cellSize.x, grid.cellSize.y, 1f);

        // Pintarlo de rojo
        SpriteRenderer sr = cuadro.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.color = color;

        return cuadro;
    }
}
