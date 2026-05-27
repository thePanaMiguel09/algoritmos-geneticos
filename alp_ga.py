"""
alp_ga.py
=========
Motor principal del Algoritmo Genético para el Aircraft Landing Problem.

Implementa el ciclo completo generación por generación:
  Inicialización → Evaluación → Selección → Cruzamiento → Mutación → Elitismo → …

Este módulo puede ejecutarse de forma autónoma (modo consola) o ser
importado por el servidor WebSocket (alp_server.py) para transmitir
cada generación a Unity en tiempo real.

Parámetros configurables:
  - POP_SIZE: tamaño de la población
  - N_GENERATIONS: número máximo de generaciones
  - CROSSOVER_RATE: probabilidad de cruzamiento (0.0–1.0)
  - MUTATION_RATE: probabilidad de mutación por individuo
  - ELITE_SIZE: número de élites conservados por generación
  - TOURNAMENT_K: tamaño del torneo de selección
"""

import random
from typing import List, Callable, Optional
from alp_data import Aircraft, generate_scenario
from alp_fitness import fitness
from alp_operators import tournament_selection, ox1_crossover, mutate, get_elite


# ─────────────────────────────────────────────────────────────
# Parámetros por defecto del AG
# ─────────────────────────────────────────────────────────────
DEFAULT_PARAMS = {
    "pop_size": 60,
    "n_generations": 200,
    "crossover_rate": 0.85,
    "mutation_rate": 0.15,
    "elite_size": 3,
    "tournament_k": 3,
}


# ─────────────────────────────────────────────────────────────
# Inicialización de la población
# ─────────────────────────────────────────────────────────────

def init_population(planes: List[Aircraft], pop_size: int) -> List[List[Aircraft]]:
    """
    Genera una población inicial de permutaciones aleatorias.

    Cada individuo es una permutación de las n aeronaves — cada avión
    aparece exactamente una vez en cada posición posible. La aleatoriedad
    garantiza cobertura diversa del espacio de búsqueda desde el inicio.

    Args:
        planes: lista de aeronaves del problema.
        pop_size: número de individuos en la población.

    Returns:
        Lista de pop_size permutaciones (cromosomas).
    """
    population = []
    for _ in range(pop_size):
        chromosome = planes[:]
        random.shuffle(chromosome)
        population.append(chromosome)
    return population


# ─────────────────────────────────────────────────────────────
# Evaluación de la población
# ─────────────────────────────────────────────────────────────

def evaluate_population(population: List[List[Aircraft]]) -> List[float]:
    """
    Evalúa el fitness de toda la población.

    Args:
        population: lista de cromosomas.

    Returns:
        Lista de costos (uno por individuo). Menor costo = mejor.
    """
    costs = []
    for chromosome in population:
        cost, _ = fitness(chromosome)
        costs.append(cost)
    return costs


# ─────────────────────────────────────────────────────────────
# Una generación del GA
# ─────────────────────────────────────────────────────────────

def next_generation(
    population: List[List[Aircraft]],
    costs: List[float],
    params: dict
) -> List[List[Aircraft]]:
    """
    Produce la siguiente generación aplicando selección, cruzamiento,
    mutación y elitismo.

    Flujo:
      1. Extraer élite (se añade directamente a la nueva generación).
      2. Seleccionar padres por torneo.
      3. Aplicar cruzamiento OX1 con probabilidad crossover_rate.
      4. Mutar cada hijo con probabilidad mutation_rate.
      5. Rellenar hasta alcanzar pop_size.

    Args:
        population: generación actual.
        costs: costos de la generación actual.
        params: diccionario con los parámetros del AG.

    Returns:
        Nueva generación de cromosomas.
    """
    pop_size = params["pop_size"]
    crossover_rate = params["crossover_rate"]
    mutation_rate = params["mutation_rate"]
    elite_size = params["elite_size"]
    tournament_k = params["tournament_k"]

    # Elitismo: los mejores individuos pasan sin cambios
    new_population = get_elite(population, costs, elite_size)

    # Generar el resto de la nueva población
    while len(new_population) < pop_size:
        parent1 = tournament_selection(population, costs, tournament_k)
        parent2 = tournament_selection(population, costs, tournament_k)

        # Cruzamiento con probabilidad crossover_rate
        if random.random() < crossover_rate:
            child1, child2 = ox1_crossover(parent1, parent2)
        else:
            child1, child2 = parent1[:], parent2[:]

        # Mutación de cada hijo
        child1 = mutate(child1, mutation_rate)
        child2 = mutate(child2, mutation_rate)

        new_population.append(child1)
        if len(new_population) < pop_size:
            new_population.append(child2)

    return new_population


