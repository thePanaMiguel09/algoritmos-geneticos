"""
main.py  —  Aircraft Landing Problem · Algoritmo Genético
==========================================================
Compatible con ConectorPython.cs de Unity.

Exporta snapshots de generaciones intermedias para que Unity
pueda navegar entre ellas y mostrar la evolución del algoritmo.

Snapshots exportados: generación 0 (inicial), cada 25 generaciones,
y la generación final con la mejor solución.
"""

import argparse
import json
import os
import sys

from alp_data import generate_scenario
from alp_fitness import fitness, fitness_summary
from alp_ga import run_ga, DEFAULT_PARAMS


SNAPSHOT_CADA = 25  # Capturar snapshot cada N generaciones


def parse_args():
    p = argparse.ArgumentParser()
    p.add_argument("--poblacion",    type=int,   default=DEFAULT_PARAMS["pop_size"])
    p.add_argument("--generaciones", type=int,   default=DEFAULT_PARAMS["n_generations"])
    p.add_argument("--cruzamiento",  type=float, default=DEFAULT_PARAMS["crossover_rate"])
    p.add_argument("--mutacion",     type=float, default=DEFAULT_PARAMS["mutation_rate"])
    p.add_argument("--elitismo",     type=int,   default=DEFAULT_PARAMS["elite_size"])
    p.add_argument("--torneo",       type=int,   default=DEFAULT_PARAMS["tournament_k"])
    p.add_argument("--salida",       type=str,   default="datos_alp.json")
    return p.parse_args()


def build_plan_detalle(planes, sequence_ids):
    """Construye el plan de aterrizaje detallado para una secuencia dada."""
    id_to_plane = {p.id: p for p in planes}
    sequence_objs = [id_to_plane[pid] for pid in sequence_ids]
    summary = fitness_summary(sequence_objs)
    plan = []
    for pos, detail in enumerate(summary["details"], start=1):
        plan.append({
            "posicion":          pos,
            "id":                detail["id"],
            "tipo":              detail["type"],
            "tiempo_objetivo":   detail["target"],
            "tiempo_aterrizaje": detail["landing_time"],
            "desviacion_seg":    detail["deviation_sec"],
            "ventana_ok":        detail["window_ok"],
        })
    return plan, summary["total_cost"]


def main():
    args = parse_args()

    params = {
        "pop_size":       args.poblacion,
        "n_generations":  args.generaciones,
        "crossover_rate": args.cruzamiento,
        "mutation_rate":  args.mutacion,
        "elite_size":     args.elitismo,
        "tournament_k":   args.torneo,
    }

    planes = generate_scenario()
    print(f"[ALP-GA] Iniciando con {len(planes)} aeronaves")
    print(f"[ALP-GA] Parametros: {params}")

    # Snapshots: guardar mejor secuencia en generaciones clave
    snapshots = []

    def progress(data):
        gen = data["generation"]
        if gen % 25 == 0:
            print(f"[ALP-GA] Gen {gen:>4d} | Mejor: {data['best_cost']:>10.2f} | Prom: {data['avg_cost']:>10.2f}")

        # Capturar snapshot en gen 0, cada SNAPSHOT_CADA, y última generación
        es_ultima = (gen == params["n_generations"] - 1)
        if gen == 0 or gen % SNAPSHOT_CADA == 0 or es_ultima:
            plan, costo = build_plan_detalle(planes, data["best_sequence"])
            snapshots.append({
                "generacion":    gen,
                "mejor_costo":   round(data["best_cost"], 2),
                "avg_cost":      round(data["avg_cost"], 2),
                "secuencia":     data["best_sequence"],
                "plan_detalle":  plan,
            })

    result = run_ga(planes, params=params, generation_callback=progress, seed=None)

    print(f"[ALP-GA] Completado. Mejor costo: {result['best_cost']}")
    print(f"[ALP-GA] Secuencia: {' -> '.join(result['best_sequence'])}")

    # Asegurar que el último snapshot es la solución final
    plan_final, _ = build_plan_detalle(planes, result["best_sequence"])
    if not snapshots or snapshots[-1]["generacion"] != params["n_generations"] - 1:
        snapshots.append({
            "generacion":   params["n_generations"] - 1,
            "mejor_costo":  result["best_cost"],
            "avg_cost":     snapshots[-1]["avg_cost"] if snapshots else 0,
            "secuencia":    result["best_sequence"],
            "plan_detalle": plan_final,
        })

    # Construir JSON de salida
    historia = [
        {"generacion": g, "mejor_costo": round(bc, 2), "costo_promedio": round(ac, 2)}
        for g, bc, ac in result["history"]
    ]

    aeronaves = [
        {"id": p.id, "tipo": p.aircraft_type,
         "earliest": p.earliest, "objetivo": p.target, "latest": p.latest}
        for p in planes
    ]

    output = {
        "params":           params,
        "mejor_secuencia":  result["best_sequence"],
        "mejor_costo":      result["best_cost"],
        "historia":         historia,
        "plan_detalle":     plan_final,
        "aeronaves":        aeronaves,
        "snapshots":        snapshots,
    }

    salida_dir = os.path.dirname(args.salida)
    if salida_dir and not os.path.exists(salida_dir):
        os.makedirs(salida_dir, exist_ok=True)

    with open(args.salida, "w", encoding="utf-8") as f:
        json.dump(output, f, ensure_ascii=False, indent=2)

    print(f"[ALP-GA] JSON escrito en: {args.salida}")
    print(f"[ALP-GA] Snapshots exportados: {len(snapshots)}")
    sys.exit(0)


if __name__ == "__main__":
    main()
