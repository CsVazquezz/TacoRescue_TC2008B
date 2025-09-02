using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class StateResponse
{
    public List<List<float>> fire; // matriz fire que viene en el JSON
}

public class FireGrid : MonoBehaviour
{
    public GameObject firePrefab;
    public GameObject smokePrefab;

    public Vector3 startPosition = new Vector3(-7.1f, 1.7f, -5.2f);
    public float cellSize = 2f;

    public Transform gameElementsParent;
    private Transform firesParent;

    private Dictionary<Vector2Int, GameObject> fireObjects = new Dictionary<Vector2Int, GameObject>();

    void Awake()
    {
        if (gameElementsParent != null)
        {
            Transform existing = gameElementsParent.Find("Fires");
            if (existing != null)
            {
                firesParent = existing;
            }
            else
            {
                GameObject firesGO = new GameObject("Fires");
                firesGO.transform.SetParent(gameElementsParent);
                firesParent = firesGO.transform;
            }
        }
    }

    public void UpdateFireGrid(string json)
    {
        StateResponse state;
        try
        {
            state = JsonConvert.DeserializeObject<StateResponse>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al parsear JSON en FireGridManager: " + e.Message);
            return;
        }

        if (state == null || state.fire == null)
        {
            Debug.LogError("State vacío o sin fire en JSON");
            return;
        }

        for (int y = 0; y < state.fire.Count; y++)
        {
            for (int x = 0; x < state.fire[y].Count; x++)
            {
                int value = (int)state.fire[y][x];
                Vector2Int pos = new Vector2Int(x, y);

                if (fireObjects.ContainsKey(pos))
                {
                    if (value == 0)
                    {
                        Destroy(fireObjects[pos]);
                        fireObjects.Remove(pos);
                    }
                    else
                    {
                        string currentTag = fireObjects[pos].tag;
                        if ((value == 1 && currentTag != "Smoke") || (value == 2 && currentTag != "Fire"))
                        {
                            Destroy(fireObjects[pos]);
                            fireObjects.Remove(pos);
                            SpawnFireObject(value, pos);
                        }
                    }
                }
                else
                {
                    if (value != 0)
                    {
                        SpawnFireObject(value, pos);
                    }
                }
            }
        }
    }

    private void SpawnFireObject(int value, Vector2Int gridPos)
    {
        GameObject prefab = (value == 1) ? smokePrefab : firePrefab;

        Vector3 worldPos = new Vector3(
            startPosition.x + gridPos.y * cellSize,
            startPosition.y,
            startPosition.z + gridPos.x * cellSize
        );

        GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity);

        // Nombrar el objeto con sus coords
        string typeName = (value == 1) ? "Smoke" : "Fire";
        obj.name = $"{typeName}({gridPos.y},{gridPos.x})";

        // Ponerlo como hijo de Fires
        if (firesParent != null)
        {
            obj.transform.SetParent(firesParent);
        }

        fireObjects[gridPos] = obj;
    }
}
