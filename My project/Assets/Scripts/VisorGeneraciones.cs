using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VisorGeneraciones : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private LectorJSONALP      lector;
    [SerializeField] private VisualizadorCola   vizCola;
    [SerializeField] private GraficaConvergencia graficaConv;  // ← NUEVA

    [Header("Controles UI")]
    [SerializeField] private Button     btnAnterior;
    [SerializeField] private Button     btnSiguiente;
    [SerializeField] private Button     btnFinal;
    [SerializeField] private Slider     sliderGeneracion;
    [SerializeField] private Text       textoGenActual;
    [SerializeField] private Text       textoCostoActual;
    [SerializeField] private Text       textoModo;
    [SerializeField] private GameObject panelVisor;

    private List<SnapshotData> snapshots = new List<SnapshotData>();
    private int indiceActual = 0;

    // ────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (btnAnterior)      btnAnterior.onClick.AddListener(Anterior);
        if (btnSiguiente)     btnSiguiente.onClick.AddListener(Siguiente);
        if (btnFinal)         btnFinal.onClick.AddListener(IrAlFinal);
        if (sliderGeneracion) sliderGeneracion.onValueChanged.AddListener(v => IrAIndice((int)v));
        ActualizarBotones();
    }

    // ── API pública ───────────────────────────────────────────────────────────
    public void CargarSnapshots()
    {
        if (!lector.DatosListos)
        {
            Debug.LogWarning("[VisorGen] Datos no listos.");
            return;
        }

        snapshots.Clear();
        var raw = lector.Datos.snapshots;

        if (raw == null || raw.Count == 0)
        {
            Debug.LogWarning("[VisorGen] No hay snapshots en el JSON.");
            Set(textoModo, "Sin snapshots");
            ActualizarBotones();
            return;
        }

        foreach (var s in raw) snapshots.Add(s);
        indiceActual = 0;

        if (sliderGeneracion)
        {
            sliderGeneracion.minValue     = 0;
            sliderGeneracion.maxValue     = snapshots.Count - 1;
            sliderGeneracion.wholeNumbers = true;
            sliderGeneracion.SetValueWithoutNotify(0);
        }

        if (panelVisor) panelVisor.SetActive(true);

        MostrarSnapshot(0);
        Debug.Log($"[VisorGen] {snapshots.Count} snapshots cargados.");
    }

    // ── Navegación ────────────────────────────────────────────────────────────
    public void Anterior() { if (indiceActual > 0) MostrarSnapshot(indiceActual - 1); }
    public void Siguiente() { if (indiceActual < snapshots.Count - 1) MostrarSnapshot(indiceActual + 1); }
    public void IrAlFinal() { if (snapshots.Count > 0) MostrarSnapshot(snapshots.Count - 1); }

    void IrAIndice(int idx)
    {
        if (idx >= 0 && idx < snapshots.Count && idx != indiceActual)
            MostrarSnapshot(idx);
    }

    // ── Mostrar snapshot ──────────────────────────────────────────────────────
    void MostrarSnapshot(int idx)
    {
        if (snapshots.Count == 0) return;
        indiceActual = Mathf.Clamp(idx, 0, snapshots.Count - 1);
        var snap = snapshots[indiceActual];

        // Textos
        Set(textoGenActual,   $"Gen {snap.generacion}  ({indiceActual + 1}/{snapshots.Count})");
        Set(textoCostoActual, $"Costo: {snap.mejor_costo:F0}");
        Set(textoModo,
            indiceActual == 0                    ? "POBLACIÓN INICIAL" :
            indiceActual == snapshots.Count - 1  ? "SOLUCIÓN FINAL"    : "EVOLUCIÓN");

        // Slider sin disparar listener
        if (sliderGeneracion) sliderGeneracion.SetValueWithoutNotify(indiceActual);

        ActualizarBotones();

        // Cola
        if (vizCola != null && snap.plan_detalle != null)
            vizCola.DibujarColaDesdeSnapshot(snap.plan_detalle);

        // Marcador en la gráfica
        if (graficaConv != null)
            graficaConv.MarcarGeneracion(snap.generacion);
    }

    void ActualizarBotones()
    {
        bool hay = snapshots.Count > 0;
        if (btnAnterior)  btnAnterior.interactable  = hay && indiceActual > 0;
        if (btnSiguiente) btnSiguiente.interactable = hay && indiceActual < snapshots.Count - 1;
        if (btnFinal)     btnFinal.interactable     = hay && indiceActual < snapshots.Count - 1;
    }

    void Set(Text t, string v) { if (t != null) t.text = v; }
}