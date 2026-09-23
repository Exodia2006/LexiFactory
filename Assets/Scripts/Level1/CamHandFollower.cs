using UnityEngine;

public class CamHandFollower : MonoBehaviour
{
    [Header("Prueba de Empuje en Z")]
    [SerializeField] private float pushZFactor = 0.5f; // Prueba poner 0.5 o 1.0

    private Transform cameraTransform;
    private Vector3 initialLocalPos;

    private void Start()
    {
        initialLocalPos = transform.localPosition;

        if (transform.parent != null)
            cameraTransform = transform.parent;
        else
            cameraTransform = Camera.main != null ? Camera.main.transform : null;

        Debug.Log($"[HANDS TEST] Posición Inicial Guardada: {initialLocalPos} | Padre: {(cameraTransform != null ? cameraTransform.name : "SIN PADRE")}");
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Calculamos la inclinación hacia abajo
        float pitch = cameraTransform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        float lookDownFactor = Mathf.Clamp01(pitch / 80f);

        // FORZADO DIRECTO: Sin Lerp ni multiplicadores raros
        Vector3 newPos = initialLocalPos;
        newPos.z += lookDownFactor * pushZFactor;
        newPos.y -= lookDownFactor * 0.1f;

        transform.localPosition = newPos;
    }
}