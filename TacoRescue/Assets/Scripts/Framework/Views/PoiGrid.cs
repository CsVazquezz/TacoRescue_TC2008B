using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class PoiStateResponse
{
    public List<List<float>> poi;
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
    }

    public void UpdatePoiGrid(string json)
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
                    if (value == 0)
                    {
                        Destroy(poiObjects[pos]);
                        poiObjects.Remove(pos);
                    }
                    else
                    {
                        string currentTag = poiObjects[pos].tag;
                        if ((value == 1 && currentTag != "Victim") || (value == 2 && currentTag != "FalseAlarm"))
                        {
                            Destroy(poiObjects[pos]);
                            poiObjects.Remove(pos);
                            SpawnPoiObject(value, pos);
                        }
                    }
                }
                else
                {
                    if (value != 0)
                    {
                        SpawnPoiObject(value, pos);
                    }
                }
            }
        }
    }

    private void SpawnPoiObject(int value, Vector2Int gridPos)
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
