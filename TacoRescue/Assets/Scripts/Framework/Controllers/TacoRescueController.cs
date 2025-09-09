using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;

[System.Serializable]
public class StateResponse
{
    public List<AgentData> agents;
    public List<List<float>> fire;
    public List<List<float>> poi;
    public List<SimulationEvent> events;
}

[System.Serializable]
public class SimulationEvent
{
    public string action;
    public int step;
    public List<int> pos;
    public List<int> pos1;
    public List<int> pos2; 

    public int x => pos != null && pos.Count > 0 ? pos[0] : 0;
    public int y => pos != null && pos.Count > 1 ? pos[1] : 0;
    public Vector2Int Pos1 => pos1 != null && pos1.Count == 2 ? new Vector2Int(pos1[0], pos1[1]) : Vector2Int.zero;
    public Vector2Int Pos2 => pos2 != null && pos2.Count == 2 ? new Vector2Int(pos2[0], pos2[1]) : Vector2Int.zero;
}

public class TacoRescueController : MonoBehaviour
{
    private string baseUrl = "https://tacorescue-tc2008b.onrender.com";
    // Botón para ver el step y el estatus
    public Button stepButton;

    public FireGrid fireGridManager;
    public AgentGrid agentGridManager;
    public PoiGrid poiGridManager;
    public WallDamageGrid wallDamageGridManager;
    public DoorDamageGrid doorDamageGridManager;
    
    private int currentEventIndex = 0;

    void Start()
    {
        // Agregar addlistener al presionarlo al botón
        stepButton.onClick.AddListener(OnStepButtonPressed);
    }

    void OnStepButtonPressed()
    {
        // llamar StepAndGetState coroutine al dar click al botón
        StartCoroutine(StepAndGetState());
    }

    public IEnumerator StepAndGetState()
    {
        yield return StepSimulation();
        yield return GetState();
    }
    
    // para la simulación como solicitudes web sin pausar el juego
    public IEnumerator StepSimulation()
    {
        // solicitud POST: step. PostWwwForm envia datos pero está vacío ""
        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(baseUrl + "/step", ""))
        {
            // envia la solicitud y espera a que termine
            yield return www.SendWebRequest();
            // Si hay problema
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Error POST /step: " + www.error);
            }
            // Si no
            else
            {
                Debug.Log("POST (step): " + www.downloadHandler.text);
            }
        }
    }

    // obtener datos del servidor sin bloquear Unity.
    public IEnumerator GetState()
    {
        // para GET: state
        using (UnityWebRequest www = UnityWebRequest.Get(baseUrl + "/state"))
        {
            // envia la solicitud y espera hasta que devuelvan la respuesta
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Error GET /state: " + www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log("GET JSON (state): " + json);

                StateResponse state = JsonConvert.DeserializeObject<StateResponse>(json);
                if (state == null) yield break;

                if (state.events != null && currentEventIndex < state.events.Count)
                {
                    SimulationEvent ev = state.events[currentEventIndex];
                    Debug.Log($"Procesando  el evento {currentEventIndex}: {ev.action} en ({ev.x},{ev.y})");

                    agentGridManager.UpdateAgents(json, ev, currentEventIndex);
                    fireGridManager.UpdateFireGrid(json, ev, currentEventIndex);
                    doorDamageGridManager.UpdateDoorDamageGrid(json, ev, currentEventIndex);
                    wallDamageGridManager.UpdateWallDamageGrid(json, ev, currentEventIndex);
                    poiGridManager.UpdatePoiGrid(json, ev, currentEventIndex);

                    currentEventIndex++;
                }
                fireGridManager.FillFireGrid(json);
                doorDamageGridManager.FillDoorDamageGrid(json);
                wallDamageGridManager.FillWallDamageGrid(json);
                poiGridManager.ReplenishPoiGrid(json);
            }
        }
    }
}
