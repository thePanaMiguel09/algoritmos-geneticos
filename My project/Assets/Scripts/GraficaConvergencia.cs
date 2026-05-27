using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GraficaConvergencia : MonoBehaviour
{
    [SerializeField] private LectorJSONALP lector;
    [SerializeField] private RawImage      rawImage;

    [Header("Stats (Inicio · Mejora · Mejor Final · Generaciones)")]
    [SerializeField] private Text textoInicio;
    [SerializeField] private Text textoMejora;
    [SerializeField] private Text textoMejorFinal;
    [SerializeField] private Text textoGeneraciones;

    [Header("Leyenda")]
    [SerializeField] private Image imagenVerde;
    [SerializeField] private Image imagenNaranja;
    [SerializeField] private Text  textoLeyendaVerde;
    [SerializeField] private Text  textoLeyendaNaranja;

    [SerializeField] private int anchoBitmap = 600;
    [SerializeField] private int altoBitmap  = 280;

    private readonly Color colorFondo    = new Color(0.05f, 0.07f, 0.10f);
    private readonly Color colorGrid     = new Color(0.13f, 0.16f, 0.22f);
    private readonly Color colorMejor    = new Color(0.15f, 0.90f, 0.45f);
    private readonly Color colorPromedio = new Color(0.95f, 0.62f, 0.10f);
    private readonly Color colorMarcador = new Color(1.00f, 1.00f, 1.00f, 0.85f);

    private Texture2D tex;

    // Guardamos los píxeles base (sin marcador) para poder redibujar solo el marcador
    private Color[] pixelesBase;
    private int totalGeneraciones = 0;
    private int margen = 20;

    // ────────────────────────────────────────────────────────────────────────
    void Start()
    {
        tex = new Texture2D(anchoBitmap, altoBitmap, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        if (rawImage) rawImage.texture = tex;

        if (imagenVerde)         imagenVerde.color        = colorMejor;
        if (imagenNaranja)       imagenNaranja.color      = colorPromedio;
        if (textoLeyendaVerde)   textoLeyendaVerde.text   = "Mejor";
        if (textoLeyendaNaranja) textoLeyendaNaranja.text = "Promedio";
    }

    // ────────────────────────────────────────────────────────────────────────
    public void DibujarGrafica()
    {
        if (!lector.DatosListos || lector.Datos.historia == null) return;
        var h = lector.Datos.historia;
        int n = h.Count;
        if (n == 0) return;

        totalGeneraciones = n;

        float yMax = float.MinValue, yMin = float.MaxValue;
        foreach (var g in h)
        {
            if (g.costo_promedio > yMax) yMax = g.costo_promedio;
            if (g.mejor_costo    < yMin) yMin = g.mejor_costo;
        }
        float margenV = (yMax - yMin) * 0.10f;
        yMax += margenV;
        yMin  = Mathf.Max(0, yMin - margenV);
        float rango = Mathf.Max(yMax - yMin, 1f);

        // Fondo
        Color[] px = new Color[anchoBitmap * altoBitmap];
        for (int i = 0; i < px.Length; i++) px[i] = colorFondo;
        tex.SetPixels(px);

        // Grid horizontal
        for (int gi = 1; gi <= 4; gi++)
        {
            int gy = margen + (int)((float)gi / 5f * (altoBitmap - margen * 2));
            for (int x = margen; x < anchoBitmap - margen; x++) SetPx(x, gy, colorGrid);
        }
        // Grid vertical
        for (int gi = 1; gi <= 5; gi++)
        {
            int gx = margen + (int)((float)gi / 6f * (anchoBitmap - margen * 2));
            for (int y = margen; y < altoBitmap - margen; y++) SetPx(gx, y, colorGrid);
        }

        // Relleno verde bajo curva mejor
        Color relleno = new Color(0.05f, 0.30f, 0.12f);
        for (int i = 0; i < n; i++)
        {
            int x  = X(i, n);
            int yC = Y(h[i].mejor_costo, yMin, rango);
            for (int yy = margen; yy <= Mathf.Min(yC, altoBitmap - margen); yy++)
                SetPx(x, yy, relleno);
        }

        // Curva promedio (naranja)
        for (int i = 0; i < n - 1; i++)
        {
            Linea(X(i, n),   Y(h[i].costo_promedio,   yMin, rango),
                  X(i+1, n), Y(h[i+1].costo_promedio, yMin, rango), colorPromedio);
        }

        // Curva mejor (verde, grosor 2)
        for (int i = 0; i < n - 1; i++)
        {
            int x0=X(i,n), y0=Y(h[i].mejor_costo,yMin,rango);
            int x1=X(i+1,n), y1=Y(h[i+1].mejor_costo,yMin,rango);
            Linea(x0, y0,   x1, y1,   colorMejor);
            Linea(x0, y0+1, x1, y1+1, colorMejor);
        }

        // Punto final mejor
        int xF=X(n-1,n), yF=Y(h[n-1].mejor_costo,yMin,rango);
        for (int dx=-4;dx<=4;dx++) for (int dy=-4;dy<=4;dy++)
            if (dx*dx+dy*dy<=16) SetPx(xF+dx,yF+dy,colorMejor);

        // Punto final promedio
        int xFP=X(n-1,n), yFP=Y(h[n-1].costo_promedio,yMin,rango);
        for (int dx=-3;dx<=3;dx++) for (int dy=-3;dy<=3;dy++)
            if (dx*dx+dy*dy<=9) SetPx(xFP+dx,yFP+dy,colorPromedio);

        tex.Apply();

        // Guardamos copia base para poder dibujar marcador encima sin recalcular todo
        pixelesBase = tex.GetPixels();

        // Stats
        float inicio     = h[0].mejor_costo;
        float mejorFinal = h[n-1].mejor_costo;
        float pctMejora  = (inicio - mejorFinal) / Mathf.Max(inicio, 1f) * 100f;

        if (textoInicio)       textoInicio.text      = $"INICIO\n{inicio:F0}";
        if (textoMejora)     { textoMejora.text       = $"MEJORA\n-{pctMejora:F1}%";
                               textoMejora.color      = new Color(0.2f,0.9f,0.4f); }
        if (textoMejorFinal)   textoMejorFinal.text   = $"MEJOR FINAL\n{mejorFinal:F0}";
        if (textoGeneraciones) textoGeneraciones.text = $"{n} generaciones";
    }

    // ── Marcador de generación actual ─────────────────────────────────────────
    /// <summary>
    /// Dibuja una línea vertical blanca en la generación indicada.
    /// Llamar desde VisorGeneraciones al cambiar de snapshot.
    /// </summary>
    public void MarcarGeneracion(int generacion)
    {
        if (pixelesBase == null || totalGeneraciones == 0) return;

        // Restaurar píxeles base (borra marcador anterior)
        tex.SetPixels(pixelesBase);

        // Calcular X del marcador
        int xMarca = X(generacion, totalGeneraciones);

        // Línea vertical punteada blanca
        for (int y = margen; y < altoBitmap - margen; y++)
        {
            // Punteado: píxel sí, píxel no
            if ((y / 3) % 2 == 0)
                SetPx(xMarca, y, colorMarcador);
        }

        // Pequeño rombo en la parte superior del marcador
        for (int dx = -3; dx <= 3; dx++)
            for (int dy = -3; dy <= 3; dy++)
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 3)
                    SetPx(xMarca + dx, altoBitmap - margen - 4 + dy, colorMarcador);

        tex.Apply();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    int X(int i, int n) =>
        margen + (int)((float)i / Mathf.Max(n-1, 1) * (anchoBitmap - margen*2));

    int Y(float val, float yMin, float rango) =>
        margen + (int)(((val - yMin) / rango) * (altoBitmap - margen*2));

    void Linea(int x0, int y0, int x1, int y1, Color c)
    {
        int dx=Mathf.Abs(x1-x0), dy=Mathf.Abs(y1-y0);
        int sx=x0<x1?1:-1, sy=y0<y1?1:-1, err=dx-dy;
        while(true)
        {
            SetPx(x0,y0,c);
            if(x0==x1&&y0==y1) break;
            int e2=2*err;
            if(e2>-dy){err-=dy;x0+=sx;}
            if(e2< dx){err+=dx;y0+=sy;}
        }
    }

    void SetPx(int x, int y, Color c)
    {
        if(x>=0&&x<anchoBitmap&&y>=0&&y<altoBitmap)
            tex.SetPixel(x,y,c);
    }
}