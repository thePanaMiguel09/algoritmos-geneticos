"""
alp_operators.py
================
Operadores genéticos para el Aircraft Landing Problem.

Todos los operadores trabajan con permutaciones de Aircraft, garantizando
que los descendientes sean siempre permutaciones válidas (sin duplicados
ni faltantes). Esta es la propiedad fundamental de la representación por
permutación exigida por la actividad.

Operadores implementados:
  - Selección por torneo (tournament_selection)
  - Cruzamiento Order Crossover OX1 (ox1_crossover)
  - Mutación por intercambio / swap (swap_mutation)
  - Mutación por inversión de segmento (inversion_mutation)
  - Elitismo (get_elite)
"""

import random
from typing import List, Tuple
from alp_data import Aircraft


# ─────────────────────────────────────────────────────────────
# Selección
# ─────────────────────────────────────────────────────────────

def tournament_selection(
    population: List[List[Aircraft]],
    costs: List[float],
    k: int = 3
) -> List[Aircraft]:
    """
    Selección por torneo.

    Elige k individuos al azar y devuelve el de menor costo (mejor fitness).
    Justificación: el torneo mantiene presión selectiva controlada —
    un k pequeño (3) preserva diversidad sin estancar prematuramente.

    Args:
        population: lista de cromosomas (cada uno es una lista de Aircraft).
        costs: lista de costos correspondientes a cada individuo.
        k: tamaño del torneo.

    Returns:
        El cromosoma ganador (copia independiente).
    """
    indices = random.sample(range(len(population)), k)
    winner_idx = min(indices, key=lambda i: costs[i])
    return population[winner_idx][:]  # copia para no mutar el original


# ─────────────────────────────────────────────────────────────
# Cruzamiento
# ─────────────────────────────────────────────────────────────

def ox1_crossover(
    parent1: List[Aircraft],
    parent2: List[Aircraft]
) -> Tuple[List[Aircraft], List[Aircraft]]:
    """
    Order Crossover (OX1) — cruzamiento de orden.

    Preserva el orden relativo de los genes del segundo padre en los hijos,
    mientras copia un segmento del primer padre directamente. Garantiza que
    cada avión aparezca exactamente una vez en el descendiente.

    Algoritmo:
      1. Elige dos puntos de corte aleatorios c1, c2.
      2. Copia el segmento [c1:c2] del padre1 al hijo1 en las mismas posiciones.
      3. Rellena el resto del hijo1 con los elementos de padre2 en el orden
         en que aparecen, omitiendo los ya copiados.
      4. Repite simétricamente para hijo2.

    Args:
        parent1, parent2: cromosomas progenitores.

    Returns:
        Tupla con dos hijos (child1, child2), ambos permutaciones válidas.
    """
    n = len(parent1)
    c1, c2 = sorted(random.sample(range(n), 2))

    def _build_child(p_segment: List[Aircraft], p_order: List[Aircraft]) -> List[Aircraft]:
        """Construye un hijo a partir del segmento de p_segment y el orden de p_order."""
        segment_ids = {a.id for a in p_segment[c1:c2]}
        child = [None] * n
        # Paso 1: copiar el segmento central
        child[c1:c2] = p_segment[c1:c2]
        # Paso 2: rellenar con el orden de p_order, saltando los ya presentes
        fill_iter = (a for a in p_order if a.id not in segment_ids)
        fill_positions = list(range(c2, n)) + list(range(0, c1))
        for pos, plane in zip(fill_positions, fill_iter):
            child[pos] = plane
        return child

    child1 = _build_child(parent1, parent2)
    child2 = _build_child(parent2, parent1)
    return child1, child2


# ─────────────────────────────────────────────────────────────
# Mutación
# ─────────────────────────────────────────────────────────────

def swap_mutation(chromosome: List[Aircraft]) -> List[Aircraft]:
    """
    Mutación por intercambio (swap).

    Elige dos posiciones al azar e intercambia los aviones en esas posiciones.
    Es la mutación más sencilla compatible con permutaciones y garantiza
    que el resultado siga siendo una permutación válida.

    Efecto: altera el orden local de dos aeronaves, introduciendo diversidad
    sin destruir la estructura global de la solución.

    Args:
        chromosome: cromosoma a mutar (se trabaja sobre una copia).

    Returns:
        Nuevo cromosoma con dos genes intercambiados.
    """
    mutant = chromosome[:]
    i, j = random.sample(range(len(mutant)), 2)
    mutant[i], mutant[j] = mutant[j], mutant[i]
    return mutant


def inversion_mutation(chromosome: List[Aircraft]) -> List[Aircraft]:
    """
    Mutación por inversión de segmento.

    Elige dos puntos de corte e invierte el segmento entre ellos.
    Produce cambios más grandes que swap, útil para escapar de óptimos
    locales cuando swap queda atrapado.

    Efecto: invierte el orden de un sub-bloque de la cola, lo que puede
    reorganizar significativamente la separación entre grupos de aeronaves.

    Args:
        chromosome: cromosoma a mutar.

    Returns:
        Nuevo cromosoma con el segmento invertido.
    """
    mutant = chromosome[:]
    i, j = sorted(random.sample(range(len(mutant)), 2))
    mutant[i:j+1] = reversed(mutant[i:j+1])
    return mutant


def mutate(
    chromosome: List[Aircraft],
    mutation_rate: float,
    use_inversion: bool = True
) -> List[Aircraft]:
    """
    Aplica mutación con probabilidad mutation_rate.

    Alterna entre swap e inversión para cubrir distintos tamaños de cambio.

    Args:
        chromosome: individuo a mutar.
        mutation_rate: probabilidad de que ocurra la mutación (0.0–1.0).
        use_inversion: si True, puede elegir inversión; si False, solo swap.

    Returns:
        Cromosoma mutado (o el original si no ocurre mutación).
    """
    if random.random() < mutation_rate:
        if use_inversion and random.random() < 0.5:
            return inversion_mutation(chromosome)
        else:
            return swap_mutation(chromosome)
    return chromosome[:]


# ─────────────────────────────────────────────────────────────
# Elitismo
# ─────────────────────────────────────────────────────────────

def get_elite(
    population: List[List[Aircraft]],
    costs: List[float],
    elite_size: int
) -> List[List[Aircraft]]:
    """
    Elitismo: conserva los mejores individuos entre generaciones.

    Los elite_size individuos con menor costo pasan directamente a la
    siguiente generación sin modificación. Esto garantiza que la mejor
    solución encontrada nunca empeore.

    Justificación: sin elitismo, existe probabilidad positiva de perder
    al mejor individuo en cada generación (efecto de deriva genética).

    Args:
        population: población actual.
        costs: costos asociados.
        elite_size: número de élites a conservar.

    Returns:
        Lista con los elite_size mejores cromosomas (copias independientes).
    """
    sorted_pairs = sorted(zip(costs, population), key=lambda x: x[0])
    return [chrom[:] for _, chrom in sorted_pairs[:elite_size]]
