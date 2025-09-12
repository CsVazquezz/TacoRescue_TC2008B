using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class WallDamageStateResponse
{
    public int step;
    public List<List<List<float>>> walls_damage { get; set; }
}

public class WallDamageGrid : MonoBehaviour
{
    public GameObject damageOnePrefab;
    public GameObject damageTwoPrefab;

    public Vector3 startPosition = new Vector3(-7f, 1.7f, -5.2f);
    public float cellSize = 2f;

    public Transform gameElementsParent;
    private Transform wallDamageParent;

    private Dictionary<Vector3Int, GameObject> wallDamageObjects = new Dictionary<Vector3Int, GameObject>();

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
    }

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
            SpawnWallDamageObject(dir1, 1.0, new Vector2Int(ev.Pos1.y, ev.Pos1.x));
            SpawnWallDamageObject(dir2, 1.0, new Vector2Int(ev.Pos2.y, ev.Pos2.x));
        }
        else if (ev.action == "demolish_wall" && ev.step == state.step)
        {
            if (wallDamageObjects.ContainsKey(wallKey1))
            {
                Destroy(wallDamageObjects[wallKey1]);
                wallDamageObjects.Remove(wallKey1);
            }
            if (wallDamageObjects.ContainsKey(wallKey2))
            {
                Destroy(wallDamageObjects[wallKey2]);
                wallDamageObjects.Remove(wallKey2);
            }
            SpawnWallDamageObject(dir1, 2.0, new Vector2Int(ev.Pos1.y, ev.Pos1.x));
            SpawnWallDamageObject(dir2, 2.0, new Vector2Int(ev.Pos2.y, ev.Pos2.x));
        }
    }

    public int CalcularDireccion(int x1, int y1, int x2, int y2) 
    {
        int dx = x2 - x1;
        int dy = y2 - y1;
        int dir = -1;
        if (dx == 1)
        {
            dir = 0;
        }
        else if (dx == -1)
        {
            dir = 2;
        }
        else if (dy == 1)
        {
            dir = 1;
        }
        else if (dy == -1)
        {
            dir = 3;
        }
        return dir;
    }

    public void FillWallDamageGrid(string json)
    {
        WallDamageStateResponse state;
        try
        {
            state = JsonConvert.DeserializeObject<WallDamageStateResponse>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al parsear JSON en WallDamageGridManager: " + e.Message);
            return;
        }

        if (state == null || state.walls_damage == null)
        {
            Debug.LogError("State vacío o sin walls_damage en JSON");
            return;
        }

        for (int y = 0; y < state.walls_damage.Count; y++)
        {
            for (int x = 0; x < state.walls_damage[y].Count; x++)
            {
                List<float> wallValues = state.walls_damage[y][x];

                for (int dir = 0; dir < wallValues.Count; dir++)
                {
                    float value = wallValues[dir];
                    Vector3Int wallKey = new Vector3Int(x, y, dir);
                    if (value == 1.0)
                    {
                        if (!wallDamageObjects.ContainsKey(wallKey)) 
                        {
                            SpawnWallDamageObject(dir, value, new Vector2Int(x, y));
                        }
                    }
                    else if (value == 2.0)
                    {
                        if (wallDamageObjects.ContainsKey(wallKey)) 
                        {
                            Destroy(wallDamageObjects[wallKey]);
                            wallDamageObjects.Remove(wallKey);
                        }
                        SpawnWallDamageObject(dir, value, new Vector2Int(x, y));
                    }
                }
            }
        }
    }

    private void SpawnWallDamageObject(int direction, double value, Vector2Int gridPos)
    {
        GameObject prefab1 = damageOnePrefab;
        GameObject prefab2 = damageTwoPrefab;

        Vector3 worldPos = new Vector3(
            startPosition.x + gridPos.y * cellSize,
            startPosition.y,
            startPosition.z + gridPos.x * cellSize
        );

        switch (direction)
        {
            case 0: worldPos += new Vector3(0, 0, cellSize / 2); break; // Arriba
            case 1: worldPos += new Vector3(cellSize / 2, 0, 0); break; // Derecha
            case 2: worldPos += new Vector3(0, 0, -cellSize / 2); break; // Abajo
            case 3: worldPos += new Vector3(-cellSize / 2, 0, 0); break; // Izquierda 
        }
        
        GameObject obj = value == 1.0 ? Instantiate(prefab1, worldPos, Quaternion.identity) : Instantiate(prefab2, worldPos, Quaternion.identity);

        obj.name = $"WallDamage({gridPos.y},{gridPos.x},Dir:{direction})";

        if (wallDamageParent != null)
        {
            obj.transform.SetParent(wallDamageParent);
        }

        wallDamageObjects[new Vector3Int(gridPos.x, gridPos.y, direction)] = obj;
    }
}
