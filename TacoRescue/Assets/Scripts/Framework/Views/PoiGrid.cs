using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class PoiStateResponse
{
    public int step;
    public List<List<int>> poi;
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
        Vector2Int pos = new Vector2Int(ev.y, ev.x);

        if (ev.action == "pick_up_victim" && ev.step == state.step)
        {
            if (poiObjects.ContainsKey(pos))
            {
                Destroy(poiObjects[pos]); // Remover PoiPrefab
                poiObjects.Remove(pos);
                GameObject revealed = RevealPoiObject(1, pos);  // Revelar victimPrefab
                Debug.Log("Victim prefab revealed: " + revealed);
                StartCoroutine(RemoveRevealedAfterDelay(revealed, 3f)); // Esperar 3 segundos
            }
        }
        else if (ev.action == "remove_false_alarm")
        {
            if (poiObjects.ContainsKey(pos))
            {
                Destroy(poiObjects[pos]); // Remover PoiPrefab
                poiObjects.Remove(pos);
                GameObject revealed = RevealPoiObject(2, pos); // Revelar falseAlarmPrefab
                Debug.Log("False alarm prefab revealed");
                StartCoroutine(RemoveRevealedAfterDelay(revealed, 3f)); // Esperar 3 segundos
            }
        }
    }

    public IEnumerator RemoveRevealedAfterDelay (GameObject revealedObj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (revealedObj != null)
        {
            Destroy(revealedObj);
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

        foreach (var obj in poiObjects.Values)
        {
            if (obj != null) Destroy(obj);
        }
        poiObjects.Clear();

        foreach (var coord in state.poi)
        {
            if (coord.Count == 2)
            {
                Vector2Int pos = new Vector2Int(coord[1], coord[0]);
                SpawnPoiObject(pos);
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

    private GameObject  RevealPoiObject(int value, Vector2Int gridPos)
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

        return obj;
    }
}
