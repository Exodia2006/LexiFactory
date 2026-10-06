using UnityEngine;
using TMPro;

public class GenericPuzzleInteractable : MonoBehaviour, IInteractable
{
    [Header("UI Text Override")]
    [SerializeField] private string hoverMessage = "Presiona E para ver instrucciones";

    [Header("World Space Canvas del Mensaje Flotante")]
    [SerializeField] private GameObject promptCanvas; // Texto 3D "Presiona E"
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Canvas de Instrucciones del Puzzle")]
    [SerializeField] private GameObject instructionCanvas; // Canvas del reto entregado por diseño

    [Header("Configuración del Código")]
    [SerializeField] private int codeDigitIndex = 2; // Index: 1 para Puzzle 2, 2 para Puzzle 3, 3 para Puzzle 4

    [Header("Cámaras (Si requiere cámara dedicada)")]
    [SerializeField] private Camera mainPlayerCamera;
    [SerializeField] private Camera puzzleCamera;

    [Header("Scripts del Jugador a Desactivar")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour mouseLookScript;

    private bool isInstructionOpen = false;
    private bool isInPuzzleView = false;
    private bool isPlayerLooking = false;
    private bool isSolved = false;

    private void Start()
    {
        if (puzzleCamera != null) puzzleCamera.gameObject.SetActive(false);
        if (promptCanvas != null) promptCanvas.SetActive(false);
        if (instructionCanvas != null) instructionCanvas.SetActive(false);

        if (promptText != null) promptText.text = hoverMessage;
    }

    private void Update()
    {
        // 1. Orientar texto 3D "Presiona E" hacia la cámara
        if (isPlayerLooking && !isInstructionOpen && !isInPuzzleView && promptCanvas != null && mainPlayerCamera != null)
        {
            promptCanvas.transform.rotation = Quaternion.LookRotation(
                promptCanvas.transform.position - mainPlayerCamera.transform.position
            );
        }

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
        if (instructionCanvas != null) instructionCanvas.SetActive(true);

        // Desactivar controles del jugador primero
        SetPlayerControls(false);

        // Liberar y mostrar el cursor de forma explícita
        UnlockCursor();
    }

    // ASIGNAR EN EL BOTÓN "EMPEZAR" DE CADA CANVAS
    public void OnStartButtonPressed()
    {
        isInstructionOpen = false;

        if (instructionCanvas != null) instructionCanvas.SetActive(false);

        // Si el reto usa cámara dedicada, la cambiamos; si no, devolvemos el control libre
        if (puzzleCamera != null)
        {
            EnterPuzzleView();
        }
        else
        {
            // Para retos en 3D libre (como buscar juguetes en la sala), se devuelven los controles
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

    // LLAMAR ESTE MÉTODO CUANDO EL JUGADOR RESUELVA EL RETO
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