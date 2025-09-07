using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Configuración de Cámara")]
    public Camera mainCamera;
    public Transform gameBoard; // Referencia a tu GameBoard para centrar las cámaras alrededor
    
    [Header("Posiciones de Cámara")]
    public float transitionSpeed = 2f;
    public bool smoothTransitions = true;
    
    [Header("Configuración Vista Superior")]
    public float topViewHeight = 10f;
    public float topViewAngle = 90f; // Mirando directamente hacia abajo
    
    [Header("Configuración Vista Lateral")]
    public float sideViewDistance = 10f;  // Más cerca del tablero para más sensación top-down
    public float sideViewHeight = 20f;    // Mucho más alto para más perspectiva aérea
    public float sideViewAngle = 35f;     // Mejor ángulo para vista isométrica
    
    // Estados de cámara
    public enum CameraView
    {
        TopView = 0,
        SideViewLeft = 1,
        SideViewRight = 2
    }
    
    private CameraView currentView = CameraView.TopView;
    private bool isTransitioning = false;
    private float transitionStartTime;
    private float transitionProgress = 0f;
    
    // Posiciones y rotaciones objetivo de la cámara
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    
    // Posiciones iniciales para interpolación suave
    private Vector3 startPosition;
    private Quaternion startRotation;
    
    void Start()
    {
        // Forzar los valores correctos (en caso de que el Inspector tenga valores viejos)
        topViewHeight = 15f;
        sideViewDistance = 10f;
        sideViewHeight = 15f;
        transitionSpeed = 2f;
        
        // Si no hay cámara asignada, usar la cámara principal
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        // Si no hay gameBoard asignado, intentar encontrarlo
        if (gameBoard == null)
        {
            GameObject gameBoardObj = GameObject.Find("GameBoard");
            if (gameBoardObj != null)
                gameBoard = gameBoardObj.transform;
        }
        
        // Información de depuración
        Debug.Log($"CameraController inicializado:");
        Debug.Log($"Cámara Principal: {(mainCamera != null ? mainCamera.name : "NULO")}");
        Debug.Log($"Tablero de Juego: {(gameBoard != null ? gameBoard.name : "NULO")}");
        Debug.Log($"Valores Forzados - Distancia: {sideViewDistance}, Altura: {sideViewHeight}");
        
        // Establecer posición inicial de cámara a vista superior
        SetCameraView(CameraView.TopView, false);
    }
    
    void Update()
    {
        // Manejo de entrada para ciclar cámaras usando el nuevo Sistema de Entrada
        if (Keyboard.current.cKey.wasPressedThisFrame && !isTransitioning)
        {
            CycleCameraView();
        }
        
        // Alternativa: Tecla V para cambio instantáneo de cámara (sin transición suave)
        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            // Forzar completar cualquier transición en curso y ciclar
            isTransitioning = false;
            CycleCameraView();
        }
        
        // Manejar transiciones suaves de cámara
        if (isTransitioning && smoothTransitions)
        {
            // Calcular progreso de transición (0 a 1)
            float transitionDuration = 1f / transitionSpeed;
            transitionProgress = (Time.time - transitionStartTime) / transitionDuration;
            transitionProgress = Mathf.Clamp01(transitionProgress);
            
            // Usar curva suave para movimiento más natural
            float smoothProgress = Mathf.SmoothStep(0f, 1f, transitionProgress);
            
            // Interpolar entre posiciones inicial y objetivo
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, smoothProgress);
            mainCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothProgress);
            
            // Verificar si la transición está completa
            if (transitionProgress >= 1f)
            {
                // Asegurar que estamos exactamente en el objetivo (no se necesita ajuste)
                mainCamera.transform.position = targetPosition;
                mainCamera.transform.rotation = targetRotation;
                isTransitioning = false;
                Debug.Log($"Transición de cámara completada a {currentView}");
            }
        }
    }
    
    public void CycleCameraView()
    {
        // Ciclar a la siguiente vista de cámara
        int nextView = ((int)currentView + 1) % 3;
        SetCameraView((CameraView)nextView, smoothTransitions);
    }
    
    public void SetCameraView(CameraView view, bool animate = true)
    {
        currentView = view;
        
        Vector3 boardCenter = gameBoard != null ? gameBoard.position : Vector3.zero;
        
        switch (view)
        {
            case CameraView.TopView:
                SetTopView(boardCenter);
                break;
            case CameraView.SideViewLeft:
                SetSideView(boardCenter, true);
                break;
            case CameraView.SideViewRight:
                SetSideView(boardCenter, false);
                break;
        }
        
        if (animate && smoothTransitions)
        {
            // Guardar posición y rotación inicial para interpolación suave
            startPosition = mainCamera.transform.position;
            startRotation = mainCamera.transform.rotation;
            
            isTransitioning = true;
            transitionStartTime = Time.time;
            transitionProgress = 0f;
            Debug.Log($"Iniciando transición de cámara a {view}");
        }
        else
        {
            // Transición instantánea
            mainCamera.transform.position = targetPosition;
            mainCamera.transform.rotation = targetRotation;
            isTransitioning = false;
            Debug.Log($"Cambio instantáneo de cámara a {view}");
        }
    }
    
    private void SetTopView(Vector3 boardCenter)
    {
        // Posicionar cámara directamente encima del centro del tablero
        targetPosition = boardCenter + Vector3.up * topViewHeight;
        
        // Mirar directamente hacia abajo
        targetRotation = Quaternion.Euler(90f, 0, 0);
    }
    
    private void SetSideView(Vector3 boardCenter, bool isLeftSide)
    {
        // Calcular posición diagonal lateral pero más cerca y alto para más sensación top-down
        Vector3 sideDirection = isLeftSide ? new Vector3(-0.7f, 0, -0.7f) : new Vector3(0.7f, 0, -0.7f);
        sideDirection = sideDirection.normalized;
        
        // Posicionar cámara más cerca y mucho más alto para perspectiva aérea
        targetPosition = boardCenter + sideDirection * sideViewDistance + Vector3.up * sideViewHeight;
        
        // Mirar hacia abajo al centro del tablero con ángulo pronunciado
        Vector3 directionToBoard = (boardCenter - targetPosition).normalized;
        targetRotation = Quaternion.LookRotation(directionToBoard, Vector3.up);
        
        // Información de depuración
        Debug.Log($"Estableciendo vista lateral {(isLeftSide ? "Izquierda" : "Derecha")}:");
        Debug.Log($"Centro del Tablero: {boardCenter}");
        Debug.Log($"Posición Objetivo: {targetPosition}");
        Debug.Log($"Dirección Lateral: {sideDirection}");
        Debug.Log($"Distancia: {sideViewDistance}, Altura: {sideViewHeight}");
    }
    
    // Métodos públicos para UI u otros scripts
    public void SetTopView() => SetCameraView(CameraView.TopView);
    public void SetLeftSideView() => SetCameraView(CameraView.SideViewLeft);
    public void SetRightSideView() => SetCameraView(CameraView.SideViewRight);
    
    // Obtener información de vista de cámara actual
    public CameraView GetCurrentView() => currentView;
    public string GetCurrentViewName() => currentView.ToString();
    
    // Para depuración - dibujar gizmos en vista de escena
    void OnDrawGizmosSelected()
    {
        if (gameBoard == null) return;
        
        Vector3 boardCenter = gameBoard.position;
        
        // Dibujar posiciones de cámara
        Gizmos.color = Color.yellow;
        
        // Posición vista superior
        Vector3 topPos = boardCenter + Vector3.up * topViewHeight;
        Gizmos.DrawWireSphere(topPos, 0.5f);
        Gizmos.DrawLine(topPos, boardCenter);
        
        // Posición vista lateral izquierda (diagonal pero más aérea)
        Vector3 leftDirection = new Vector3(-0.7f, 0, -0.7f).normalized;
        Vector3 leftPos = boardCenter + leftDirection * sideViewDistance + Vector3.up * sideViewHeight;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(leftPos, 0.5f);
        Gizmos.DrawLine(leftPos, boardCenter);
        
        // Posición vista lateral derecha (diagonal pero más aérea)
        Vector3 rightDirection = new Vector3(0.7f, 0, -0.7f).normalized;
        Vector3 rightPos = boardCenter + rightDirection * sideViewDistance + Vector3.up * sideViewHeight;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(rightPos, 0.5f);
        Gizmos.DrawLine(rightPos, boardCenter);
    }
}