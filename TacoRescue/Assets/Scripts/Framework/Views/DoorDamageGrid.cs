using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Estructura para almacenar las coordenadas de una puerta
/// </summary>
[System.Serializable]
public struct DoorCoords
{
    public int x1, y1, x2, y2; // Coordenadas de inicio y fin de la puerta

    /// <summary>
    /// Constructor para inicializar las coordenadas de la puerta
    /// </summary>
    public DoorCoords(int x1, int y1, int x2, int y2)
    {
        this.x1 = x1; this.y1 = y1;
        this.x2 = x2; this.y2 = y2;
    }
}

/// <summary>
/// Clase para deserializar la respuesta del estado de daño de las puertas desde JSON
/// </summary>
[System.Serializable]
public class DoorDamageStateResponse
{
    public int step; // Paso actual del juego
    public Dictionary<string, List<int>> doors { get; set; } // Diccionario de puertas y sus estados
}

/// <summary>
/// Clase principal que maneja la cuadrícula de daño de puertas en el juego TacoRescue
/// Se encarga de mapear puertas a posiciones de cuadrícula y controlar sus animaciones de apertura/cierre
/// </summary>
public class DoorDamageGrid : MonoBehaviour
{
    [Header("Configuración de Animaciones de Puertas")]
    public Transform doorsParent; // Referencia al objeto padre "Doors" en la escena
    public string openAnimationTrigger = "Open"; // Nombre del trigger para abrir puertas
    public string closeAnimationTrigger = "Close"; // Nombre del trigger para cerrar puertas
    
    [Header("Configuración de Cuadrícula")]
    public Vector3 startPosition = new Vector3(-7f, 1.7f, -5.2f); // Posición de inicio de la cuadrícula
    public float cellSize = 2f; // Tamaño de cada celda de la cuadrícula
    public float doorDetectionRadius = 3f; // Radio aumentado para ayudar a encontrar puertas

    [Header("Configuración de Proximidad de Agentes")]
    public float agentProximityRadius = 1f; // Radio para detectar agentes cerca de puertas
    public LayerMask agentLayerMask = -1; // Layer mask para detectar agentes
    public string agentTag = "Agent"; // Tag de los agentes
    
    public Transform gameElementsParent; // Padre de los elementos del juego

    // Diccionarios para mapear posiciones de cuadrícula a objetos de puerta y sus estados
    private Dictionary<Vector2Int, GameObject> gridToDoorMapping = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, bool> doorStates = new Dictionary<Vector2Int, bool>(); // true = abierta, false = cerrada

    /// <summary>
    /// Método que se ejecuta al despertar el objeto
    /// Inicializa el mapeo de puertas a la cuadrícula
    /// </summary>
    void Awake()
    {
        MapExistingDoorsToGrid(); // Intenta primero el mapeo basado en nombres
        
        // Si el mapeo basado en nombres no funcionó bien, intenta el mapeo basado en posiciones
        if (gridToDoorMapping.Count < 3) // Si mapeamos menos de 3 puertas
        {
            Debug.Log("El mapeo basado en nombres encontró pocas puertas, intentando mapeo basado en posiciones...");
        }
    }

    /// <summary>
    /// Actualiza continuamente para verificar la proximidad de agentes a las puertas
    /// </summary>
    void Update()
    {
        CheckAgentProximityToDoors();
    }

    /// <summary>
    /// Verifica si hay agentes cerca de las puertas y las abre/cierra automáticamente
    /// </summary>
    private void CheckAgentProximityToDoors()
    {
        foreach (var doorMapping in gridToDoorMapping)
        {
            Vector2Int gridPos = doorMapping.Key;
            GameObject doorObject = doorMapping.Value;
            
            if (doorObject == null) continue;

            bool agentNearby = IsAgentNearDoor(doorObject.transform.position);
            bool currentlyOpen = doorStates.GetValueOrDefault(gridPos, false);

            // Abre la puerta si hay un agente cerca y está cerrada
            if (agentNearby && !currentlyOpen)
            {
                OpenDoor(gridPos);
            }
            // Cierra la puerta si no hay agentes cerca y está abierta
            else if (!agentNearby && currentlyOpen)
            {
                CloseDoor(gridPos);
            }
        }
    }

