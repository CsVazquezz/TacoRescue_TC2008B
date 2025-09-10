using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public struct DoorCoords
{
    public int x1, y1, x2, y2;

    public DoorCoords(int x1, int y1, int x2, int y2)
    {
        this.x1 = x1; this.y1 = y1;
        this.x2 = x2; this.y2 = y2;
    }
}

[System.Serializable]
public class DoorDamageStateResponse
{
    public Dictionary<string, List<int>> doors { get; set; }
}

public class DoorDamageGrid : MonoBehaviour
{
    public GameObject doorDamagePrefab;

    public Vector3 startPosition = new Vector3(-7f, 1.7f, -5.2f);
    public float cellSize = 2f;

    public Transform gameElementsParent;
    private Transform doorDamageParent;

    private Dictionary<Vector2Int, GameObject> doorDamageObjects = new Dictionary<Vector2Int, GameObject>();

    void Awake()
    {
        if (gameElementsParent != null)
        {
            Transform existing = gameElementsParent.Find("DoorDamages");
            if (existing != null)
            {
                doorDamageParent = existing;
            }
            else
            {
                GameObject doorDamagesGO = new GameObject("DoorDamages");
                doorDamagesGO.transform.SetParent(gameElementsParent);
                doorDamageParent = doorDamagesGO.transform;
            }
        }
        List<DoorCoords> initialDoors = new List<DoorCoords>
        {
            new DoorCoords(3, 1, 3, 2),
            new DoorCoords(5, 2, 5, 3),
            new DoorCoords(2, 3, 1, 3),
            new DoorCoords(4, 4, 4, 5),
            new DoorCoords(0, 4, 0, 5),
            new DoorCoords(2, 5, 2, 6),
            new DoorCoords(0, 6, 0, 7),
            new DoorCoords(4, 7, 3, 7),
        };
        foreach (var pos in initialDoors)
        {
            SpawnDoorDamageObject(new Vector2Int(pos.x1, pos.y1), 1);
            SpawnDoorDamageObject(new Vector2Int(pos.x2, pos.y2), 1);
        }
    }

    public void UpdateDoorDamageGrid(string json, SimulationEvent ev, int eventIndex)
    {
        if (ev == null) return;
        DoorDamageStateResponse state = JsonConvert.DeserializeObject<DoorDamageStateResponse>(json);

        if (ev.action == "open_door" && ev.step == state.step)
        {
            GameObject prefab = doorDamageOnePrefab;
            Vector3 worldPos1 = new Vector3(
                startPosition.x + ev.Pos1.y * cellSize,
                startPosition.y,
                startPosition.z + ev.Pos1.x * cellSize
            );
            Vector3 worldPos2 = new Vector3(
                startPosition.x + ev.Pos2.y * cellSize,
                startPosition.y,
                startPosition.z + ev.Pos2.x * cellSize
            );
            
            GameObject obj1 = Instantiate(prefab, worldPos1, Quaternion.identity);
            GameObject obj2 = Instantiate(prefab, worldPos2, Quaternion.identity);

            obj1.name = $"DoorDamage({ev.Pos1.y},{ev.Pos1.x})";
            obj2.name = $"DoorDamage({ev.Pos2.y},{ev.Pos2.y})";

            if (doorDamageParent != null)
            {
                obj1.transform.SetParent(doorDamageParent);
                obj2.transform.SetParent(doorDamageParent);
            }

            doorDamageObjects[new Vector2Int(ev.Pos1.x, ev.Pos1.y)] = obj1;
            doorDamageObjects[new Vector2Int(ev.Pos2.x, ev.Pos2.y)] = obj1;
        }
    }

    public void FillDoorDamageGrid(string json)
    {
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

        List<Vector2Int> keysToRemove = new List<Vector2Int>();
        foreach (var kv in state.doors)
        {   
            string strKey = $"{kv.Key.y},{kv.Key.x}";
            if (!doorDamageObjects.ContainsKey(strKey))
            {
                Destroy(doorDamageObjects[pos]);
                keysToRemove.Add(kv.Key);
            }
        }

        foreach (var k in keysToRemove)
            doorDamageObjects.Remove(k);
    }

    private void SpawnDoorDamageObject(Vector2Int gridPos, int option)
    {
        GameObject prefab1 = doorPrefab;
        GameObject prefab2 = doorOpenPrefab;

        Vector3 worldPos = new Vector3(
            startPosition.x + gridPos.y * cellSize,
            startPosition.y,
            startPosition.z + gridPos.x * cellSize
        );
        
        GameObject obj = option == 1 ? Instantiate(prefab1, worldPos, Quaternion.identity) : Instantiate(prefab2, worldPos, Quaternion.identity);

        obj.name = $"DoorDamage({gridPos.y},{gridPos.x})";

        if (doorDamageParent != null)
        {
            obj.transform.SetParent(doorDamageParent);
        }

        doorDamageObjects[new Vector2Int(gridPos.x, gridPos.y)] = obj;
    }
}
