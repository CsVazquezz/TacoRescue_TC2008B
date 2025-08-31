#!/usr/bin/env python
# coding: utf-8

# In[ ]:


# !pip install numpy scipy matplotlib seaborn scikit-learn mesa==3.0 -q


# In[2]:


# Requiero Mesa > 3.0.3
# Importamos las clases que se requieren para manejar los agentes (Agent) y su entorno (Model).
from mesa import Agent, Model

# Debido a que puede existir más de un solo agente por celda, haremos uso de ''MultiGrid''.
from mesa.space import MultiGrid

# Con 'BaseScheduler', los agentes se activan de forma secuencial en el orden que fueron agregados.
from mesa.time import BaseScheduler

# Haremos uso de ''DataCollector'' para obtener información de cada paso de la simulación.
from mesa.datacollection import DataCollector

# Importamos los siguientes paquetes para el mejor manejo de valores numéricos.
import numpy as np
import pandas as pd
import matplotlib
import matplotlib.pyplot as plt
import matplotlib.animation as animation
plt.rcParams["animation.html"] = "jshtml"
matplotlib.rcParams['animation.embed_limit'] = 2**128


# In[3]:


class TacoRescueAgent(Agent):
  def __init__(self, model):
    super().__init__(model)
    self.AP = 4
    self.carrying_victim = False

  def move(self):
    possible_positions = self.model.grid.get_neighborhood(self.pos, moore=False, include_center=False)
    options = np.random.permutation(len(possible_positions))

    for i in options:
      position = possible_positions[i]
      if self.model.grid.is_cell_empty(position):
        self.model.grid.move_agent(self, position)
        break

  def step(self):
      (x, y) = self.pos
      self.move()


# In[4]:


# Función que genera una matriz RGB para visualizar el estado actual del tablero
# Blanco: Vacío | Gris: Humo | Rojo: Fuego
def get_grid(model):
    width, height = model.grid.width, model.grid.height
    grid = np.ones((width, height, 3))

    COLOR_FIRE = [1, 0, 0]        # Rojo
    COLOR_SMOKE = [0.5, 0.5, 0.5] # Gris

    # Fuego y Humo
    for x in range(width):
      for y in range(height):
        if model.fire[x][y] == 2:
          grid[x, y] = COLOR_FIRE
        elif model.fire[x][y] == 1:
          grid[x, y] = COLOR_SMOKE

    return grid

# Función que dibuja las paredes de cada celda en los ejes del grid
def draw_walls(ax, walls, walls_damage):
  height, width, _ = walls.shape
  for y in range(height):
    for x in range(width):
      cell = walls[y, x]
      damage = walls_damage[x, y]

      x0, y0 = x - 0.5, y - 0.5
      x1, y1 = x + 0.5, y + 0.5

      # Arriba
      if cell[0] == 1:
        if damage[0] == 0:
          ax.plot([x0, x1], [y1, y1], color="black", linewidth=2)
        elif damage[0] == 1:
          ax.plot([x0, x1], [y1, y1], color="orange", linewidth=2, linestyle="--")

      # Derecha
      if cell[1] == 1:
        if damage[1] == 0:
          ax.plot([x1, x1], [y0, y1], color="black", linewidth=2)
        elif damage[1] == 1:
          ax.plot([x1, x1], [y0, y1], color="orange", linewidth=2, linestyle="--")

      # Abajo
      if cell[2] == 1:
        if damage[2] == 0:
          ax.plot([x0, x1], [y0, y0], color="black", linewidth=2)
        elif damage[2] == 1:
            ax.plot([x0, x1], [y0, y0], color="orange", linewidth=2, linestyle="--")

      # Izquierda
      if cell[3] == 1:
        if damage[3] == 0:
          ax.plot([x0, x0], [y0, y1], color="black", linewidth=2)
        elif damage[3] == 1:
          ax.plot([x0, x0], [y0, y1], color="orange", linewidth=2, linestyle="--")


# In[5]:


