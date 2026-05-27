using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using System.IO;
using System.Collections;

public class ConectorPythonALP : MonoBehaviour
{
    [Header("Scripts")]
    [SerializeField] private LectorJSONALP      lector;
    [SerializeField] private VisorGeneraciones  visorGeneraciones;
    [SerializeField] private VisualizadorCola   vizCola;
    [SerializeField] private GraficaConvergencia graficaConv;
    [SerializeField] private PanelInfoALP        panelInfo;

    [Header("Sliders")]
    [SerializeField] private Slider sliderPoblacion;
    [SerializeField] private Slider sliderGeneraciones;
    [SerializeField] private Slider sliderCruzamiento;
    [SerializeField] private Slider sliderMutacion;
    [SerializeField] private Slider sliderElitismo;

    [Header("Textos de valor")]
    [SerializeField] private Text textoPoblacion;
    [SerializeField] private Text textoGeneraciones;
    [SerializeField] private Text textoCruzamiento;
    [SerializeField] private Text textoMutacion;
    [SerializeField] private Text textoElitismo;
    [SerializeField] private Text textoEstado;

    [Header("Python")]
    [SerializeField] private string rutaPython = "python";
    [SerializeField] private string rutaScript = "";

    private bool ejecutando = false;

    void Start()
    {
        if (string.IsNullOrEmpty(rutaScript))
        {
            rutaScript = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "..", "main.py"));
        }

        sliderPoblacion.onValueChanged.AddListener(v =>
            textoPoblacion.text = $"Población: {(int)v}");
        sliderGeneraciones.onValueChanged.AddListener(v =>
            textoGeneraciones.text = $"Generaciones: {(int)v}");
        sliderCruzamiento.onValueChanged.AddListener(v =>
            textoCruzamiento.text = $"Cruzamiento: {v:F2}");
        sliderMutacion.onValueChanged.AddListener(v =>
            textoMutacion.text = $"Mutación: {v:F2}");
        sliderElitismo.onValueChanged.AddListener(v =>
            textoElitismo.text = $"Élites: {(int)v}");

        ActualizarTextos();
    }

    public void OnEjecutarClick()
    {
        if (ejecutando) { textoEstado.text = "Ya hay una ejecución en curso..."; return; }
        StartCoroutine(EjecutarPython());
    }

    private IEnumerator EjecutarPython()
    {
        ejecutando       = true;
        textoEstado.text = "Ejecutando GA...";

        int   pob  = (int)sliderPoblacion.value;
        int   gen  = (int)sliderGeneraciones.value;
        float cx   = sliderCruzamiento.value;
        float mut  = sliderMutacion.value;
        int   elit = (int)sliderElitismo.value;

        string rutaJSON = Path.Combine(Application.streamingAssetsPath, "datos_alp.json");

        string args = $"\"{rutaScript}\" " +
                      $"--poblacion {pob} " +
                      $"--generaciones {gen} " +
                      $"--cruzamiento {cx.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} " +
                      $"--mutacion {mut.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} " +
                      $"--elitismo {elit} " +
                      $"--salida \"{rutaJSON}\"";

        ProcessStartInfo info = new ProcessStartInfo
        {
            FileName               = rutaPython,
            Arguments              = args,
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding  = System.Text.Encoding.UTF8
        };

        Process proceso = new Process { StartInfo = info };
        proceso.Start();

        float t = 0f;
        while (!proceso.HasExited)
        {
            t += 0.1f;
            textoEstado.text = $"Ejecutando GA... ({t:F0}s)";
            yield return new WaitForSeconds(0.1f);
        }

        string error = proceso.StandardError.ReadToEnd();
        if (proceso.ExitCode != 0)
        {
            UnityEngine.Debug.LogError($"[ALP] Error Python:\n{error}");
            textoEstado.text = "Error. Revisa la consola.";
            ejecutando = false;
            yield break;
        }

        yield return new WaitForSeconds(0.2f);

        lector.CargarDatos(rutaJSON);

        yield return new WaitForSeconds(0.1f);

        if (panelInfo  != null) { panelInfo.Resetear(); panelInfo.MostrarResumen(); }
        if (vizCola    != null) vizCola.DibujarCola();
        if (graficaConv != null) graficaConv.DibujarGrafica();
        if (visorGeneraciones != null) visorGeneraciones.CargarSnapshots();

        textoEstado.text = $"Listo — Pob:{pob} Gen:{gen} Cx:{cx:F2} Mut:{mut:F2} Elit:{elit}";
        ejecutando = false;
    }

    void ActualizarTextos()
    {
        textoPoblacion.text    = $"Población: {(int)sliderPoblacion.value}";
        textoGeneraciones.text = $"Generaciones: {(int)sliderGeneraciones.value}";
        textoCruzamiento.text  = $"Cruzamiento: {sliderCruzamiento.value:F2}";
        textoMutacion.text     = $"Mutación: {sliderMutacion.value:F2}";
        textoElitismo.text     = $"Élites: {(int)sliderElitismo.value}";
    }
}
