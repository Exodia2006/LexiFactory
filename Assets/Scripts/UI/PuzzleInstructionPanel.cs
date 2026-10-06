using UnityEngine;
using UnityEngine.UI;

public class PuzzleInstructionPanel : MonoBehaviour, IInteractable
{
    [Header("UI Canvas de Instrucciones")]
    [SerializeField] private GameObject instructionCanvas; // El Canvas/Panel que te entregaron para este puzzle
    [SerializeField] private string hoverMessage = "Presiona E para ver instrucciones";

    [Header("Scripts del Jugador a Desactivar")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour mouseLookScript;

    [Header("Opciones de Control")]
    [SerializeField] private bool autoCloseWithE = true;

    private bool isPanelOpen = false;

    private void Start()
    {
        if (instructionCanvas != null)
        {
            instructionCanvas.SetActive(false); // Inicia oculto
        }
    }

    private void Update()
    {
        if (!isPanelOpen) return;

        // Permite cerrar el Canvas con la tecla E o Escape
        if (autoCloseWithE && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)))
        {
            CloseInstructionPanel();
        }
    }

    // --- INTERFAZ IINTERACTABLE ---
    public string GetHoverText() => hoverMessage;
    public void OnHandEnter() { }
    public void OnHandExit() { }

    public void Interact()
    {
        if (!isPanelOpen)
        {
            OpenInstructionPanel();
        }
    }

    public void OpenInstructionPanel()
    {
        isPanelOpen = true;

        if (instructionCanvas != null)
        {
            instructionCanvas.SetActive(true);
        }

        // Desactivar controles del jugador para poder mover el cursor
        SetPlayerControls(false);

        // Liberar y mostrar el cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseInstructionPanel()
    {
        isPanelOpen = false;

        if (instructionCanvas != null)
        {
            instructionCanvas.SetActive(false);
        }

        // Reactivar controles del jugador
        SetPlayerControls(true);

        // Bloquear y ocultar el cursor nuevamente
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetPlayerControls(bool state)
    {
        if (playerMovementScript != null) playerMovementScript.enabled = state;
        if (mouseLookScript != null) mouseLookScript.enabled = state;
    }
}