class TacoRescueModel(Model):
  def __init__(self, width=8, height=6, players=6):
    super().__init__()

    self.grid = MultiGrid(width, height, torus=False)
    self.schedule = BaseScheduler(self)
    self.datacollector = DataCollector(model_reporters=
        {"Grid":get_grid,
        "Walls": lambda model: np.copy(model.walls),
        "WallsDamage": lambda model: np.copy(model.walls_damage),
        "Steps": lambda model: model.steps})

    self.steps = 0

    self.victims = [(3,4), (7,1)]
    self.false_alarms = [(0,1)]
    self.fire_pos = [(1,4),(1,3),(2,4),(2,3),(3,3),(3,2),(4,3),(5,1),(5,0),(6,1)]
    self.entries = [(5,5),(0,3),(7,2),(2,0)]
    self.doors_pos = [(1,3,2,3),(2,5,3,5),(3,2,3,1),(4,4,5,6),(4,0,5,0),(5,2,6,2),(6,0,7,0),(7,4,7,3)]

    # Diccionario que almacena información de las puertas
    # Cada puerta conecta dos celdas, se registran en ambos sentidos
    self.doors = {}
    for (x1, y1, x2, y2) in self.doors_pos:
        self.doors[(x1, y1)] = (x2, y2)
        self.doors[(x2, y2)] = (x1, y1)

    # Cada celda tiene un array de 4 paredes: [arriba, derecha, abajo, izquierda]
    # 0: No hay pared / puerta abierta | 1: Si hay pared / puerta cerrada
    self.walls = np.array([
      [[0,0,1,1],[0,0,1,0],[0,0,1,0],[0,0,1,0],[0,1,1,0],[0,0,1,1],[0,1,1,0],[0,1,1,1]],
      [[1,0,0,1],[1,0,0,0],[1,0,0,0],[1,0,0,0],[1,1,0,0],[1,0,0,1],[1,1,0,0],[1,1,0,1]],
      [[0,0,1,1],[0,1,1,0],[0,0,1,1],[0,0,1,0],[0,0,1,0],[0,1,1,0],[0,0,1,1],[0,1,1,0]],
      [[0,0,0,1],[0,1,0,0],[1,0,0,1],[1,0,0,0],[1,0,0,0],[1,1,0,0],[1,0,0,1],[1,1,0,0]],
      [[0,0,0,1],[0,0,0,0],[0,1,1,0],[0,0,1,1],[0,1,1,0],[0,0,1,1],[0,0,1,0],[0,1,1,0]],
      [[1,0,0,1],[1,0,0,0],[1,1,0,0],[1,0,0,1],[1,1,0,0],[1,0,0,1],[1,0,0,0],[1,1,0,0]]])

    # Matriz que almacena daño acumulado en paredes
    self.walls_damage = np.zeros( (width, height, 4) )

    # Matriz que almacena el estado del fuego
    # 0: Vacío | 1 = Humo | 2 = Fuego
    self.fire = np.zeros( (width, height) )
    for (x, y) in self.fire_pos:
      self.fire[x][y] = 2

    # Colocar agentes en las entradas del tablero
    i = 0
    while (i < players):
      position = self.entries[i % len(self.entries)]
      agent = TacoRescueAgent(self)
      self.grid.place_agent(agent, position)
      self.schedule.add(agent)
      i += 1

  # Método que avanza el fuego según las reglas del juego
  def advance_fire(self):
    x = self.random.randrange(self.grid.width)
    y = self.random.randrange(self.grid.height)
    current = self.fire[x][y]

    # Si está vacío -> poner humo
    if current == 0:
      self.fire[x][y] = 1
      if self.is_adjacent_to_fire(x, y):
          self.fire[x][y] = 2

    # Si hay humo -> convertir en fuego
    elif current == 1:
        self.fire[x][y] = 2

    # Si hay fuego -> explosión
    elif current == 2:
        self.explosion(x, y)

    # Aplicar flashover después de cada avance
    self.flashover()

  # Método que verifica si una celda es adyacente a fuego
  def is_adjacent_to_fire(self, x, y):
    directions = [(0, 1), (0, -1), (1, 0), (-1, 0)]
    for dx, dy in directions:
        nx, ny = x + dx, y + dy
        if self.can_propagate(x, y, nx, ny):
            if self.fire[nx][ny] == 2:
                return True
    return False

  # Método que verifica si se puede propagar fuego entre dos celdas
  def can_propagate(self, x, y, nx, ny):
    if not (0 <= nx < self.grid.width and 0 <= ny < self.grid.height):
      return False

    dx, dy = nx - x, ny - y

    # Arriba
    if dx == 0 and dy == 1:
      if self.walls[y][x][0] == 1:
        return False

    # Abajo
    if dx == 0 and dy == -1:
      if self.walls[y][x][2] == 1:
        return False

    # Derecha
    if dx == 1 and dy == 0:
      if self.walls[y][x][1] == 1:
        return False

    # Izquierda
    if dx == -1 and dy == 0:
      if self.walls[y][x][3] == 1:
        return False

    return True

  # Método que maneja una explosión: propaga fuego y daña paredes
  def explosion(self, x, y):
    directions = [(0, 1), (0, -1), (1, 0), (-1, 0)]
    for dx, dy in directions:
      nx, ny = x + dx, y + dy
      self.damage_wall(x, y, dx, dy)
      if self.can_propagate(x, y, nx, ny):
        self.shockwave(nx, ny, dx, dy)

  # Método que propaga la onda expansiva en línea recta
  def shockwave(self, x, y, dx, dy):
    while 0 <= x < self.grid.width and 0 <= y < self.grid.height:
      prev_x, prev_y = x - dx, y - dy
      if not self.can_propagate(prev_x, prev_y, x, y):
        break

      # Si encuentra vacío o humo, lo convierte en fuego y se detiene
      if self.fire[x][y] == 0 or self.fire[x][y] == 1:
          self.fire[x][y] = 2
          break

      # Si hay fuego, continua la onda expansiva
      elif self.fire[x][y] == 2:
          self.damage_wall(x, y, dx, dy)
          x += dx
          y += dy

  # Método que aplica el flashover: humo adyacente a fuego -> fuego
  def flashover(self):
    for y in range(self.grid.height):
      for x in range(self.grid.width):
        if self.fire[x][y] == 1 and self.is_adjacent_to_fire(x, y):
          self.fire[x][y] = 2

  # Método que daña paredes y puertas entre dos celdas
  def damage_wall(self, x, y, dx, dy):

    # Arriba
    if dx == 0 and dy == 1:
      wall = 0
      opp_wall = 2
      nx, ny = x, y + 1

    # Abajo
    elif dx == 0 and dy == -1:
      wall = 2
      opp_wall = 0
      nx, ny = x, y - 1

    # Derecha
    elif dx == 1 and dy == 0:
      wall = 1
      opp_wall = 3
      nx, ny = x + 1, y

    # Izquierda
    elif dx == -1 and dy == 0:
      wall = 3
      opp_wall = 1
      nx, ny = x - 1, y

    # Si es una puerta: romper inmediatamente
    if (x, y) in self.doors and self.doors[(x, y)] == (nx, ny):
      if self.walls[y][x][wall] == 1:
        self.walls[y][x][wall] = 0
        self.walls[ny][nx][opp_wall] = 0
        del self.doors[(x, y)]
        del self.doors[(nx, ny)]
      return

    # Si es una pared: acumula daño (2 golpes -> se destruye)
    if self.walls[y][x][wall] == 1:
      self.walls_damage[x][y][wall] += 1
      if self.walls_damage[x][y][wall] == 2:
        self.walls[y][x][wall] = 0

    # También daña la pared opuesta en la celda vecina
    if 0 <= nx < self.grid.width and 0 <= ny < self.grid.height:
      if self.walls[ny][nx][opp_wall] == 1:
        self.walls_damage[nx][ny][opp_wall] += 1
        if self.walls_damage[nx][ny][opp_wall] >= 2:
          self.walls[ny][nx][opp_wall] = 0

  # Método que ejecuta un paso del modelo
  def step(self):
    self.datacollector.collect(self)
    self.schedule.step()
    self.advance_fire()


# In[6]:


model = TacoRescueModel()
while (model.steps < 50):
    model.step()


# In[8]:


# Obtenemos la información que almacenó el colector
# Este nos entregará un DataFrame de pandas que contiene toda la información
all_grids = model.datacollector.get_model_vars_dataframe()
all_walls = model.datacollector.get_model_vars_dataframe()["Walls"]
all_walls_damage = model.datacollector.get_model_vars_dataframe()["WallsDamage"]
