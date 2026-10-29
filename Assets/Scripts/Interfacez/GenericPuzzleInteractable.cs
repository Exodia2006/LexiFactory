using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lead Gameplay Architecture:
/// Controlador base para objetos interactuables de tipo puzzle.
/// Integra transiciones suaves de cámara (Player Camera <-> Puzzle Camera),
/// limpieza rigurosa de estado en descarga de escena (OnDestroy/OnDisable),
/// e inicialización blindada al cargar o reiniciar el nivel.
/// </summary>
public class GenericPuzzleInteractable : MonoBehaviour, IInteractable
{
    [Header("Cartel de Interacción (Prompt UI)")]
    [Tooltip("Contenedor general del cartel flotante de interacción")]
    [SerializeField] private GameObject promptContainer;
    [Tooltip("Imagen donde se renderiza el icono/sprite de la tecla [E]")]
    [SerializeField] private Image keyIconImage;
    [Tooltip("Sprite opcional de la tecla [E]")]
    [SerializeField] private Sprite keyIconSprite;
    [Tooltip("Texto opcional de la acción")]
    [SerializeField] private TextMeshProUGUI actionDescriptionText;
    [SerializeField] private string actionDescription = "Interactuar";

    [Header("Collider de Interacción (Opcional)")]
    [Tooltip("Collider de interacción específico. Si se asigna, se desactivará al resolver.")]
    [SerializeField] private Collider interactionCollider;

    [Header("World Space Canvas (Compatibilidad Anterior)")]
    [SerializeField] private GameObject promptCanvas; // Texto 3D "Presiona E"
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string hoverMessage = "Presiona E para ver instrucciones";

    [Header("Canvas de Instrucciones del Puzzle")]
    [SerializeField] private GameObject instructionCanvas;

    [Header("Configuración del Código")]
    [SerializeField] private int codeDigitIndex = 0; // Configurar en Inspector: 0, 1, 2 o 3

    [Header("Cámaras (Opcional)")]
    [Tooltip("Cámara en primera persona del jugador")]
    [SerializeField] private Camera mainPlayerCamera;
    [Tooltip("Cámara dedicada que enfoca el puzzle (TV, Escritorio, etc.)")]
    [SerializeField] private Camera puzzleCamera;

    [Header("Transición Suave de Cámara")]
    [Tooltip("Duración de la transición entre la cámara del jugador y la del puzzle")]
    [Range(0.1f, 2f)]
    [SerializeField] private float cameraTransitionDuration = 0.6f;
    [Tooltip("Habilitar interpolación suave mediante CameraTransitionManager")]
    [SerializeField] private bool useSmoothCameraTransition = true;

