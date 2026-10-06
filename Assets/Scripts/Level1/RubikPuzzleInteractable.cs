using UnityEngine;
using TMPro;

public class RubikPuzzleInteractable : MonoBehaviour, IInteractable
{
    [Header("UI Text Override")]
    [SerializeField] private string hoverMessage = "Presiona E para ver instrucciones";

    [Header("World Space Canvas del Mensaje Flotante")]
    [SerializeField] private GameObject promptCanvas; // Texto 3D "Presiona E"
    [SerializeField] private TextMeshProUGUI promptText;

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

    private bool isInstructionOpen = false;
    private bool isInPuzzleView = false;
    private bool isPlayerLooking = false;

    private void Start()
    {
        if (rubikCamera != null) rubikCamera.gameObject.SetActive(false);
        if (promptCanvas != null) promptCanvas.SetActive(false);
        if (instructionCanvas != null) instructionCanvas.SetActive(false);

        if (promptText != null) promptText.text = hoverMessage;
    }

    private void Update()
    {
        // 1. Orientar el texto flotante "Presiona E" hacia la cámara mientras el jugador mira el objeto
        if (isPlayerLooking && !isInstructionOpen && !isInPuzzleView && promptCanvas != null && mainPlayerCamera != null)
        {
            promptCanvas.transform.rotation = Quaternion.LookRotation(
                promptCanvas.transform.position - mainPlayerCamera.transform.position
            );
        }

        // 2. Si el panel de instrucciones está abierto y se presiona E
        if (isInstructionOpen)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                OnStartButtonPressed(); // Mismo método que usa el botón
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseInstructions();
                return;
            }
        }

        // 3. Si está metido en la cámara del puzzle
        if (isInPuzzleView)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            {
                ExitPuzzle();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                DetectBlockClick();
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
    public string GetHoverText() => hoverMessage;

    public void OnHandEnter()
    {
        if (!isInstructionOpen && !isInPuzzleView)
        {
            isPlayerLooking = true;
            if (promptCanvas != null) promptCanvas.SetActive(true);
        }
    }

    public void OnHandExit()
    {
        isPlayerLooking = false;
        if (promptCanvas != null) promptCanvas.SetActive(false);
    }

    public void Interact()
    {
        if (!isInstructionOpen && !isInPuzzleView)
        {
            OpenInstructions();
        }
    }

    public void OpenInstructions()
    {
        isInstructionOpen = true;

        if (promptCanvas != null) promptCanvas.SetActive(false);
        if (instructionCanvas != null) instructionCanvas.SetActive(true);

        SetPlayerControls(false);

        // Liberar cursor para hacer clic en el botón de UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // MÉTODO PUBLICO PARA ASIGNAR EN EL ONCLICK() DEL BOTÓN "EMPEZAR"
    public void OnStartButtonPressed()
    {
        isInstructionOpen = false;

        if (instructionCanvas != null)
            instructionCanvas.SetActive(false);

        EnterPuzzle();
    }

    // MÉTODO PUBLICO PARA ASIGNAR EN EL ONCLICK() DEL BOTÓN "CERRAR / X"
    public void CloseInstructions()
    {
        isInstructionOpen = false;

        if (instructionCanvas != null)
            instructionCanvas.SetActive(false);

        RestorePlayerState();

        if (isPlayerLooking && promptCanvas != null)
        {
            promptCanvas.SetActive(true);
        }
    }

    private void EnterPuzzle()
    {
        isInPuzzleView = true;

        if (mainPlayerCamera != null) mainPlayerCamera.gameObject.SetActive(false);
        if (rubikCamera != null) rubikCamera.gameObject.SetActive(true);

        SetPlayerControls(false);

        if (puzzleController != null) puzzleController.ActivatePuzzle();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitPuzzle()
    {
        isInPuzzleView = false;

        if (rubikCamera != null) rubikCamera.gameObject.SetActive(false);
        if (mainPlayerCamera != null) mainPlayerCamera.gameObject.SetActive(true);

        SetPlayerControls(true);

        if (puzzleController != null) puzzleController.DeactivatePuzzle();

        RestorePlayerState();

        if (isPlayerLooking && promptCanvas != null)
        {
            promptCanvas.SetActive(true);
        }
    }

    // Restablece el cursor y recalibra la cámara del jugador
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
            // Forzar reinicio del script para que Unity detecte el bloqueo del mouse
            mouseLookScript.enabled = false;
            if (state) mouseLookScript.enabled = true;
        }
    }
}