# ─────────────────────────────────────────────────────────────
# Ciclo principal del GA
# ─────────────────────────────────────────────────────────────

def run_ga(
    planes: List[Aircraft],
    params: Optional[dict] = None,
    generation_callback: Optional[Callable[[dict], None]] = None,
    seed: Optional[int] = None
) -> dict:
    """
    Ejecuta el algoritmo genético completo.

    Args:
        planes: lista de aeronaves del problema.
        params: diccionario de parámetros (usa DEFAULT_PARAMS si es None).
        generation_callback: función llamada al final de cada generación con
                             un diccionario de estadísticas. Permite transmitir
                             datos a Unity vía WebSocket en tiempo real.
        seed: semilla aleatoria para reproducibilidad (opcional).

    Returns:
        Diccionario con la mejor solución encontrada:
          - best_sequence: lista de IDs de aeronaves en orden de aterrizaje
          - best_cost: costo total de la mejor solución
          - history: lista de (gen, best_cost, avg_cost) por generación
    """
    if seed is not None:
        random.seed(seed)

    if params is None:
        params = DEFAULT_PARAMS.copy()

    # Inicialización
    population = init_population(planes, params["pop_size"])
    costs = evaluate_population(population)

    best_idx = min(range(len(costs)), key=lambda i: costs[i])
    global_best_chrom = population[best_idx][:]
    global_best_cost = costs[best_idx]

    history = []

    for gen in range(params["n_generations"]):
        # Registrar estadísticas de esta generación
        best_cost_gen = min(costs)
        avg_cost_gen = sum(costs) / len(costs)

        if best_cost_gen < global_best_cost:
            best_idx = costs.index(best_cost_gen)
            global_best_cost = best_cost_gen
            global_best_chrom = population[best_idx][:]

        history.append((gen, global_best_cost, avg_cost_gen))

        # Construir payload para Unity
        gen_data = {
            "generation": gen,
            "best_sequence": [p.id for p in global_best_chrom],
            "best_cost": round(global_best_cost, 2),
            "population_costs": [round(c, 2) for c in costs],
            "avg_cost": round(avg_cost_gen, 2),
            "n_planes": len(planes),
            "params": params,
        }

        # Llamar al callback (servidor WebSocket lo usará para enviar a Unity)
        if generation_callback:
            generation_callback(gen_data)

        # Avanzar a la siguiente generación
        population = next_generation(population, costs, params)
        costs = evaluate_population(population)

    return {
        "best_sequence": [p.id for p in global_best_chrom],
        "best_cost": round(global_best_cost, 2),
        "history": history,
    }


# ─────────────────────────────────────────────────────────────
# Punto de entrada: ejecución en consola sin Unity
# ─────────────────────────────────────────────────────────────

if __name__ == "__main__":
    planes = generate_scenario()

    print("=" * 55)
    print("  Aircraft Landing Problem — Algoritmo Genético")
    print("=" * 55)
    print(f"  Aeronaves: {len(planes)}")
    print(f"  Parámetros: {DEFAULT_PARAMS}\n")

    def console_callback(data: dict):
        if data["generation"] % 20 == 0:
            print(
                f"  Gen {data['generation']:>4d} | "
                f"Mejor: {data['best_cost']:>10.2f} | "
                f"Prom: {data['avg_cost']:>10.2f}"
            )

    result = run_ga(planes, generation_callback=console_callback, seed=42)

    print("\n" + "=" * 55)
    print("  RESULTADO FINAL")
    print("=" * 55)
    print(f"  Mejor costo   : {result['best_cost']}")
    print(f"  Mejor secuencia: {' → '.join(result['best_sequence'])}")
    print("=" * 55)
