using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    [SerializeField] private float mouseSensitivity = 200f;
    [SerializeField] private Transform playerBody; // Arrastra aquí el GameObject 'Player'

    private float xRotation = 0f;

    private void Start()
    {
        // Ocultar y bloquear el puntero en el centro
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Mathf.Approximately(Time.timeScale, 0f)) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Arriba / Abajo (Cámara)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -55f, 70f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Izquierda / Derecha (Cuerpo del Jugador)
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
        else
        {
            // Si no asignaste playerBody, rota la cámara directamente a los lados
            transform.parent.Rotate(Vector3.up * mouseX);
        }
    }
}