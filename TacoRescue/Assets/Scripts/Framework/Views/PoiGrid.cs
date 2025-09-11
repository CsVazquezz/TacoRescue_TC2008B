using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class PoiStateResponse
{
    public int step;
    public List<List<float>> poi;
    public List<SimulationEvent> events;
}

public class PoiGrid : MonoBehaviour
{
    public GameObject poiPrefab;
    public GameObject victimPrefab;
    public GameObject falseAlarmPrefab;

    public Vector3 startPosition = new Vector3(-7f, 1.7f, -5.2f);
    public float cellSize = 2f;

    public Transform gameElementsParent;
    private Transform poiParent;

    private Dictionary<Vector2Int, GameObject> poiObjects = new Dictionary<Vector2Int, GameObject>();

    void Awake()
    {
        if (gameElementsParent != null)
        {
            Transform existing = gameElementsParent.Find("POI");
            if (existing != null)
            {
                poiParent = existing;
            }
            else
            {
                GameObject poiGo = new GameObject("POI");
                poiGo.transform.SetParent(gameElementsParent);
                poiParent = poiGo.transform;
            }
        }
        List<Vector2Int> initialPois = new List<Vector2Int>
        {
            new Vector2Int(4, 3),
            new Vector2Int(1, 7),
            new Vector2Int(1, 0)
        };

        foreach (var pos in initialPois)
        {
            if (!poiObjects.ContainsKey(pos))
            {
                SpawnPoiObject(pos);
            }
        }
    }

    public void UpdatePoiGrid(string json, SimulationEvent ev, int eventIndex)
    {
        if (ev == null) return;
        PoiStateResponse state = JsonConvert.DeserializeObject<PoiStateResponse>(json);
        Vector2Int pos = new Vector2Int(ev.x, ev.y);

        if (ev.action == "pick_up_victim" && ev.step == state.step)
        {
            if (poiObjects.ContainsKey(pos))
            {
                Destroy(poiObjects[pos]); // Remover PoiPrefab
                poiObjects.Remove(pos);
                RevealPoiObject(1, pos); // Revelar victimPrefab
                Invoke(nameof(RemovePoiAtPos), 3f); // Esperar 3 segundos
                void RemovePoiAtPos()
                {
                    if (poiObjects.ContainsKey(pos))
                    {
                        Destroy(poiObjects[pos]);
                        poiObjects.Remove(pos);
                    }
                }
            }
        }
        else if (ev.action == "remove_false_alarm")
        {
            if (poiObjects.ContainsKey(pos))
            {
                Destroy(poiObjects[pos]); // Remover PoiPrefab
                poiObjects.Remove(pos);
                RevealPoiObject(2, pos); // Revelar falseAlarmPrefab
                Invoke(nameof(RemovePoiAtPos), 3f); // Esperar 3 segundos
                void RemovePoiAtPos()
                {
                    if (poiObjects.ContainsKey(pos))
                    {
                        Destroy(poiObjects[pos]);
                        poiObjects.Remove(pos);
                    }
                }
            }
        }
    }

    public void ReplenishPoiGrid(string json)
    {
        PoiStateResponse state;
        try
        {
            state = JsonConvert.DeserializeObject<PoiStateResponse>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al parsear JSON en PoiGridManager: " + e.Message);
            return;
        }

        if (state == null || state.poi == null)
        {
            Debug.LogError("State vacío o sin poi en JSON");
            return;
        }

        for (int y = 0; y < state.poi.Count; y++)
        {
            for (int x = 0; x < state.poi[y].Count; x++)
            {
                int value = (int)state.poi[y][x];
                Vector2Int pos = new Vector2Int(x, y);

                if (poiObjects.ContainsKey(pos))
                {
                    if (value == 0.0)
                    {
                        Destroy(poiObjects[pos]);
                        poiObjects.Remove(pos);
                    }
                }
                else
                {
                    if (value != 0.0)
                    {
                        SpawnPoiObject(pos);
                    }
                }
            }
        }
    }

    private void SpawnPoiObject(Vector2Int gridPos)
    {
        GameObject prefab = poiPrefab;

        Vector3 worldPos = new Vector3(
            startPosition.x + gridPos.y * cellSize,
            startPosition.y,
            startPosition.z + gridPos.x * cellSize
        );

        GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity);

        string typeName = "POI";
        obj.name = $"{typeName}({gridPos.y},{gridPos.x})";

        if (poiParent != null)
        {
            obj.transform.SetParent(poiParent);
        }

        poiObjects[gridPos] = obj;
    }

    private void RevealPoiObject(int value, Vector2Int gridPos)
    {
        GameObject prefab = (value == 1) ? victimPrefab : falseAlarmPrefab;

        Vector3 worldPos = new Vector3(
            startPosition.x + gridPos.y * cellSize,
            startPosition.y,
            startPosition.z + gridPos.x * cellSize
        );

        GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity);

        string typeName = (value == 1) ? "Victim" : "FalseAlarm";
        obj.name = $"{typeName}({gridPos.y},{gridPos.x})";

        if (poiParent != null)
        {
            obj.transform.SetParent(poiParent);
        }

        poiObjects[gridPos] = obj;
    }
}
