using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class AgentData
{
    public int id;
    public int x;
    public int y;
    public bool carrying_victim;
}

[System.Serializable]
public class AgentStateResponse
{
    public int step;
    public List<AgentData> agents;
    public List<SimulationEvent> events;
}

public class AgentGrid : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject agentPrefab;

    [Header("Grid Settings")]
    public Vector3 startPosition = new Vector3(-7f, 1.7f, -5.2f);
    public float cellSize = 2f;
    public float moveSpeed = 5f;

    [Header("Hierarchy Parents")]
    public Transform gameElementsParent;
    private Transform agentsParent;
    private Dictionary<int, Coroutine> agentCoroutines = new Dictionary<int, Coroutine>();

    private Dictionary<int, GameObject> agentObjects = new Dictionary<int, GameObject>();

    void Awake()
    {
        if (gameElementsParent != null)
        {
            Transform existing = gameElementsParent.Find("Players");
            if (existing != null)
            {
                agentsParent = existing;
            }
            else
            {
                GameObject agentsGO = new GameObject("Players");
                agentsGO.transform.SetParent(gameElementsParent);
                agentsParent = agentsGO.transform;
            }
        }
        List<Vector2Int> initialAgents = new List<Vector2Int>
        {3 0 2 2 0 1
            new Vector2Int(0, 2), 3
            new Vector2Int(5, 5), 0
            new Vector2Int(2, 7), 2
            new Vector2Int(2, 7), 2
            new Vector2Int(5, 5), 0
            new Vector2Int(3, 0), 1
        };
        int id = 0;
        foreach (var agent in initialAgents)
        {
            Vector3 targetPos = new Vector3(
                startPosition.x + agent.y * cellSize,
                startPosition.y,
                startPosition.z + agent.x * cellSize
            );

            if (!agentObjects.ContainsKey(id))
            {
                GameObject obj = Instantiate(agentPrefab, targetPos, Quaternion.identity);
                obj.name = $"Agent{id}";

                if (agentsParent != null)
                    obj.transform.SetParent(agentsParent);

                agentObjects[id] = obj;
                id ++;
            }
        }
    }

    public void UpdateAgents(string json, SimulationEvent ev, int eventIndex)
    {
        if (ev == null) return;

        AgentStateResponse state;
        try
        {
            state = JsonConvert.DeserializeObject<AgentStateResponse>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al parsear JSON en AgentGridManager: " + e.Message);
            return;
        }

        if (state == null || state.agents == null)
        {
            Debug.LogError("State vacío o sin agents en JSON");
            return;
        }

        AgentData agentData = state.agents.Find(a => a.id == ev.id);
        if (agentData == null) return;

        GameObject agentObj = agentObjects[agentData.id];
        Debug.Log($"Update Agent Grid ev.step: {ev.step} state.step: {state.step} ev.action: {ev.action}");

        if ( (ev.action == "move" || ev.action == "knock_out") && ev.step == state.step)
        {
            Vector3 targetPos = new Vector3(
                startPosition.x + ev.x * cellSize,
                startPosition.y,
                startPosition.z + ev.y * cellSize
            );
            if (agentCoroutines.ContainsKey(agentData.id) && agentCoroutines[agentData.id] != null)
            {
                StopCoroutine(agentCoroutines[agentData.id]);
            }
            agentCoroutines[agentData.id] = StartCoroutine(MoveAgent(agentObj.transform, targetPos, agentData.id));;
        } 
        else if (ev.action == "knock_out")
        {
            Vector3 targetPos = new Vector3(
                startPosition.x + ev.x * cellSize,
                startPosition.y,
                startPosition.z + ev.y * cellSize
            );
            if (agentCoroutines.ContainsKey(agentData.id) && agentCoroutines[agentData.id] != null)
            {
                StopCoroutine(agentCoroutines[agentData.id]);
            }
            agentCoroutines[agentData.id] = StartCoroutine(MoveAgent(agentObj.transform, targetPos, agentData.id));;
        }
        else if (ev.action == "drop_off_victim")
        {
            // si solo si se necesita
        }
    }

    private System.Collections.IEnumerator MoveAgent(Transform agentTransform, Vector3 targetPos, int agentId)
    {
        while (Vector3.Distance(agentTransform.position, targetPos) > 0.01f)
        {
            agentTransform.position = Vector3.MoveTowards(
                agentTransform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }
        agentTransform.position = targetPos;
        agentCoroutines[agentId] = null;
    }
}
