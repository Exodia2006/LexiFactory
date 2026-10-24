using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Camera mainPlayerCamera;
    [SerializeField] private Camera puzzleCamera;

    [Header("Scripts del Jugador a Desactivar")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour mouseLookScript;

    public bool IsSolved { get; private set; } = false;

    private bool isInstructionOpen = false;
    private bool isInPuzzleView = false;
    private bool isPlayerLooking = false;
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

        if (mainPlayerCamera == null)
            mainPlayerCamera = Camera.main;
    }

    private void Start()
    {
        if (puzzleCamera != null) puzzleCamera.gameObject.SetActive(false);
        if (instructionCanvas != null) instructionCanvas.SetActive(false);

        HidePrompt();

        if (actionDescriptionText != null)
            actionDescriptionText.text = !string.IsNullOrEmpty(actionDescription) ? actionDescription : hoverMessage;
        else if (promptText != null)
            promptText.text = hoverMessage;
    }

    private void Update()
    {
        if (IsSolved) return;

        // 1. Orientar cartel flotante hacia la cámara
        if (isPlayerLooking && !isInstructionOpen && !isInPuzzleView)
        {
            if (mainPlayerCamera == null) mainPlayerCamera = Camera.main;

            GameObject targetToRotate = promptContainer != null ? promptContainer : promptCanvas;
            if (targetToRotate != null && mainPlayerCamera != null)
            {
                targetToRotate.transform.rotation = Quaternion.LookRotation(
                    targetToRotate.transform.position - mainPlayerCamera.transform.position
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
                CloseInstructions();
                return;
            }
        }

        // 3. Control mientras está metido en la cámara del reto (TV, Escritorio, etc.)
        if (isInPuzzleView)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitPuzzle();
                return;
            }
        }
    }

    // --- INTERFAZ IINTERACTABLE ---
    public string GetHoverText() => IsSolved ? string.Empty : (!string.IsNullOrEmpty(actionDescription) ? actionDescription : hoverMessage);

    public void OnHandEnter()
    {
        if (IsSolved || isInstructionOpen || isInPuzzleView) return;
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
        if (IsSolved) return;
        if (GameManager.IsAnyPuzzleActive && !GameManager.IsThisPuzzleActive(this))
        {
            Debug.LogWarning("[GenericPuzzleInteractable] Ya hay otro puzzle en progreso. Termina o sal del actual primero.");
            return;
        }

        if (!isInstructionOpen && !isInPuzzleView)
        {
            OpenInstructions();
        }
    }

    // --- GESTIÓN DE PROMPTS Y BLOQUEO ---
    public void ShowPrompt()
    {
        if (IsSolved || isInstructionOpen || isInPuzzleView) return;
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
        if (IsSolved) return;
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
        if (IsSolved) return;

        isInstructionOpen = false;

        if (instructionCanvas != null)
            instructionCanvas.SetActive(false);

        // 1. Si el objeto tiene cámara propia de reto (como el TV o Escritorio), enfocamos
        if (puzzleCamera != null)
        {
            EnterPuzzleView();
        }
        else
        {
            // Puzzles que se juegan caminando en primera persona (ej: Caja de Juguetes)
            RestorePlayerState();
        }

        // 2. Iniciar la lógica del controlador correspondiente
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

    public void CloseInstructions()
    {
        isInstructionOpen = false;

        GameManager.UnregisterActivePuzzle(this);

        if (instructionCanvas != null) instructionCanvas.SetActive(false);

        RestorePlayerState();

        if (isPlayerLooking && !IsSolved)
        {
            ShowPrompt();
        }
    }

    public void EnterPuzzleView()
    {
        GameManager.RegisterActivePuzzle(this);
        isInPuzzleView = true;

        if (mainPlayerCamera != null) mainPlayerCamera.gameObject.SetActive(false);
        if (puzzleCamera != null) puzzleCamera.gameObject.SetActive(true);

        SetPlayerControls(false);
        UnlockCursor();
    }

    public void ExitPuzzle()
    {
        isInPuzzleView = false;
        GameManager.UnregisterActivePuzzle(this);

        if (puzzleCamera != null) puzzleCamera.gameObject.SetActive(false);
        if (mainPlayerCamera != null) mainPlayerCamera.gameObject.SetActive(true);

        // Notificar si se cancela con Escape
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

        RestorePlayerState();

        if (isPlayerLooking && !IsSolved)
        {
            ShowPrompt();
        }
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