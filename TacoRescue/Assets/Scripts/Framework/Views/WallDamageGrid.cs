using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Clase para deserializar la respuesta del estado de daño de las paredes desde JSON
/// </summary>
[System.Serializable]
public class WallDamageStateResponse
{
    public int step; // Paso actual de la simulación
    public List<List<List<float>>> walls_damage { get; set; } // Lista 3D de daños de paredes
}

/// <summary>
/// Clase principal que maneja la cuadrícula de daño de paredes con cambios de color
/// Mapea paredes existentes en la escena y cambia su color cuando se dañan
/// </summary>
public class WallDamageGrid : MonoBehaviour
{
    [Header("Configuración de Paredes Existentes")]
    public Transform wallsParent; // Referencia al objeto padre "Walls" en la escena
    public string wallNamePattern = "wall"; // Patrón de nombres de las paredes
    
    [Header("Configuración de Cuadrícula")]
    public Vector3 startPosition = new Vector3(-7f, 1.7f, -5.2f); // Posición inicial de la cuadrícula
    public float cellSize = 2f; // Tamaño de cada celda
    public float wallDetectionRadius = 1f; // Radio para detectar paredes en posiciones

    [Header("Configuración de Colores de Daño")]
    public Color normalWallColor = Color.white; // Color para paredes normales
    public Color damagedWallColor = Color.orange; // Color para paredes dañadas (nivel 1)
    public Color demolishedWallColor = Color.red; // Color para paredes demolidas (nivel 2)

    [Header("Configuración de Jerarquía")]
    public Transform gameElementsParent; // Padre de los elementos del juego
    private Transform wallDamageParent; // Contenedor específico para objetos de daño de paredes

    // Diccionarios para gestión de objetos
    private Dictionary<Vector3Int, GameObject> gridToWallMapping = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, int> wallDamageStates = new Dictionary<Vector3Int, int>(); // 0=normal, 1=dañada, 2=demolida

    /// <summary>
    /// Inicialización del componente - configura la jerarquía de objetos y mapea paredes existentes
    /// </summary>
    void Awake()
    {
        if (gameElementsParent != null)
        {
            Transform existing = gameElementsParent.Find("WallDamages");
            if (existing != null)
            {
                wallDamageParent = existing;
            }
            else
            {
                GameObject wallDamagesGO = new GameObject("WallDamages");
                wallDamagesGO.transform.SetParent(gameElementsParent);
                wallDamageParent = wallDamagesGO.transform;
            }
        }
        
        // Mapear paredes existentes en la escena a coordenadas de cuadrícula
        MapExistingWallsToGrid();
    }

