using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RubikPuzzleInteractable : MonoBehaviour, IInteractable
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

    [Header("World Space Canvas del Mensaje Flotante (Compatibilidad)")]
    [SerializeField] private GameObject promptCanvas; // Texto 3D "Presiona E"
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string hoverMessage = "Presiona E para ver instrucciones";

    [Header("Canvas de Instrucciones del Puzzle")]
    [SerializeField] private GameObject instructionCanvas; // El Canvas de UI con el botón "Empezar"

    [Header("Cámaras")]
    [SerializeField] private Camera mainPlayerCamera;
    [SerializeField] private Camera rubikCamera;

    [Header("Scripts del Jugador a Desactivar")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour mouseLookScript;

    [Header("Referencia al Controlador del Acertijo")]
    [SerializeField] private RubikPuzzleController puzzleController;

    public bool IsSolved { get; private set; } = false;

    private bool isInstructionOpen = false;
    private bool isInPuzzleView = false;
    private bool isPlayerLooking = false;

    private void Awake()
    {
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
        if (rubikCamera != null) rubikCamera.gameObject.SetActive(false);
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

            GameObject target = promptContainer != null ? promptContainer : promptCanvas;
            if (target != null && mainPlayerCamera != null)
            {
                target.transform.rotation = Quaternion.LookRotation(
                    target.transform.position - mainPlayerCamera.transform.position
                );
            }
        }

        // 2. Si el panel de instrucciones está abierto y se presiona E
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

        // 3. Si está en vista de puzzle
        if (isInPuzzleView)
        {
            if (Input.GetMouseButtonDown(0))
            {
                DetectBlockClick();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitPuzzle();
            }
        }
    }

    private void DetectBlockClick()
    {
        if (rubikCamera == null) return;

        Ray ray = rubikCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 20f);

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<RubikBlock>(out var block))
            {
                block.ClickBlock();
                break;
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
            Debug.LogWarning("[RubikPuzzleInteractable] Ya hay otro puzzle en progreso.");
            return;
        }

        if (!isInstructionOpen && !isInPuzzleView)
        {
            OpenInstructions();
        }
    }

    public void ShowPrompt()
    {
        if (IsSolved || isInstructionOpen || isInPuzzleView) return;
        if (GameManager.IsAnyPuzzleActive && !GameManager.IsThisPuzzleActive(this)) return;

        if (promptContainer != null)
            promptContainer.SetActive(true);

        if (keyIconImage != null)
            keyIconImage.gameObject.SetActive(true);

        if (actionDescriptionText != null)
            actionDescriptionText.text = !string.IsNullOrEmpty(actionDescription) ? actionDescription : hoverMessage;
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

        HidePrompt();

        Collider[] cols = GetComponents<Collider>();
        foreach (var c in cols)
        {
            if (c != null && c.isTrigger) c.enabled = false;
        }

        Collider[] childCols = GetComponentsInChildren<Collider>();
        foreach (var c in childCols)
        {
            if (c != null && c.isTrigger) c.enabled = false;
        }

        int defaultLayer = LayerMask.NameToLayer("Default");
        if (defaultLayer != -1)
        {
            gameObject.layer = defaultLayer;
        }

        if (FloatingTextManager.Instance != null)
        {
            FloatingTextManager.Instance.Hide();
        }

        Debug.Log($"[Interactable] Puzzle en {gameObject.name} marcado como resuelto. Interacción bloqueada.");
    }

    public void OpenInstructions()
    {
        if (IsSolved) return;
        if (GameManager.IsAnyPuzzleActive && !GameManager.IsThisPuzzleActive(this)) return;

        GameManager.RegisterActivePuzzle(this);

        isInstructionOpen = true;

        HidePrompt();

        if (instructionCanvas != null) instructionCanvas.SetActive(true);

        SetPlayerControls(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnStartButtonPressed()
    {
        if (IsSolved) return;

        isInstructionOpen = false;

        if (instructionCanvas != null)
            instructionCanvas.SetActive(false);

        EnterPuzzleView();
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
        if (rubikCamera != null) rubikCamera.gameObject.SetActive(true);

        SetPlayerControls(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (puzzleController != null)
            puzzleController.ActivatePuzzle();
    }

    public void ExitPuzzle()
    {
        isInPuzzleView = false;
        GameManager.UnregisterActivePuzzle(this);

        if (rubikCamera != null) rubikCamera.gameObject.SetActive(false);
        if (mainPlayerCamera != null) mainPlayerCamera.gameObject.SetActive(true);

        SetPlayerControls(true);

        if (puzzleController != null) puzzleController.DeactivatePuzzle();

        RestorePlayerState();

        if (isPlayerLooking && !IsSolved)
        {
            ShowPrompt();
        }
    }

    private void RestorePlayerState()
    {
        SetPlayerControls(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetPlayerControls(bool state)
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = state;

        if (mouseLookScript != null)
        {
            mouseLookScript.enabled = false;
            if (state) mouseLookScript.enabled = true;
        }
    }
}