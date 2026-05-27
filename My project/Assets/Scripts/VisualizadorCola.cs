using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;   // ← FALTABA ESTO

public class VisualizadorCola : MonoBehaviour
{
    [SerializeField] private LectorJSONALP lector;
    [SerializeField] private RectTransform contenedor;
    [SerializeField] private PanelInfoALP  panelInfo;

    // ── Colores de tipo ──────────────────────────────────────────────────────
    private static readonly Color ColH       = new Color(0.22f, 0.48f, 0.90f);
    private static readonly Color ColM       = new Color(0.88f, 0.68f, 0.10f);
    private static readonly Color ColL       = new Color(0.18f, 0.72f, 0.42f);
    private static readonly Color ColViolado = new Color(0.75f, 0.18f, 0.18f);

    private static readonly Color BgOscuro  = new Color(0.11f, 0.12f, 0.15f);
    private static readonly Color TextoPrim  = new Color(0.95f, 0.95f, 0.97f);
    private static readonly Color TextoSec   = new Color(0.60f, 0.63f, 0.68f);
    private static readonly Color BarraPos   = new Color(0.25f, 0.75f, 0.42f, 0.85f);
    private static readonly Color BarraNeg   = new Color(0.90f, 0.55f, 0.12f, 0.85f);

    private const float ALTURA_TARJETA = 82f;
    private const float ESPACIADO      = 4f;
    private const float MAX_DESV_BARRA = 600f;

    private Font fuenteBase;

    void Awake() => fuenteBase = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    // ── Entrada principal ────────────────────────────────────────────────────
    public void DibujarCola()
    {
        if (!lector.DatosListos) return;
        DibujarColaDesdeSnapshot(lector.Datos.plan_detalle);
    }

    /// <summary>Dibuja la cola usando el plan de un snapshot específico.</summary>
    public void DibujarColaDesdeSnapshot(List<DetallePlan> plan)
    {
        if (plan == null) return;
        foreach (Transform h in contenedor) Destroy(h.gameObject);

        float totalH = plan.Count * (ALTURA_TARJETA + ESPACIADO);
        contenedor.sizeDelta = new Vector2(contenedor.sizeDelta.x, totalH);

        for (int i = 0; i < plan.Count; i++)
            CrearTarjeta(plan[i], i);
    }

    // ── Construcción de tarjeta ──────────────────────────────────────────────
    void CrearTarjeta(DetallePlan av, int idx)
    {
        Color acento = av.ventana_ok ? ColorTipo(av.tipo) : ColViolado;

        // Raíz
        GameObject go = new GameObject($"Tarjeta_{av.id}");
        go.transform.SetParent(contenedor, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 1);
        rt.anchorMax        = new Vector2(1, 1);
        rt.pivot            = new Vector2(0.5f, 1);
        rt.sizeDelta        = new Vector2(0, ALTURA_TARJETA);
        rt.anchoredPosition = new Vector2(0, -(idx * (ALTURA_TARJETA + ESPACIADO)));

        Image fondoImg = go.AddComponent<Image>();
        fondoImg.color = BgOscuro;

        // Borde izquierdo de color
        CrearBordeIzq(go.transform, acento);

        // Botón
        Button btn = go.AddComponent<Button>();
        int pos = av.posicion;
        btn.onClick.AddListener(() => panelInfo?.MostrarDetalle(pos));
        btn.targetGraphic = fondoImg;
        ColorBlock cb = btn.colors;
        cb.normalColor      = BgOscuro;
        cb.highlightedColor = new Color(0.18f, 0.20f, 0.25f);
        cb.pressedColor     = new Color(0.14f, 0.15f, 0.18f);
        cb.selectedColor    = BgOscuro;
        btn.colors = cb;

        // Chip de posición
        CrearChipPosicion(go.transform, av.posicion, acento);

        float bodyLeft = 54f;
        string signo   = av.desviacion_seg >= 0 ? "+" : "";

        // ID
        Texto(go.transform, av.id,
              14, FontStyle.Bold, TextAnchor.UpperLeft, TextoPrim,
              new Vector2(0, 0.55f), new Vector2(0.55f, 1f),
              new Vector2(bodyLeft, 0), new Vector2(-6f, -8f));

        // Badge tipo
        CrearBadgeTipo(go.transform, TipoNombre(av.tipo), acento, bodyLeft);

        // Tiempos
        Texto(go.transform,
              $"Obj {av.tiempo_objetivo}s   Aterriza {av.tiempo_aterrizaje}s",
              11, FontStyle.Normal, TextAnchor.UpperLeft, TextoSec,
              new Vector2(0, 0.28f), new Vector2(1f, 0.55f),
              new Vector2(bodyLeft, 0), new Vector2(-6f, 0));

        // Barra de desviación
        CrearBarraDesviacion(go.transform, av.desviacion_seg, signo, bodyLeft);

        // Dot ventana
        CrearDotVentana(go.transform, av.ventana_ok);
    }

