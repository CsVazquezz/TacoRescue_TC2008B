using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TacoRescueController : MonoBehaviour
{
    private string baseUrl = "https://tacorescue-tc2008b.onrender.com";
    // Botón para ver el step y el estatus
    public Button stepButton;

    public FireGrid fireGridManager;
    public AgentGrid agentGridManager;

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

                agentGridManager.UpdateAgents(json);
                fireGridManager.UpdateFireGrid(json);
            }
        }
    }
}
