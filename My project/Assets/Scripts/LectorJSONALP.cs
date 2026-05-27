using UnityEngine;
using System.IO;
using System.Collections.Generic;

// ── Clases de datos ───────────────────────────────────────────────────────────

[System.Serializable] public class DatosALP
{
    public ParamsGA            params_ga;
    public float               mejor_costo;
    public List<HistGen>       historia;
    public List<DetallePlan>   plan_detalle;
    public List<DatosAeronave> aeronaves;
    public List<SnapshotData>  snapshots;
    [System.NonSerialized] public List<string> mejor_secuencia = new List<string>();
}

[System.Serializable] public class ParamsGA
{
    public int pop_size; public int n_generations;
    public float crossover_rate; public float mutation_rate; public int elite_size;
}

[System.Serializable] public class HistGen
{
    public int generacion; public float mejor_costo; public float costo_promedio;
}

[System.Serializable] public class DetallePlan
{
    public int posicion; public string id; public string tipo;
    public int tiempo_objetivo; public int tiempo_aterrizaje;
    public int desviacion_seg; public bool ventana_ok;
}

[System.Serializable] public class DatosAeronave
{
    public string id; public string tipo;
    public int earliest; public int objetivo; public int latest;
}

[System.Serializable] public class SnapshotData
{
    public int               generacion;
    public float             mejor_costo;
    public float             avg_cost;
    // Estas dos listas se llenan manualmente (JsonUtility no las deserializa anidadas)
    [System.NonSerialized] public List<string>      secuencia    = new List<string>();
    [System.NonSerialized] public List<DetallePlan> plan_detalle = new List<DetallePlan>();
}

// ── Lector ────────────────────────────────────────────────────────────────────

public class LectorJSONALP : MonoBehaviour
{
    public DatosALP Datos       { get; private set; }
    public bool     DatosListos { get; private set; } = false;

    public void CargarDatos(string rutaJSON)
    {
        DatosListos = false;
        if (!File.Exists(rutaJSON))
        {
            Debug.LogError($"[Lector] No existe: {rutaJSON}");
            return;
        }

        string json = File.ReadAllText(rutaJSON, System.Text.Encoding.UTF8)
                          .Replace("\"params\":", "\"params_ga\":");

        // Deserialización base (JsonUtility)
        Datos = JsonUtility.FromJson<DatosALP>(json);
        if (Datos == null)
        {
            Debug.LogError("[Lector] Error al parsear JSON.");
            return;
        }

        // Extraer mejor_secuencia manualmente
        Datos.mejor_secuencia = ExtraerArrayStrings(json, "mejor_secuencia", 0);

        // Parsear plan_detalle y secuencia de cada snapshot manualmente
        if (Datos.snapshots != null && Datos.snapshots.Count > 0)
        {
            List<string> subJSONs = ExtraerSubJSONsDeArray(json, "snapshots");
            for (int i = 0; i < Datos.snapshots.Count && i < subJSONs.Count; i++)
            {
                string sj = subJSONs[i];
                Datos.snapshots[i].secuencia    = ExtraerArrayStrings(sj, "secuencia", 0);
                Datos.snapshots[i].plan_detalle = ExtraerPlanDetalle(sj);
            }
        }

        DatosListos = true;
        Debug.Log($"[Lector] OK — costo: {Datos.mejor_costo} — " +
                  $"secuencia: {string.Join(", ", Datos.mejor_secuencia)} — " +
                  $"snapshots: {Datos.snapshots?.Count ?? 0}");

        // Debug snapshots
        if (Datos.snapshots != null)
        {
            foreach (var s in Datos.snapshots)
                Debug.Log($"[Lector] Snapshot gen={s.generacion} " +
                          $"plan={s.plan_detalle?.Count ?? 0} items " +
                          $"seq={s.secuencia?.Count ?? 0} ids");
        }
    }