    /// <summary>
    /// Mapea las paredes existentes en la escena a posiciones de cuadrícula
    /// Similar al sistema de puertas pero para paredes direccionales
    /// </summary>
    private void MapExistingWallsToGrid()
    {
        if (wallsParent == null)
        {
            Debug.LogError("¡wallsParent no está asignado! Por favor asigna el objeto padre 'Walls' desde la jerarquía de la escena.");
            return;
        }

        // Limpia los mapeos previos
        gridToWallMapping.Clear();
        wallDamageStates.Clear();

        Debug.Log("=== MAPEANDO PAREDES EXISTENTES A CUADRÍCULA ===");
        
        // Obtener todas las paredes en la escena
        List<Transform> allWalls = new List<Transform>();
        foreach (Transform wall in wallsParent)
        {
            if (wall.name.ToLower().Contains(wallNamePattern.ToLower()))
            {
                allWalls.Add(wall);
                Vector3 pos = wall.position;
                Debug.Log($"Pared encontrada: '{wall.name}' en posición ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
            }
        }
        
        Debug.Log($"Total de paredes encontradas: {allWalls.Count}");
        
        // Mapear cada pared a su posición de cuadrícula y dirección correspondiente
        foreach (Transform wall in allWalls)
        {
            Vector2Int gridPos = WorldToGridPosition(wall.position);
            int direction = DetermineWallDirection(wall.position, gridPos);
            
            if (direction >= 0)
            {
                Vector3Int wallKey = new Vector3Int(gridPos.x, gridPos.y, direction);
                gridToWallMapping[wallKey] = wall.gameObject;
                wallDamageStates[wallKey] = 0; // Estado inicial: normal
                
                Debug.Log($"Mapeada pared '{wall.name}' a cuadrícula ({gridPos.x}, {gridPos.y}) dirección {direction}");
            }
            else
            {
                Debug.LogWarning($"No se pudo determinar dirección para pared '{wall.name}' en posición {wall.position}");
            }
        }
        
        Debug.Log($"Se mapearon exitosamente {gridToWallMapping.Count} paredes");
    }

    /// <summary>
    /// Método de debug para mostrar la estructura JSON esperada y el estado actual
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugWallSystem()
    {
        Debug.Log("=== DEBUG DEL SISTEMA DE PAREDES ===");
        Debug.Log(" ESTRUCTURA JSON ESPERADA:");
        Debug.Log("   walls_damage[y][x][direction]");
        Debug.Log("   - y: fila (0 a height-1)");
        Debug.Log("   - x: columna (0 a width-1)"); 
        Debug.Log("   - direction: 0=ARRIBA, 1=DERECHA, 2=ABAJO, 3=IZQUIERDA");
        Debug.Log("   - valores: 0=normal, 1=dañada, 2=demolida");
        Debug.Log("");
        
        Debug.Log($"🗺️ PAREDES MAPEADAS: {gridToWallMapping.Count}");
        foreach (var kvp in gridToWallMapping)
        {
            Vector3Int wallKey = kvp.Key;
            GameObject wallObj = kvp.Value;
            int currentState = wallDamageStates.GetValueOrDefault(wallKey, 0);
            string dirName = GetDirectionName(wallKey.z);
            Debug.Log($"   Pared ({wallKey.x},{wallKey.y}) {dirName} → '{wallObj.name}' Estado: {currentState}");
        }
        
        Debug.Log("=== FIN DEBUG ===");
    }

    /// <summary>
    /// Convierte posición mundial a coordenadas de cuadrícula
    /// </summary>
    /// <param name="worldPos">Posición en el mundo</param>
    /// <returns>Coordenadas de cuadrícula</returns>
    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.z - startPosition.z) / cellSize);
        int y = Mathf.RoundToInt((worldPos.x - startPosition.x) / cellSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Determina la dirección de una pared basándose en su posición relativa a la celda
    /// </summary>
    /// <param name="wallPos">Posición de la pared</param>
    /// <param name="gridPos">Posición de la celda en la cuadrícula</param>
    /// <returns>Dirección: 0=Arriba, 1=Derecha, 2=Abajo, 3=Izquierda, -1=Error</returns>
    private int DetermineWallDirection(Vector3 wallPos, Vector2Int gridPos)
    {
        Vector3 cellCenter = new Vector3(
            startPosition.x + gridPos.y * cellSize,
            startPosition.y,
            startPosition.z + gridPos.x * cellSize
        );
        
        Vector3 offset = wallPos - cellCenter;
        float absX = Mathf.Abs(offset.x);
        float absZ = Mathf.Abs(offset.z);
        
        // Determinar qué eje tiene mayor offset para identificar la dirección
        if (absZ > absX)
        {
            // Pared en eje Z (arriba/abajo)
            return offset.z > 0 ? 0 : 2; // 0=Arriba, 2=Abajo
        }
        else
        {
            // Pared en eje X (derecha/izquierda)
            return offset.x > 0 ? 1 : 3; // 1=Derecha, 3=Izquierda
        }
    }

    /// <summary>
    /// Actualiza la cuadrícula de daño de paredes basándose en eventos de simulación
    /// Aplica efectos visuales a las paredes mapeadas existentes en lugar de crear objetos
    /// </summary>
    /// <param name="json">Datos JSON con el estado actual</param>
    /// <param name="ev">Evento de simulación</param>
    /// <param name="eventIndex">Índice del evento</param>
    public void UpdateWallDamageGrid(string json, SimulationEvent ev, int eventIndex)
    {
        if (ev == null) return;
        
        WallDamageStateResponse state = JsonConvert.DeserializeObject<WallDamageStateResponse>(json);
        int dir1 = CalcularDireccion(ev.Pos1.y, ev.Pos1.x, ev.Pos2.y, ev.Pos2.x);
        int dir2 = CalcularDireccion(ev.Pos2.y, ev.Pos2.x, ev.Pos1.y, ev.Pos1.x);
        Vector3Int wallKey1 = new Vector3Int(ev.Pos1.x, ev.Pos1.y, dir1);
        Vector3Int wallKey2 = new Vector3Int(ev.Pos2.x, ev.Pos2.y, dir2);
        
        if (ev.action == "damage_wall" && ev.step == state.step)
        {
            Debug.Log($"🔨 Dañando paredes en posiciones {ev.Pos1} y {ev.Pos2}");
            ChangeWallColor(wallKey1, 1); // Nivel 1 de daño
            ChangeWallColor(wallKey2, 1);
        }
        else if (ev.action == "demolish_wall" && ev.step == state.step)
        {
            Debug.Log($" Demoliendo paredes en posiciones {ev.Pos1} y {ev.Pos2}");
            ChangeWallColor(wallKey1, 2); // Nivel 2 de daño (demolida)
            ChangeWallColor(wallKey2, 2);
        }
    }

    /// <summary>
    /// Cambia el color de una pared específica según el nivel de daño
    /// ESTE MÉTODO ACTUALIZA SOLAMENTE EL COLOR DE LA PARED MAPEADA BASÁNDOSE EN LA INFORMACIÓN DEL JSON
    /// </summary>
    /// <param name="wallKey">Clave de la pared (x, y, dirección)</param>
    /// <param name="damageLevel">Nivel de daño: 1=dañada, 2=demolida</param>
    private void ChangeWallColor(Vector3Int wallKey, int damageLevel)
    {
        Debug.Log($"� CAMBIANDO COLOR: Pared {wallKey} → Nivel {damageLevel}");
        
        if (gridToWallMapping.TryGetValue(wallKey, out GameObject wallObject))
        {
            int currentState = wallDamageStates.GetValueOrDefault(wallKey, 0);
            
            Debug.Log($" Estado actual de pared {wallKey}: {currentState} → Nuevo estado: {damageLevel}");
            
            // Solo aplicar daño si es un nivel superior o diferente
            if (damageLevel != currentState)
            {
                wallDamageStates[wallKey] = damageLevel;
                
                Renderer renderer = wallObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Cambiar color según nivel de daño
                    Color newColor;
                    switch (damageLevel)
                    {
                        case 1: // Dañada
                            newColor = damagedWallColor; // Color naranja para daño
                            break;
                        case 2: // Demolida
                            newColor = demolishedWallColor; // Color rojo para demolición
                            break;
                        default: // Normal
                            newColor = normalWallColor; // Color blanco normal
                            break;
                    }
                    
                    renderer.material.color = newColor;
                    Debug.Log($" COLOR CAMBIADO: Pared '{wallObject.name}' en {wallKey} → {newColor}");
                }
                else
                {
                    Debug.LogWarning($" Pared '{wallObject.name}' no tiene Renderer component");
                }
            }
            else
            {
                Debug.Log($" Pared {wallKey} ya tiene nivel de daño {damageLevel}, no se actualiza");
            }
        }
        else
        {
            Debug.LogWarning($" NO SE ENCONTRÓ pared mapeada en posición {wallKey}");
            Debug.LogWarning($" Paredes disponibles: {gridToWallMapping.Count}");
            
            // Mostrar algunas paredes mapeadas para debug
            if (gridToWallMapping.Count > 0)
            {
                int count = 0;
                Debug.Log(" Primeras 5 paredes mapeadas:");
                foreach (var kvp in gridToWallMapping)
                {
                    if (count >= 5) break;
                    Debug.Log($"   - {kvp.Key} → {kvp.Value.name}");
                    count++;
                }
            }
        }
    }

    /// <summary>
    /// Calcula la dirección de una pared basándose en dos posiciones consecutivas
    /// </summary>
    /// <param name="x1">Coordenada X de la primera posición</param>
    /// <param name="y1">Coordenada Y de la primera posición</param>
    /// <param name="x2">Coordenada X de la segunda posición</param>
    /// <param name="y2">Coordenada Y de la segunda posición</param>
    /// <returns>Dirección de la pared: 0=Arriba, 1=Derecha, 2=Abajo, 3=Izquierda, -1=Error</returns>
    public int CalcularDireccion(int x1, int y1, int x2, int y2) 
    {
        int dx = x2 - x1;
        int dy = y2 - y1;
        int dir = -1;
        
        if (dx == 1)
        {
            dir = 0; // Arriba
        }
        else if (dx == -1)
        {
            dir = 2; // Abajo
        }
        else if (dy == 1)
        {
            dir = 1; // Derecha
        }
        else if (dy == -1)
        {
            dir = 3; // Izquierda
        }
        
        return dir;
    }

    /// <summary>
    /// Llena la cuadrícula de daño de paredes basándose en el estado completo del JSON
    /// Utilizado para inicializar o sincronizar el estado visual con el servidor
    /// ESTE MÉTODO LEE EL JSON Y ACTUALIZA EL ESTADO DE LAS PAREDES SEGÚN LA INFORMACIÓN RECIBIDA
    /// </summary>
    /// <param name="json">JSON con el estado completo de las paredes dañadas</param>
    public void FillWallDamageGrid(string json)
    {
        Debug.Log($"📥 RECIBIENDO JSON para actualizar estado de paredes: {json.Length} caracteres");
        
        WallDamageStateResponse state;
        try
        {
            state = JsonConvert.DeserializeObject<WallDamageStateResponse>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($" Error al parsear JSON en WallDamageGridManager: {e.Message}");
            Debug.LogError($"JSON recibido: {json}");
            return;
        }

        if (state == null)
        {
            Debug.LogError(" Estado deserializado es null");
            return;
        }
        
        if (state.walls_damage == null)
        {
            Debug.LogError(" walls_damage es null en el JSON");
            return;
        }

        Debug.Log($" JSON parseado exitosamente - Step: {state.step}");
        Debug.Log($" Procesando grid de paredes: {state.walls_damage.Count} filas x {(state.walls_damage.Count > 0 ? state.walls_damage[0].Count : 0)} columnas");

        int wallsProcessed = 0;
        int wallsUpdated = 0;

        // Procesar cada celda del grid
        for (int y = 0; y < state.walls_damage.Count; y++)
        {
            for (int x = 0; x < state.walls_damage[y].Count; x++)
            {
                List<float> wallValues = state.walls_damage[y][x];
                
                // Verificar que la celda tenga exactamente 4 valores (4 direcciones)
                if (wallValues == null || wallValues.Count != 4)
                {
                    Debug.LogWarning($" Celda ({x},{y}) tiene {wallValues?.Count ?? 0} valores de pared en lugar de 4");
                    continue;
                }

                // Procesar cada dirección de pared (0=arriba, 1=derecha, 2=abajo, 3=izquierda)
                for (int dir = 0; dir < wallValues.Count; dir++)
                {
                    float value = wallValues[dir];
                    Vector3Int wallKey = new Vector3Int(x, y, dir);
                    wallsProcessed++;
                    
                    // Solo actualizar si hay daño (valor > 0)
                    if (value > 0)
                    {
                        string directionName = GetDirectionName(dir);
                        Debug.Log($" Pared en ({x},{y}) dirección {directionName} tiene valor: {value}");
                        
                        if (value == 1.0f)
                        {
                            ChangeWallColor(wallKey, 1); // Daño nivel 1
                            wallsUpdated++;
                            Debug.Log($"🔨 Aplicando DAÑO NIVEL 1 a pared ({x},{y}) {directionName}");
                        }
                        else if (value == 2.0f)
                        {
                            ChangeWallColor(wallKey, 2); // Daño nivel 2 (demolida)
                            wallsUpdated++;
                            Debug.Log($" Aplicando DAÑO NIVEL 2 (demolida) a pared ({x},{y}) {directionName}");
                        }
                        else
                        {
                            Debug.LogWarning($" Valor de daño no reconocido: {value} en pared ({x},{y}) {directionName}");
                        }
                    }
                }
            }
        }
        
        Debug.Log($" RESUMEN: Procesadas {wallsProcessed} paredes, actualizadas {wallsUpdated} con daño");
        Debug.Log($" Total de paredes mapeadas en escena: {gridToWallMapping.Count}");
    }

    /// <summary>
    /// Obtiene el nombre descriptivo de una dirección de pared
    /// </summary>
    /// <param name="direction">Dirección: 0=arriba, 1=derecha, 2=abajo, 3=izquierda</param>
    /// <returns>Nombre de la dirección</returns>
    private string GetDirectionName(int direction)
    {
        switch (direction)
        {
            case 0: return "ARRIBA";
            case 1: return "DERECHA"; 
            case 2: return "ABAJO";
            case 3: return "IZQUIERDA";
            default: return $"DESCONOCIDA({direction})";
        }
    }
}
