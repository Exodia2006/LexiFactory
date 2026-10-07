using System.Collections;
using TMPro;
using UnityEngine;

public class GenericPuzzleInteractable : MonoBehaviour, IInteractable
{
    [Header("UI Text Override")]
    [SerializeField] private string hoverMessage = "Presiona E para ver instrucciones";

    [Header("World Space Canvas del Mensaje Flotante")]
    [SerializeField] private GameObject promptCanvas; // Texto 3D "Presiona E"
    [SerializeField] private TextMeshProUGUI promptText;

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

    private bool isInstructionOpen = false;
    private bool isInPuzzleView = false;
    private bool isPlayerLooking = false;
    private bool isSolved = false;
    private bool canProcessInput = true; // Control para ignorar la 'E' del mismo frame

    private void Start()
    {
        if (puzzleCamera != null) puzzleCamera.gameObject.SetActive(false);
        if (promptCanvas != null) promptCanvas.SetActive(false);
        if (instructionCanvas != null) instructionCanvas.SetActive(false);

        if (promptText != null) promptText.text = hoverMessage;
    }

    private void Update()
    {
        // 1. Orientar texto 3D hacia la cámara
        if (isPlayerLooking && !isInstructionOpen && !isInPuzzleView && promptCanvas != null && mainPlayerCamera != null)
        {
            promptCanvas.transform.rotation = Quaternion.LookRotation(
                promptCanvas.transform.position - mainPlayerCamera.transform.position
            );
        }

        // Si acabamos de abrir el panel en este fotograma, ignoramos el teclado hasta el siguiente frame
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

        // 3. Control mientras está metido en la cámara del reto
        if (isInPuzzleView)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            {
                ExitPuzzle();
                return;
            }

            // Detectar clic izquierdo en los juguetes
            if (Input.GetMouseButtonDown(0))
            {
                DetectToyClick();
            }
        }
    }

    // --- INTERFAZ IINTERACTABLE ---
    public string GetHoverText() => hoverMessage;

    public void OnHandEnter()
    {
        if (!isInstructionOpen && !isInPuzzleView && !isSolved)
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
        if (!isInstructionOpen && !isInPuzzleView && !isSolved)
        {
            OpenInstructions();
        }
    }

    public void OpenInstructions()
    {
        isInstructionOpen = true;

        if (promptCanvas != null) promptCanvas.SetActive(false);

        if (instructionCanvas != null)
        {
            instructionCanvas.SetActive(true);
        }

        SetPlayerControls(false);
        UnlockCursor();

        // Evita que el 'Update()' lea la tecla 'E' en este mismo fotograma
        StartCoroutine(EnableInputNextFrame());
    }

    private IEnumerator EnableInputNextFrame()
    {
        canProcessInput = false;
        yield return null; // Esperar al siguiente fotograma
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
        isInstructionOpen = false;

        if (instructionCanvas != null)
            instructionCanvas.SetActive(false);

        if (puzzleCamera != null)
        {
            EnterPuzzleView();

            IPuzzleController controller = GetComponent<IPuzzleController>();
            if (controller != null)
            {
                controller.ActivatePuzzle();
            }
        }
        else
        {
            RestorePlayerState();
        }
    }

    public void CloseInstructions()
    {
        isInstructionOpen = false;

        if (instructionCanvas != null) instructionCanvas.SetActive(false);

        RestorePlayerState();

        if (isPlayerLooking && promptCanvas != null)
        {
            promptCanvas.SetActive(true);
        }
    }

    private void EnterPuzzleView()
    {
        isInPuzzleView = true;

        if (mainPlayerCamera != null) mainPlayerCamera.gameObject.SetActive(false);
        if (puzzleCamera != null) puzzleCamera.gameObject.SetActive(true);

        SetPlayerControls(false);
        UnlockCursor();
    }

    public void ExitPuzzle()
    {
        isInPuzzleView = false;

        if (puzzleCamera != null) puzzleCamera.gameObject.SetActive(false);
        if (mainPlayerCamera != null) mainPlayerCamera.gameObject.SetActive(true);

        RestorePlayerState();

        if (isPlayerLooking && promptCanvas != null)
        {
            promptCanvas.SetActive(true);
        }
    }

    public void CompletePuzzle()
    {
        if (isSolved) return;

        isSolved = true;

        if (ScreenFlash.Instance != null)
            ScreenFlash.Instance.FlashSuccess();

        if (GameManager.Instance != null)
            GameManager.Instance.RevealDigit(codeDigitIndex);

        if (isInPuzzleView)
            ExitPuzzle();
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

    private void DetectToyClick()
    {
        if (puzzleCamera == null) return;

        Ray ray = puzzleCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 20f);

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<ToyBlock>(out var block))
            {
                block.OnClickBlock();
                break;
            }
        }
    }
}