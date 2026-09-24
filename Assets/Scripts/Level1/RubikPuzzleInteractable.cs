using UnityEngine;

public class RubikPuzzleInteractable : MonoBehaviour, IInteractable
{
    [Header("UI Text Override")]
    [SerializeField] private string hoverMessage = "Resolver acertijo de colores";

    [Header("Cámaras")]
    [SerializeField] private Camera mainPlayerCamera;
    [SerializeField] private Camera rubikCamera;

    [Header("Scripts del Jugador a Desactivar")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour mouseLookScript;

    [Header("Referencia al Controlador del Acertijo")]
    [SerializeField] private RubikPuzzleController puzzleController;

    private bool isInPuzzleView = false;

    private void Start()
    {
        if (rubikCamera != null) rubikCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
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
    public void OnHandEnter() { }
    public void OnHandExit() { }

    public void Interact()
    {
        if (!isInPuzzleView) EnterPuzzle();
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetPlayerControls(bool state)
    {
        if (playerMovementScript != null) playerMovementScript.enabled = state;
        if (mouseLookScript != null) mouseLookScript.enabled = state;
    }
}