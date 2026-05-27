"""
alp_fitness.py
==============
Evaluación de la función fitness para el Aircraft Landing Problem.

Dado un cromosoma (permutación de aeronaves), calcula:
  1. El tiempo de aterrizaje de cada avión, respetando:
     - Ventana válida [earliest, latest]
     - Separación mínima con el avión anterior en la cola
  2. El costo total del plan:
     - Penalización por aterrizar antes del objetivo (combustible)
     - Penalización por aterrizar después del objetivo (retrasos)
     - Penalización fuerte por violar la ventana (solución inválida)

Un costo menor indica una mejor solución. El GA minimiza esta función.
"""

from typing import List, Dict, Tuple
from alp_data import Aircraft, get_separation, WINDOW_VIOLATION_PENALTY


def compute_landing_times(sequence: List[Aircraft]) -> List[int]:
    """
    Calcula determinísticamente los tiempos de aterrizaje para una secuencia.

    Regla: cada avión aterriza lo más cerca posible a su tiempo objetivo,
    pero no antes de su 'earliest' ni antes de que haya pasado la separación
    mínima desde el aterrizaje del avión anterior.

    Args:
        sequence: lista ordenada de Aircraft (el cromosoma decodificado).

    Returns:
        Lista de tiempos de aterrizaje (segundos) para cada posición.
    """
    times: List[int] = []
    for i, plane in enumerate(sequence):
        # Tiempo mínimo por la ventana de disponibilidad
        min_time = plane.earliest

        # Tiempo mínimo por separación con el avión anterior
        if i > 0:
            prev_plane = sequence[i - 1]
            sep = get_separation(prev_plane.aircraft_type, plane.aircraft_type)
            min_time = max(min_time, times[i - 1] + sep)

        # Aterrizar lo más cerca posible al objetivo, respetando el mínimo
        landing_time = max(min_time, plane.target)

        # Si el objetivo ya pasó (min_time > target), aterrizar en min_time
        if min_time > plane.target:
            landing_time = min_time

        times.append(landing_time)
    return times


def fitness(sequence: List[Aircraft]) -> Tuple[float, List[int]]:
    """
    Evalúa el costo total de un plan de aterrizaje.

    Args:
        sequence: permutación de aeronaves que define el orden de aterrizaje.

    Returns:
        Tupla (costo_total, lista_de_tiempos_de_aterrizaje).
        Menor costo = mejor solución.
    """
    times = compute_landing_times(sequence)
    total_cost = 0.0

    for plane, t in zip(sequence, times):
        # Costo por desviación del objetivo
        if t < plane.target:
            # Aterrizó antes: costo por combustible extra en espera circular
            total_cost += plane.penalty_early * (plane.target - t)
        else:
            # Aterrizó después: costo por retraso a pasajeros y conexiones
            total_cost += plane.penalty_late * (t - plane.target)

        # Penalización por violar la ventana de aterrizaje
        if t < plane.earliest or t > plane.latest:
            total_cost += WINDOW_VIOLATION_PENALTY

    return total_cost, times


def fitness_summary(sequence: List[Aircraft]) -> Dict:
    """
    Retorna un diccionario con el desglose del fitness para diagnóstico
    y para enviar a Unity vía WebSocket.
    """
    cost, times = fitness(sequence)
    details = []
    for plane, t in zip(sequence, times):
        deviation = t - plane.target
        window_ok = plane.earliest <= t <= plane.latest
        details.append({
            "id": plane.id,
            "type": plane.aircraft_type,
            "target": plane.target,
            "landing_time": t,
            "deviation_sec": deviation,
            "window_ok": window_ok,
        })
    return {
        "total_cost": round(cost, 2),
        "sequence_ids": [p.id for p in sequence],
        "details": details,
    }
