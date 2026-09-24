using UnityEngine;
using TMPro;

public class RubikPuzzleInteractable : MonoBehaviour, IInteractable
{
    [Header("UI Text Override")]
    [SerializeField] private string hoverMessage = "Presiona E para interactuar";

    [Header("World Space Canvas del Mensaje")]
    [SerializeField] private GameObject promptCanvas; // Duplicado del Canvas con el texto flotante
    [SerializeField] private TextMeshProUGUI promptText; // Texto dentro del Canvas (opcional si quieres cambiar el mensaje)

    [Header("Cámaras")]
    [SerializeField] private Camera mainPlayerCamera;
    [SerializeField] private Camera rubikCamera;

    [Header("Scripts del Jugador a Desactivar")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour mouseLookScript;

    [Header("Referencia al Controlador del Acertijo")]
    [SerializeField] private RubikPuzzleController puzzleController;

    private bool isInPuzzleView = false;
    private bool isPlayerLooking = false;

    private void Start()
    {
        if (rubikCamera != null) rubikCamera.gameObject.SetActive(false);
        if (promptCanvas != null) promptCanvas.SetActive(false); // Iniciar oculto

        if (promptText != null) promptText.text = hoverMessage;
    }

    private void Update()
    {
        // 1. Si el jugador está mirando el objeto (y no está metido en el puzzle), orientar el Canvas a la cámara
        if (isPlayerLooking && !isInPuzzleView && promptCanvas != null && mainPlayerCamera != null)
        {
            // Apuntar el Canvas hacia la cámara principal para que siempre sea legible
            promptCanvas.transform.rotation = Quaternion.LookRotation(promptCanvas.transform.position - mainPlayerCamera.transform.position);
        }

        if (!isInPuzzleView) return;

        // Salir con Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitPuzzle();
            return;
        }

        // Clic izquierdo para interactuar con los botones del Rubik
        if (Input.GetMouseButtonDown(0))
        {
            DetectBlockClick();
        }
    }

    private void DetectBlockClick()
    {
        if (rubikCamera == null) return;

        Ray ray = rubikCamera.ScreenPointToRay(Input.mousePosition);

        // RaycastAll para atravesar el collider de la estructura/máquina
        RaycastHit[] hits = Physics.RaycastAll(ray, 20f);

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<RubikBlock>(out var block))
            {
                block.ClickBlock(); // Ejecuta la lógica del bloque
                break; // Detenerse al encontrar el primer bloque válido
            }
        }
    }

    // --- INTERFAZ IINTERACTABLE ---
    public string GetHoverText() => hoverMessage;

    public void OnHandEnter()
    {
        // Se activa cuando la mano / raycast del jugador mira al objeto
        if (!isInPuzzleView)
        {
            isPlayerLooking = true;
            if (promptCanvas != null) promptCanvas.SetActive(true);
        }
    }

    public void OnHandExit()
    {
        // Se desactiva cuando el jugador se aleja o deja de mirar el objeto
        isPlayerLooking = false;
        if (promptCanvas != null) promptCanvas.SetActive(false);
    }

    public void Interact()
    {
        if (!isInPuzzleView) EnterPuzzle();
    }

    private void EnterPuzzle()
    {
        isInPuzzleView = true;

        // Ocultar el mensaje flotante al entrar a la vista del puzzle
        if (promptCanvas != null) promptCanvas.SetActive(false);

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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Si al salir del puzzle la mano sigue apuntando al objeto, reactivar el mensaje
        if (isPlayerLooking && promptCanvas != null)
        {
            promptCanvas.SetActive(true);
        }
    }

    private void SetPlayerControls(bool state)
    {
        if (playerMovementScript != null) playerMovementScript.enabled = state;
        if (mouseLookScript != null) mouseLookScript.enabled = state;
    }
}