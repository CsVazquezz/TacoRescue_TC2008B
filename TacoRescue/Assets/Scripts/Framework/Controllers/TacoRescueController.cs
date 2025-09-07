using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;

public class TacoRescueController : MonoBehaviour
{
    private string baseUrl = "https://tacorescue-tc2008b.onrender.com";
    // Botón para ver el step y el estatus
    public Button stepButton;

    public FireGrid fireGridManager;
    public AgentGrid agentGridManager;
    public PoiGrid poiGridManager;
    
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
                    poiGridManager.UpdatePoiGrid(json, ev, currentEventIndex);

                    currentEventIndex++;
                }
                else
                {
                    Debug.Log("No new events to process.");
                }
                poiGridManager.ReplenishPoiGrid(json);
            }
        }
    }
}
