using UnityEngine;

public class Panel_controler_historia : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el panel de UI que deseas mostrar/ocultar.")]
    [SerializeField] private GameObject panel;

    [Tooltip("Arrastra el script de movimiento de tu personaje.")]
    [SerializeField] private MonoBehaviour playerMovementScript;

    [Tooltip("Arrastra el script que controla la rotación de la cámara (ej. MouseLook o el script de la cámara en primera persona).")]
    [SerializeField] private MonoBehaviour cameraLookScript;

    private void Start()
    {
        // Al iniciar el juego, si el panel está activo, mostramos el cursor y detenemos todo.
        if (panel != null && panel.activeSelf)
        {
            SetPanelState(true);
        }
        else
        {
            SetPanelState(false);
        }
    }

    /// <summary>
    /// Alterna el estado del panel (Abrir/Cerrar). Asignar al botón OnClick().
    /// </summary>
    public void TogglePanel()
    {
        if (panel == null) return;

        bool newState = !panel.activeSelf;
        SetPanelState(newState);
    }

    /// <summary>
    /// Establece el estado del panel, movimiento, cámara y cursor.
    /// </summary>
    public void SetPanelState(bool isOpen)
    {
        if (panel != null)
        {
            panel.SetActive(isOpen);
        }

        // Desactivar o activar el movimiento del jugador
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = !isOpen;
        }

        // Desactivar o activar la rotación de la cámara en primera persona
        if (cameraLookScript != null)
        {
            cameraLookScript.enabled = !isOpen;
        }

        // Control estricto del cursor
        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

    }

}