    /// <summary>
    /// Verifica si hay algún agente cerca de una posición específica
    /// </summary>
    /// <param name="doorPosition">Posición de la puerta</param>
    /// <returns>True si hay un agente cerca, false en caso contrario</returns>
    private bool IsAgentNearDoor(Vector3 doorPosition)
    {
        // Busca objetos con el tag especificado dentro del radio
        GameObject[] agentsInRange = GameObject.FindGameObjectsWithTag(agentTag);
        
        foreach (GameObject agent in agentsInRange)
        {
            float distance = Vector3.Distance(agent.transform.position, doorPosition);
            if (distance <= agentProximityRadius)
            {
                return true;
            }
        }

        // Método alternativo usando Physics.OverlapSphere si los agentes tienen Colliders
        Collider[] agentColliders = Physics.OverlapSphere(doorPosition, agentProximityRadius, agentLayerMask);
        foreach (Collider col in agentColliders)
        {
            if (col.CompareTag(agentTag))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Mapea las puertas existentes en la escena a posiciones de cuadrícula
    /// Utiliza nombres predefinidos para hacer el mapeo
    /// </summary>
    private void MapExistingDoorsToGrid()
    {
        if (doorsParent == null)
        {
            Debug.LogError("¡doorsParent no está asignado! Por favor asigna el objeto padre 'Doors' desde la jerarquía de la escena.");
            return;
        }

        // Limpia los mapeos previos
        gridToDoorMapping.Clear();
        doorStates.Clear();

        Debug.Log("=== ANALIZANDO TODAS LAS PUERTAS EN LA ESCENA ===");
        
        // Primero, veamos qué puertas existen realmente y sus posiciones
        List<Transform> allDoors = new List<Transform>();
        foreach (Transform door in doorsParent)
        {
            allDoors.Add(door);
            Vector3 pos = door.position;
            Debug.Log($"Puerta encontrada: '{door.name}' en posición ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})");
        }
        
        Debug.Log($"Total de puertas encontradas: {allDoors.Count}");
        Debug.Log("=== MAPEO DE PUERTAS HARDCODEADO ===");

        // MAPEOS HARDCODEADOS - Mapeamos puertas por sus nombres/posiciones reales
        // Esta es una solución temporal para que funcione, luego podemos refinar basándose en tu layout específico de puertas
        
        // Intenta mapear puertas encontrándolas por patrones de nombres o posiciones más cercanas
        TryMapDoorByName("door-rotate", new Vector2Int(3, 1));
        TryMapDoorByName("door-rotate (1)", new Vector2Int(5, 2));
        TryMapDoorByName("door-rotate (2)", new Vector2Int(2, 3));
        TryMapDoorByName("door-rotate (3)", new Vector2Int(4, 4));
        TryMapDoorByName("door-rotate (4)", new Vector2Int(0, 4));
        TryMapDoorByName("door-rotate (5)", new Vector2Int(2, 5));
        TryMapDoorByName("door-rotate (6)", new Vector2Int(0, 6));
        TryMapDoorByName("door-rotate (7)", new Vector2Int(4, 7));
        TryMapDoorByName("door-rotate (8)", new Vector2Int(3, 7));
        TryMapDoorByName("door-rotate (9)", new Vector2Int(1, 3));
        TryMapDoorByName("door-rotate (10)", new Vector2Int(4, 5));
        TryMapDoorByName("door-rotate (11)", new Vector2Int(0, 5));

        Debug.Log($"Se mapearon exitosamente {gridToDoorMapping.Count} puertas de {allDoors.Count} puertas disponibles");
    }

    /// <summary>
    /// Intenta mapear una puerta específica por su nombre a una posición de cuadrícula
    /// </summary>
    /// <param name="doorName">Nombre de la puerta a buscar</param>
    /// <param name="gridPos">Posición en la cuadrícula donde mapear la puerta</param>
    private void TryMapDoorByName(string doorName, Vector2Int gridPos)
    {
        if (doorsParent == null) return;
        
        foreach (Transform door in doorsParent)
        {
            if (door.name == doorName)
            {
                gridToDoorMapping[gridPos] = door.gameObject;
                doorStates[gridPos] = false; // Inicialmente cerrada
                Debug.Log($"Mapeada '{doorName}' a cuadrícula {gridPos} en posición {door.position}");
                return;
            }
        }
        
        Debug.LogWarning($"No se pudo encontrar puerta llamada '{doorName}' para cuadrícula {gridPos}");
    }

    /// <summary>
    /// Mapeo alternativo hardcodeado por posición si los nombres no funcionan
    /// </summary>
    private void MapDoorsByActualPositions()
    {
        if (doorsParent == null) return;
        
        Debug.Log("=== MAPEANDO PUERTAS POR POSICIONES REALES ===");
        
        // Limpia cualquier mapeo existente
        gridToDoorMapping.Clear();
        doorStates.Clear();
        
        // Obtiene todas las puertas y sus posiciones
        List<Transform> allDoors = new List<Transform>();
        foreach (Transform door in doorsParent)
        {
            allDoors.Add(door);
        }
        
        // Mapeos de posición hardcodeados basados en posiciones típicas de puertas

        MapDoorByPosition(allDoors, new Vector3(-5f, 1.7f, -3.2f), new Vector2Int(3, 1)); // Posiciones aproximadas
        MapDoorByPosition(allDoors, new Vector3(-3f, 1.7f, -1.2f), new Vector2Int(5, 2));
        MapDoorByPosition(allDoors, new Vector3(-1f, 1.7f, 0.8f), new Vector2Int(2, 3));
        MapDoorByPosition(allDoors, new Vector3(1f, 1.7f, 2.8f), new Vector2Int(4, 4));
        MapDoorByPosition(allDoors, new Vector3(-7f, 1.7f, 2.8f), new Vector2Int(0, 4));
        MapDoorByPosition(allDoors, new Vector3(-3f, 1.7f, 4.8f), new Vector2Int(2, 5));
        MapDoorByPosition(allDoors, new Vector3(-7f, 1.7f, 6.8f), new Vector2Int(0, 6));
        MapDoorByPosition(allDoors, new Vector3(1f, 1.7f, 8.8f), new Vector2Int(4, 7));
        
        Debug.Log($"Mapeo basado en posiciones completado: {gridToDoorMapping.Count} puertas mapeadas");
    }
    
    /// <summary>
    /// Mapea una puerta por su posición, encontrando la puerta más cercana a la posición objetivo
    /// </summary>
    /// <param name="doors">Lista de todas las puertas disponibles</param>
    /// <param name="targetPosition">Posición objetivo donde buscar una puerta</param>
    /// <param name="gridPos">Posición en la cuadrícula donde mapear la puerta encontrada</param>
    private void MapDoorByPosition(List<Transform> doors, Vector3 targetPosition, Vector2Int gridPos)
    {
        Transform closestDoor = null;
        float closestDistance = float.MaxValue;
        
        foreach (Transform door in doors)
        {
            float distance = Vector3.Distance(door.position, targetPosition);
            if (distance < closestDistance && distance < 2f) // Dentro de 2 unidades
            {
                closestDistance = distance;
                closestDoor = door;
            }
        }
        
        if (closestDoor != null)
        {
            gridToDoorMapping[gridPos] = closestDoor.gameObject;
            doorStates[gridPos] = false;
            Debug.Log($" Mapeada puerta '{closestDoor.name}' en {closestDoor.position} a cuadrícula {gridPos} (distancia: {closestDistance:F2})");
        }
        else
        {
            Debug.LogWarning($" No se encontró puerta cerca de la posición objetivo {targetPosition} para cuadrícula {gridPos}");
        }
    }

    /// <summary>
    /// Calcula la dirección de una puerta basándose en las coordenadas de inicio y fin
    /// </summary>
    /// <param name="x1">Coordenada X inicial</param>
    /// <param name="y1">Coordenada Y inicial</param>
    /// <param name="x2">Coordenada X final</param>
    /// <param name="y2">Coordenada Y final</param>
    /// <returns>Dirección como entero (0=Arriba, 1=Derecha, 2=Abajo, 3=Izquierda)</returns>
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
    /// Actualiza la cuadrícula de daño de puertas basándose en eventos de simulación
    /// NOTA: Las puertas ahora se abren automáticamente por proximidad de agentes, no por eventos
    /// </summary>
    /// <param name="json">Datos JSON con el estado actual</param>
    /// <param name="ev">Evento de simulación</param>
    /// <param name="eventIndex">Índice del evento</param>
    public void UpdateDoorDamageGrid(string json, SimulationEvent ev, int eventIndex)
    {
        if (ev == null) return;
        
        // Verificación de seguridad para asegurar que el componente esté inicializado correctamente
        if (gridToDoorMapping == null)
        {
            Debug.LogWarning("DoorDamageGrid no está inicializado correctamente. gridToDoorMapping es null.");
            return;
        }
        
        // COMENTADO: Ya no abrimos puertas basándose en eventos de simulación
        // Las puertas ahora se abren automáticamente cuando los agentes se acercan
        /*
        DoorDamageStateResponse state = JsonConvert.DeserializeObject<DoorDamageStateResponse>(json);

        if (ev.action == "open_door" && ev.step == state.step)
        {
            Vector2Int gridPos = new Vector2Int(ev.Pos1.y, ev.Pos1.x);
            Debug.Log($"Intentando abrir puerta en posición de cuadrícula {gridPos}");
            OpenDoor(gridPos);
        }
        */
    }

    /// <summary>
    /// Abre una puerta en la posición de cuadrícula especificada
    /// Maneja tanto puertas con Animator como sin él
    /// </summary>
    /// <param name="gridPos">Posición en la cuadrícula donde abrir la puerta</param>
    public void OpenDoor(Vector2Int gridPos)
    {
        if (gridToDoorMapping.TryGetValue(gridPos, out GameObject doorObject))
        {
            if (doorStates.TryGetValue(gridPos, out bool isOpen) && !isOpen)
            {
                // Activa la animación de apertura
                Animator animator = doorObject.GetComponent<Animator>();
                if (animator != null)
                {
                    // Intenta encontrar el nombre de trigger correcto si el predeterminado no funciona
                    if (HasTrigger(animator, openAnimationTrigger))
                    {
                        animator.SetTrigger(openAnimationTrigger);
                        doorStates[gridPos] = true;
                        Debug.Log($"Abriendo puerta por proximidad de agente en posición {gridPos}: {doorObject.name}");
                    }
                    else
                    {
                        // Intenta nombres de triggers alternativos comunes
                        string[] commonOpenTriggers = { "Open", "open", "OpenDoor", "DoorOpen", "TriggerOpen" };
                        bool triggerFound = false;
                        
                        foreach (string trigger in commonOpenTriggers)
                        {
                            if (HasTrigger(animator, trigger))
                            {
                                animator.SetTrigger(trigger);
                                doorStates[gridPos] = true;
                                Debug.Log($"Abriendo puerta por proximidad en posición {gridPos}: {doorObject.name} usando trigger: {trigger}");
                                Debug.LogWarning($"Considera actualizar openAnimationTrigger a '{trigger}' en el inspector para mejor rendimiento");
                                triggerFound = true;
                                break;
                            }
                        }
                        
                        if (!triggerFound)
                        {
                            Debug.LogWarning($"No se encontró trigger de apertura adecuado para puerta: {doorObject.name}. Triggers disponibles: {GetAvailableTriggers(animator)}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"No se encontró componente Animator en puerta: {doorObject.name}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"No hay puerta mapeada en posición de cuadrícula {gridPos}");
        }
    }

    /// <summary>
    /// Cierra una puerta en la posición de cuadrícula especificada
    /// </summary>
    /// <param name="gridPos">Posición en la cuadrícula donde cerrar la puerta</param>
    public void CloseDoor(Vector2Int gridPos)
    {
        if (gridToDoorMapping.TryGetValue(gridPos, out GameObject doorObject))
        {
            if (doorStates.TryGetValue(gridPos, out bool isOpen) && isOpen)
            {
                // Activa la animación de cierre
                Animator animator = doorObject.GetComponent<Animator>();
                if (animator != null)
                {
                    // Intenta encontrar el nombre de trigger correcto si el predeterminado no funciona
                    if (HasTrigger(animator, closeAnimationTrigger))
                    {
                        animator.SetTrigger(closeAnimationTrigger);
                        doorStates[gridPos] = false;
                        Debug.Log($"Cerrando puerta en posición de cuadrícula {gridPos}: {doorObject.name}");
                    }
                    else
                    {
                        // Intenta nombres de triggers alternativos comunes
                        string[] commonCloseTriggers = { "Close", "close", "CloseDoor", "DoorClose", "TriggerClose" };
                        bool triggerFound = false;
                        
                        foreach (string trigger in commonCloseTriggers)
                        {
                            if (HasTrigger(animator, trigger))
                            {
                                animator.SetTrigger(trigger);
                                doorStates[gridPos] = false;
                                Debug.Log($"Cerrando puerta en posición de cuadrícula {gridPos}: {doorObject.name} usando trigger: {trigger}");
                                Debug.LogWarning($"Considera actualizar closeAnimationTrigger a '{trigger}' en el inspector para mejor rendimiento");
                                triggerFound = true;
                                break;
                            }
                        }
                        
                        if (!triggerFound)
                        {
                            Debug.LogWarning($"No se encontró trigger de cierre adecuado para puerta: {doorObject.name}. Triggers disponibles: {GetAvailableTriggers(animator)}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"No se encontró componente Animator en puerta: {doorObject.name}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"No hay puerta mapeada en posición de cuadrícula {gridPos}");
        }
    }

    /// <summary>
    /// Método auxiliar para verificar si un animator tiene un trigger específico
    /// </summary>
    /// <param name="animator">El componente Animator a verificar</param>
    /// <param name="triggerName">Nombre del trigger a buscar</param>
    /// <returns>True si el trigger existe, false en caso contrario</returns>
    private bool HasTrigger(Animator animator, string triggerName)
    {
        if (animator.runtimeAnimatorController == null) return false;
        
        foreach (var parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Método auxiliar para obtener todos los triggers disponibles para depuración
    /// </summary>
    /// <param name="animator">El componente Animator del cual obtener los triggers</param>
    /// <returns>String con todos los triggers disponibles separados por comas</returns>
    private string GetAvailableTriggers(Animator animator)
    {
        if (animator.runtimeAnimatorController == null) return "No hay controlador asignado";
        
        List<string> triggers = new List<string>();
        foreach (var parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                triggers.Add(parameter.name);
            }
        }
        return triggers.Count > 0 ? string.Join(", ", triggers) : "No se encontraron triggers";
    }

    /// <summary>
    /// Llena la cuadrícula de daño de puertas basándose en los datos JSON recibidos
    /// NOTA: Comentado para permitir que las puertas se abran solo por proximidad de agentes
    /// </summary>
    /// <param name="json">Datos JSON con el estado de las puertas</param>
    public void FillDoorDamageGrid(string json)
    {
        // COMENTADO: Ya no controlamos las puertas desde JSON
        // Las puertas ahora se controlan automáticamente por proximidad de agentes
        /*
        DoorDamageStateResponse state;
        try
        {
            state = JsonConvert.DeserializeObject<DoorDamageStateResponse>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al parsear JSON en DoorDamageGridManager: " + e.Message);
            return;
        }

        if (state == null || state.doors == null)
        {
            Debug.LogError("State vacío o sin doors en JSON");
            return;
        }

        // Parsea los estados de las puertas desde JSON y actualiza en consecuencia
        HashSet<Vector2Int> openDoors = new HashSet<Vector2Int>();
        foreach (var kv in state.doors)
        {   
            string key = kv.Key.Trim('(', ')');
            string[] parts = key.Split(',');
            int x = int.Parse(parts[0]);
            int y = int.Parse(parts[1]);
            openDoors.Add(new Vector2Int(y, x));
        }    

        // Actualiza los estados de las puertas basándose en los datos JSON
        foreach (var doorMapping in gridToDoorMapping)
        {
            Vector2Int gridPos = doorMapping.Key;
            bool shouldBeOpen = openDoors.Contains(gridPos);
            bool currentlyOpen = doorStates.GetValueOrDefault(gridPos, false);

            if (shouldBeOpen && !currentlyOpen)
            {
                OpenDoor(gridPos);
            }
            else if (!shouldBeOpen && currentlyOpen)
            {
                CloseDoor(gridPos);
            }
        }
        */
    }
}