    [Header("Scripts del Jugador a Desactivar")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour mouseLookScript;

    public bool IsSolved { get; private set; } = false;
    public bool IsTransitioning => isTransitioning;

    private bool isInstructionOpen = false;
    private bool isInPuzzleView = false;
    private bool isInFirstPersonPuzzle = false; // Puzzle sin cámara propia (ej: Caja de Juguetes) en curso
    private bool isPlayerLooking = false;
    private bool isTransitioning = false;
    private bool canProcessInput = true; // Control para ignorar la 'E' del mismo frame

    private void Awake()
    {
        // Compatibilidad defensiva: Si promptContainer no está asignado pero promptCanvas sí, usarlo
        if (promptContainer == null && promptCanvas != null)
            promptContainer = promptCanvas;

        if (actionDescriptionText == null && promptText != null)
            actionDescriptionText = promptText;

        if (keyIconImage != null && keyIconSprite != null)
            keyIconImage.sprite = keyIconSprite;

        ResolvePlayerCamera();
    }

    private void Start()
    {
        // Auto-detectar scripts de movimiento del jugador si están sin asignar en Inspector
        if (playerMovementScript == null)
            playerMovementScript = FindFirstObjectByType<PlayerMovement>();

        if (mouseLookScript == null)
            mouseLookScript = FindFirstObjectByType<MouseLook>();

        // Forzar inicialización limpia y blindada en Start de la escena
        EnsureCleanState();

        // Asegurar que los controles del jugador comiencen activos al iniciar la escena
        SetPlayerControls(true);

        if (actionDescriptionText != null)
            actionDescriptionText.text = !string.IsNullOrEmpty(actionDescription) ? actionDescription : hoverMessage;
        else if (promptText != null)
            promptText.text = hoverMessage;
    }

    private void OnDisable()
    {
        CleanupOnExit();
    }

    private void OnDestroy()
    {
        CleanupOnExit();
    }

    /// <summary>
    /// Limpieza completa al descargar la escena o deshabilitar el objeto:
    /// Libera registros estáticos en GameManager, detiene corrutinas y apaga cámaras secundarias.
    /// </summary>
    private void CleanupOnExit()
    {
        GameManager.UnregisterActivePuzzle(this);
        StopAllCoroutines();

        isTransitioning = false;
        isInPuzzleView = false;
        isInFirstPersonPuzzle = false;
        isInstructionOpen = false;

        if (puzzleCamera != null)
            puzzleCamera.gameObject.SetActive(false);

        if (instructionCanvas != null)
            instructionCanvas.SetActive(false);
    }

    /// <summary>
    /// Restablece el puzzle a un estado base listo para interacción al entrar o reiniciar el nivel.
    /// </summary>
    public void EnsureCleanState()
    {
        isTransitioning = false;
        isInPuzzleView = false;
        isInFirstPersonPuzzle = false;
        isInstructionOpen = false;
        canProcessInput = true;
        isPlayerLooking = false;

        if (puzzleCamera != null)
            puzzleCamera.gameObject.SetActive(false);

        if (instructionCanvas != null)
            instructionCanvas.SetActive(false);

        if (!IsSolved)
        {
            if (interactionCollider != null)
                interactionCollider.enabled = true;
        }

        HidePrompt();
    }

    private void Update()
    {
        if (IsSolved || isTransitioning) return;

        // 1. Orientar cartel flotante hacia la cámara
        if (isPlayerLooking && !isInstructionOpen && !isInPuzzleView)
        {
            Camera activeCam = GetActivePlayerCamera();
            GameObject targetToRotate = promptContainer != null ? promptContainer : promptCanvas;
            if (targetToRotate != null && activeCam != null)
            {
                targetToRotate.transform.rotation = Quaternion.LookRotation(
                    targetToRotate.transform.position - activeCam.transform.position
                );
            }
        }

        if (!canProcessInput) return;

        // 2. Control en el panel de Instrucciones
        if (isInstructionOpen)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                OnStartButtonPressed();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameManager.ConsumeEscape();
                CloseInstructions();
                return;
            }
        }

