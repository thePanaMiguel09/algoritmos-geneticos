"""
alp_data.py
===========
Definición de datos del problema Aircraft Landing Problem (ALP).

Escenario con 15 aeronaves de distintos tipos (H, M, L).
Con 15 aviones hay 15! = 1.307.674.368.000 permutaciones posibles,
lo que hace que el GA ya no garantice siempre el mismo óptimo,
generando variación visible entre ejecuciones.

Tipos de aeronave (ICAO Wake Turbulence Category):
  H = Heavy  (>136 000 kg)
  M = Medium (7 000–136 000 kg)
  L = Light  (<7 000 kg)

Matriz de separación mínima en segundos [líder][seguidor]:
         seguidor → H    M    L
  líder H          96  157  196
  líder M          60   69  131
  líder L          60   69   82
"""

from dataclasses import dataclass
from typing import List


@dataclass
class Aircraft:
    id: str
    aircraft_type: str  # "H", "M" o "L"
    earliest: int       # Tiempo más temprano de aterrizaje
    target: int         # Tiempo ideal / objetivo
    latest: int         # Tiempo más tardío permitido
    penalty_early: float
    penalty_late: float


SEP_MATRIX = {
    ("H", "H"): 96,  ("H", "M"): 157, ("H", "L"): 196,
    ("M", "H"): 60,  ("M", "M"): 69,  ("M", "L"): 131,
    ("L", "H"): 60,  ("L", "M"): 69,  ("L", "L"): 82,
}

WINDOW_VIOLATION_PENALTY = 1000.0


def get_separation(leader_type: str, follower_type: str) -> int:
    return SEP_MATRIX[(leader_type, follower_type)]


def generate_scenario() -> List[Aircraft]:
    """
    Escenario con 15 aeronaves.
    Las 5 nuevas (A11-A15) añaden más variedad de tipos y ventanas
    para que el espacio de búsqueda sea suficientemente grande y el GA
    produzca resultados distintos en cada ejecución.
    """
    planes = [
        # ── Aeronaves originales ──────────────────────────────────────
        Aircraft("A01", "H", earliest=0,   target=129, latest=559, penalty_early=10.0, penalty_late=10.0),
        Aircraft("A02", "H", earliest=10,  target=195, latest=744, penalty_early=10.0, penalty_late=10.0),
        Aircraft("A03", "M", earliest=15,  target=89,  latest=510, penalty_early=30.0, penalty_late=30.0),
        Aircraft("A04", "M", earliest=20,  target=96,  latest=521, penalty_early=30.0, penalty_late=30.0),
        Aircraft("A05", "H", earliest=45,  target=110, latest=576, penalty_early=10.0, penalty_late=10.0),
        Aircraft("A06", "L", earliest=60,  target=120, latest=447, penalty_early=30.0, penalty_late=30.0),
        Aircraft("A07", "M", earliest=85,  target=124, latest=448, penalty_early=30.0, penalty_late=30.0),
        Aircraft("A08", "H", earliest=100, target=126, latest=559, penalty_early=10.0, penalty_late=10.0),
        Aircraft("A09", "M", earliest=110, target=135, latest=510, penalty_early=30.0, penalty_late=30.0),
        Aircraft("A10", "L", earliest=125, target=155, latest=447, penalty_early=30.0, penalty_late=30.0),
        # ── Aeronaves nuevas ──────────────────────────────────────────
        #Aircraft("A11", "H", earliest=140, target=210, latest=620, penalty_early=10.0, penalty_late=10.0),
        #Aircraft("A12", "M", earliest=155, target=230, latest=580, penalty_early=30.0, penalty_late=30.0),
        #Aircraft("A13", "L", earliest=170, target=245, latest=500, penalty_early=30.0, penalty_late=30.0),
        #Aircraft("A14", "M", earliest=190, target=270, latest=600, penalty_early=30.0, penalty_late=30.0),
        #Aircraft("A15", "H", earliest=210, target=300, latest=680, penalty_early=10.0, penalty_late=10.0),
    ]
    return planes