    // ── Extrae todos los sub-JSON de un array JSON por nombre de campo ────────
    private List<string> ExtraerSubJSONsDeArray(string json, string campo)
    {
        var resultado = new List<string>();
        int campoIdx = json.IndexOf($"\"{campo}\"");
        if (campoIdx < 0) return resultado;

        int arrStart = json.IndexOf('[', campoIdx);
        if (arrStart < 0) return resultado;

        int i = arrStart + 1;
        int depth = 0;
        int objStart = -1;

        while (i < json.Length)
        {
            char c = json[i];
            if (c == '{')
            {
                if (depth == 0) objStart = i;
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0 && objStart >= 0)
                {
                    resultado.Add(json.Substring(objStart, i - objStart + 1));
                    objStart = -1;
                }
                else if (depth < 0) break; // salimos del array
            }
            i++;
        }
        return resultado;
    }

    // ── Extrae un array de strings por nombre de campo dentro de un JSON ──────
    private List<string> ExtraerArrayStrings(string json, string campo, int desde)
    {
        var result = new List<string>();
        int idx = json.IndexOf($"\"{campo}\"", desde);
        if (idx < 0) return result;

        int start = json.IndexOf('[', idx);
        int end   = json.IndexOf(']', start);
        if (start < 0 || end < 0) return result;

        string contenido = json.Substring(start + 1, end - start - 1);
        foreach (string parte in contenido.Split(','))
        {
            string limpio = parte.Trim().Trim('"');
            if (!string.IsNullOrEmpty(limpio)) result.Add(limpio);
        }
        return result;
    }

    // ── Extrae la lista de DetallePlan de un sub-JSON de snapshot ─────────────
    private List<DetallePlan> ExtraerPlanDetalle(string snapJSON)
    {
        var resultado = new List<DetallePlan>();

        List<string> objetos = ExtraerSubJSONsDeArray(snapJSON, "plan_detalle");
        foreach (string obj in objetos)
        {
            var d = new DetallePlan();
            d.posicion         = LeerInt(obj,    "posicion");
            d.id               = LeerString(obj, "id");
            d.tipo             = LeerString(obj, "tipo");
            d.tiempo_objetivo  = LeerInt(obj,    "tiempo_objetivo");
            d.tiempo_aterrizaje= LeerInt(obj,    "tiempo_aterrizaje");
            d.desviacion_seg   = LeerInt(obj,    "desviacion_seg");
            d.ventana_ok       = LeerBool(obj,   "ventana_ok");
            resultado.Add(d);
        }
        return resultado;
    }

    // ── Helpers de lectura de valores primitivos ──────────────────────────────
    private int LeerInt(string json, string campo)
    {
        int idx = json.IndexOf($"\"{campo}\"");
        if (idx < 0) return 0;
        int colon = json.IndexOf(':', idx);
        if (colon < 0) return 0;
        int start = colon + 1;
        while (start < json.Length && (json[start] == ' ' || json[start] == '\n' || json[start] == '\r')) start++;
        int end = start;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
        if (int.TryParse(json.Substring(start, end - start), out int v)) return v;
        return 0;
    }

    private string LeerString(string json, string campo)
    {
        int idx = json.IndexOf($"\"{campo}\"");
        if (idx < 0) return "";
        int colon = json.IndexOf(':', idx);
        if (colon < 0) return "";
        int q1 = json.IndexOf('"', colon + 1);
        if (q1 < 0) return "";
        int q2 = json.IndexOf('"', q1 + 1);
        if (q2 < 0) return "";
        return json.Substring(q1 + 1, q2 - q1 - 1);
    }

    private bool LeerBool(string json, string campo)
    {
        int idx = json.IndexOf($"\"{campo}\"");
        if (idx < 0) return false;
        int colon = json.IndexOf(':', idx);
        if (colon < 0) return false;
        int start = colon + 1;
        while (start < json.Length && json[start] == ' ') start++;
        return json.Length > start + 3 && json.Substring(start, 4) == "true";
    }
}