        // 3. Control mientras está en la cámara del reto (TV, Escritorio, etc.)
        if (isInPuzzleView || isInFirstPersonPuzzle)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameManager.ConsumeEscape();
                ExitPuzzle();
                return;
            }
        }
    }

    // --- INTERFAZ IINTERACTABLE ---
    public string GetHoverText() => IsSolved ? string.Empty : (!string.IsNullOrEmpty(actionDescription) ? actionDescription : hoverMessage);

    public void OnHandEnter()
    {
        if (IsSolved || isInstructionOpen || isInPuzzleView || isInFirstPersonPuzzle || isTransitioning) return;
        if (GameManager.IsAnyPuzzleActive && !GameManager.IsThisPuzzleActive(this)) return;

        isPlayerLooking = true;
        ShowPrompt();
    }

    public void OnHandExit()
    {
        isPlayerLooking = false;
        HidePrompt();
    }

    public void Interact()
    {
        if (IsSolved || isTransitioning) return;
        if (GameManager.IsAnyPuzzleActive && !GameManager.IsThisPuzzleActive(this))
        {
            Debug.LogWarning("[GenericPuzzleInteractable] Ya hay otro puzzle en progreso. Termina o sal del actual primero.");
            return;
        }

        if (!isInstructionOpen && !isInPuzzleView && !isInFirstPersonPuzzle)
        {
            OpenInstructions();
        }
    }

    // --- GESTIÓN DE PROMPTS Y BLOQUEO ---
    public void ShowPrompt()
    {
        if (IsSolved || isInstructionOpen || isInPuzzleView || isInFirstPersonPuzzle || isTransitioning) return;
        if (GameManager.IsAnyPuzzleActive && !GameManager.IsThisPuzzleActive(this)) return;

        if (promptContainer != null)
            promptContainer.SetActive(true);

        if (keyIconImage != null)
            keyIconImage.gameObject.SetActive(true);

        if (actionDescriptionText != null)
        {
            actionDescriptionText.text = !string.IsNullOrEmpty(actionDescription) ? actionDescription : hoverMessage;
        }
    }

    public void HidePrompt()
    {
        if (promptContainer != null)
            promptContainer.SetActive(false);

        if (promptCanvas != null && promptCanvas != promptContainer)
            promptCanvas.SetActive(false);
    }

    public void MarkAsSolved()
    {
        if (IsSolved) return;

        IsSolved = true;
        isPlayerLooking = false;
        isInFirstPersonPuzzle = false;
        GameManager.UnregisterActivePuzzle(this);

        // 1. Forzar apagado inmediato del cartel de interacción
        HidePrompt();

        // 2. Desactivar collider de interacción asignado
        if (interactionCollider != null)
        {
            interactionCollider.enabled = false;
        }

        // 3. Desactivar colliders de tipo Trigger en el objeto y sus hijos
        Collider[] allColliders = GetComponents<Collider>();
        foreach (var col in allColliders)
        {
            if (col != null && col.isTrigger)
            {
                col.enabled = false;
            }
        }

        Collider[] childColliders = GetComponentsInChildren<Collider>();
        foreach (var col in childColliders)
        {
            if (col != null && col.isTrigger)
            {
                col.enabled = false;
            }
        }

        // 4. Cambiar la capa del objeto a Default para que el Raycast de interacción lo ignore
        int defaultLayer = LayerMask.NameToLayer("Default");
        if (defaultLayer != -1)
        {
            gameObject.layer = defaultLayer;
        }

        // 5. Apagar cartel global de texto si estuviese activo
        if (FloatingTextManager.Instance != null)
        {
            FloatingTextManager.Instance.Hide();
        }

        Debug.Log($"[Interactable] Puzzle en {gameObject.name} marcado como resuelto. Interacción bloqueada.");
    }

    // --- FLUJO DE INSTRUCCIONES Y CÁMARA ---
    public void OpenInstructions()
    {
        if (IsSolved || isTransitioning) return;
        if (GameManager.IsAnyPuzzleActive && !GameManager.IsThisPuzzleActive(this)) return;

        GameManager.RegisterActivePuzzle(this);

        isInstructionOpen = true;

        HidePrompt();

        if (instructionCanvas != null) instructionCanvas.SetActive(true);

        SetPlayerControls(false);
        UnlockCursor();

        StartCoroutine(EnableInputNextFrame());
    }

    private IEnumerator EnableInputNextFrame()
    {
        canProcessInput = false;
        yield return null;
        canProcessInput = true;
    }

    private void SetPlayerControls(bool state)
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = state;

        if (mouseLookScript != null)
            mouseLookScript.enabled = state;
    }

    public void OnStartButtonPressed()
    {
        if (IsSolved || isTransitioning) return;

        isInstructionOpen = false;

        // Ocultar Canvas de instrucciones de inmediato
        if (instructionCanvas != null)
            instructionCanvas.SetActive(false);

        // 1. Si el puzzle cuenta con cámara dedicada (TV, Escritorio), interpolar suavemente hacia ella
        if (puzzleCamera != null)
        {
            EnterPuzzleView(() =>
            {
                // Al finalizar la transición, se activa la lógica y la UI del puzzle
                StartAttachedPuzzle();
            });
        }
        else
        {
            // Puzzles jugados en primera persona (ej: Caja de Juguetes): sesión sin cámara propia.
            // Se marca la sesión para que Escape pueda salir limpiamente del reto.
            isInFirstPersonPuzzle = true;
            RestorePlayerState();
            StartAttachedPuzzle();
        }
    }

    public void CloseInstructions()
    {
        if (isTransitioning) return;

        isInstructionOpen = false;

        GameManager.UnregisterActivePuzzle(this);

        if (instructionCanvas != null) instructionCanvas.SetActive(false);

        RestorePlayerState();

        if (isPlayerLooking && !IsSolved)
        {
            ShowPrompt();
        }
    }

    /// <summary>
    /// Transiciona suavemente desde la cámara del jugador hacia la cámara del puzzle.
    /// Desactiva controles e inputs mientras la transición está en curso.
    /// </summary>
    public void EnterPuzzleView(Action onComplete = null)
    {
        if (isTransitioning) return;

        GameManager.RegisterActivePuzzle(this);
        SetPlayerControls(false);

        Camera activeCam = GetActivePlayerCamera();

        if (useSmoothCameraTransition && puzzleCamera != null && activeCam != null)
        {
            isTransitioning = true;
            CameraTransitionManager.Instance.Transition(activeCam, puzzleCamera, cameraTransitionDuration, () =>
            {
                isTransitioning = false;
                isInPuzzleView = true;
                UnlockCursor();
                onComplete?.Invoke();
            });
        }
        else
        {
            isInPuzzleView = true;
            if (activeCam != null) activeCam.gameObject.SetActive(false);
            if (puzzleCamera != null) puzzleCamera.gameObject.SetActive(true);
            UnlockCursor();
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Salida del puzzle: Oculta la UI del puzzle de inmediato y transiciona suavemente de regreso a la cámara del jugador.
    /// Reactiva controles y bloquea el cursor al completar.
    /// </summary>
    public void ExitPuzzle(Action onComplete = null)
    {
        if (isTransitioning) return;

        isInPuzzleView = false;
        isInFirstPersonPuzzle = false;
        GameManager.UnregisterActivePuzzle(this);

        // 1. Ocultar la UI del puzzle y detener controladores inmediatamente
        StopAttachedPuzzle();

        Camera activeCam = GetActivePlayerCamera();

        // 2. Si cuenta con cámara dedicada de puzzle, interpolar suavemente de regreso al jugador
        if (useSmoothCameraTransition && puzzleCamera != null && activeCam != null)
        {
            isTransitioning = true;
            CameraTransitionManager.Instance.Transition(puzzleCamera, activeCam, cameraTransitionDuration, () =>
            {
                isTransitioning = false;
                RestorePlayerState();

                if (isPlayerLooking && !IsSolved)
                {
                    ShowPrompt();
                }

                onComplete?.Invoke();
            });
        }
        else
        {
            if (puzzleCamera != null) puzzleCamera.gameObject.SetActive(false);
            if (activeCam != null) activeCam.gameObject.SetActive(true);

            RestorePlayerState();

            if (isPlayerLooking && !IsSolved)
            {
                ShowPrompt();
            }

            onComplete?.Invoke();
        }
    }

    private void StartAttachedPuzzle()
    {
        TVPuzzleController tvPuzzle = GetComponent<TVPuzzleController>();
        if (tvPuzzle != null)
        {
            tvPuzzle.StartPuzzle();
            return;
        }

        DeskPuzzleController deskPuzzle = GetComponent<DeskPuzzleController>();
        if (deskPuzzle != null)
        {
            deskPuzzle.StartPuzzle();
            return;
        }

        ToyBoxDropZone boxPuzzle = GetComponent<ToyBoxDropZone>();
        if (boxPuzzle != null)
        {
            boxPuzzle.StartPuzzle();
            return;
        }
    }

    private void StopAttachedPuzzle()
    {
        TVPuzzleController tvPuzzle = GetComponent<TVPuzzleController>();
        if (tvPuzzle != null && tvPuzzle.IsActive)
        {
            tvPuzzle.StopPuzzle();
        }

        DeskPuzzleController deskPuzzle = GetComponent<DeskPuzzleController>();
        if (deskPuzzle != null && deskPuzzle.IsActive)
        {
            deskPuzzle.StopPuzzle();
        }

        ToyBoxDropZone boxPuzzle = GetComponent<ToyBoxDropZone>();
        if (boxPuzzle != null && boxPuzzle.IsPuzzleActive)
        {
            boxPuzzle.StopPuzzle();
        }
    }

    private void ResolvePlayerCamera()
    {
        if (mainPlayerCamera == null)
        {
            mainPlayerCamera = Camera.main;
            if (mainPlayerCamera == null)
            {
                MouseLook mouseLook = FindFirstObjectByType<MouseLook>();
                if (mouseLook != null)
                {
                    mainPlayerCamera = mouseLook.GetComponent<Camera>();
                }
            }
        }
    }

    private Camera GetActivePlayerCamera()
    {
        if (mainPlayerCamera != null) return mainPlayerCamera;
        ResolvePlayerCamera();
        return mainPlayerCamera != null ? mainPlayerCamera : Camera.main;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestorePlayerState()
    {
        SetPlayerControls(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
