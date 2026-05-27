using UnityEngine;
using UnityEngine.UI;

public class PanelInfoALP : MonoBehaviour
{
    [Header("Lector")]
    [SerializeField] private LectorJSONALP lector;

    [Header("Resumen")]
    [SerializeField] private Text textoMejorCosto;
    [SerializeField] private Text textoSecuencia;
    [SerializeField] private Text textoParams;

    [Header("Detalle aeronave")]
    [SerializeField] private GameObject panelDetalle;
    [SerializeField] private Text textoID;
    [SerializeField] private Text textoTipo;
    [SerializeField] private Text textoPosicion;
    [SerializeField] private Text textoTiempoObj;
    [SerializeField] private Text textoTiempoReal;
    [SerializeField] private Text textoDesviacion;
    [SerializeField] private Text textoVentana;

    void Start() { if (panelDetalle) panelDetalle.SetActive(false); }

    public void MostrarResumen()
    {
        if (!lector.DatosListos) return;
        var d = lector.Datos;

        Set(textoMejorCosto, $"Mejor costo: {d.mejor_costo:F2}");

        if (d.mejor_secuencia != null && d.mejor_secuencia.Count > 0)
        {
            // Mostrar como lista numerada: 1.A03  2.A06  3.A01 ...
            var lineas = new System.Text.StringBuilder("Secuencia optima:\n");
            for (int i = 0; i < d.mejor_secuencia.Count; i++)
                lineas.Append($"{i+1}.{d.mejor_secuencia[i]}  ");
            Set(textoSecuencia, lineas.ToString().Trim());
        }
        else
            Set(textoSecuencia, "Secuencia: (sin datos)");

        if (d.params_ga != null)
            Set(textoParams,
                $"Pob:{d.params_ga.pop_size}  Gen:{d.params_ga.n_generations}  " +
                $"Cx:{d.params_ga.crossover_rate:F2}  Mut:{d.params_ga.mutation_rate:F2}  " +
                $"Elites:{d.params_ga.elite_size}");
    }

    public void MostrarDetalle(int posicion)
    {
        if (!lector.DatosListos) return;
        var plan = lector.Datos.plan_detalle;
        if (posicion < 1 || posicion > plan.Count) return;
        var av = plan[posicion - 1];

        if (panelDetalle) panelDetalle.SetActive(true);

        string signo = av.desviacion_seg >= 0 ? "+" : "";
        string tipo  = av.tipo == "H" ? "Heavy" : av.tipo == "M" ? "Medium" : "Light";
        bool   ok    = av.ventana_ok;
        Color  c     = ok ? new Color(0.2f,0.85f,0.3f) : new Color(0.9f,0.2f,0.2f);

        Set(textoID,         $"{av.id}");
        Set(textoTipo,       $"Tipo: {tipo}");
        Set(textoPosicion,   $"Posicion #{av.posicion} en la cola");
        Set(textoTiempoObj,  $"Tiempo objetivo:   {av.tiempo_objetivo}s");
        Set(textoTiempoReal, $"Tiempo aterrizaje: {av.tiempo_aterrizaje}s");
        Set(textoDesviacion, $"Desviacion: {signo}{av.desviacion_seg}s");
        Set(textoVentana,    ok ? "Ventana cumplida" : "Ventana VIOLADA");
        if (textoVentana) textoVentana.color = c;
    }

    public void Resetear()
    {
        if (panelDetalle) panelDetalle.SetActive(false);
        Set(textoMejorCosto, ""); Set(textoSecuencia, ""); Set(textoParams, "");
    }

    void Set(Text t, string v) { if (t != null) t.text = v; }
}