    void CrearBordeIzq(Transform padre, Color color)
    {
        GameObject b = new GameObject("BordeIzq");
        b.transform.SetParent(padre, false);
        b.AddComponent<Image>().color = color;
        RectTransform r = b.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = new Vector2(0, 1);
        r.offsetMin = Vector2.zero; r.offsetMax = new Vector2(5, 0);
    }

    void CrearChipPosicion(Transform padre, int posicion, Color color)
    {
        GameObject chip = new GameObject("ChipPos");
        chip.transform.SetParent(padre, false);
        chip.AddComponent<Image>().color = new Color(color.r, color.g, color.b, 0.22f);
        RectTransform r = chip.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0, 0.5f);
        r.pivot     = new Vector2(0, 0.5f);
        r.sizeDelta = new Vector2(36, 36);
        r.anchoredPosition = new Vector2(10, 0);
        Texto(chip.transform, $"{posicion}",
              13, FontStyle.Bold, TextAnchor.MiddleCenter, color,
              Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    void CrearBadgeTipo(Transform padre, string nombre, Color acento, float bodyLeft)
    {
        GameObject bg = new GameObject("BadgeTipo");
        bg.transform.SetParent(padre, false);
        bg.AddComponent<Image>().color = new Color(acento.r, acento.g, acento.b, 0.20f);
        RectTransform r = bg.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.55f, 0.55f);
        r.anchorMax = new Vector2(1f,    1f);
        r.offsetMin = new Vector2(0,  -8f);
        r.offsetMax = new Vector2(-8f, -8f);
        Texto(bg.transform, nombre,
              11, FontStyle.Bold, TextAnchor.MiddleCenter, acento,
              Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    void CrearBarraDesviacion(Transform padre, int desviacion, string signo, float bodyLeft)
    {
        GameObject fondoBarra = new GameObject("BarraFondo");
        fondoBarra.transform.SetParent(padre, false);
        fondoBarra.AddComponent<Image>().color = new Color(1, 1, 1, 0.06f);
        RectTransform rf = fondoBarra.GetComponent<RectTransform>();
        rf.anchorMin = new Vector2(0, 0);
        rf.anchorMax = new Vector2(1, 0.28f);
        rf.offsetMin = new Vector2(bodyLeft, 6);
        rf.offsetMax = new Vector2(-6, 0);

        float pct = Mathf.Clamp01(Mathf.Abs(desviacion) / MAX_DESV_BARRA);
        if (pct > 0.005f)
        {
            GameObject relleno = new GameObject("BarraRelleno");
            relleno.transform.SetParent(fondoBarra.transform, false);
            relleno.AddComponent<Image>().color = desviacion >= 0 ? BarraPos : BarraNeg;
            RectTransform rr = relleno.GetComponent<RectTransform>();
            rr.anchorMin = Vector2.zero;
            rr.anchorMax = new Vector2(pct, 1);
            rr.offsetMin = Vector2.zero;
            rr.offsetMax = Vector2.zero;
        }

        string label  = desviacion == 0 ? "En hora" : $"Δ {signo}{desviacion}s";
        Color colLabel = desviacion == 0
            ? new Color(0.4f, 0.9f, 0.5f)
            : (desviacion > 0 ? new Color(0.4f, 0.85f, 0.55f) : new Color(1f, 0.65f, 0.25f));

        Texto(fondoBarra.transform, label,
              10, FontStyle.Bold, TextAnchor.MiddleLeft, colLabel,
              Vector2.zero, Vector2.one,
              new Vector2(4, 0), new Vector2(-4, 0));
    }

    void CrearDotVentana(Transform padre, bool ok)
    {
        GameObject dot = new GameObject("DotVentana");
        dot.transform.SetParent(padre, false);
        dot.AddComponent<Image>().color = ok
            ? new Color(0.20f, 0.95f, 0.40f)
            : new Color(1.00f, 0.25f, 0.25f);
        RectTransform r = dot.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1, 1);
        r.sizeDelta        = new Vector2(8, 8);
        r.anchoredPosition = new Vector2(-6, -6);
    }

    void Texto(Transform padre, string contenido, int size, FontStyle style,
               TextAnchor anchor, Color color,
               Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax)
    {
        GameObject go = new GameObject("Txt");
        go.transform.SetParent(padre, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = offMin; rt.offsetMax = offMax;
        Text t = go.AddComponent<Text>();
        t.text      = contenido;
        t.fontSize  = size;
        t.fontStyle = style;
        t.alignment = anchor;
        t.color     = color;
        t.font      = fuenteBase;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    Color ColorTipo(string tipo) =>
        tipo == "H" ? ColH : tipo == "M" ? ColM : ColL;

    string TipoNombre(string tipo) =>
        tipo == "H" ? "Heavy" : tipo == "M" ? "Medium" : "Light";
}