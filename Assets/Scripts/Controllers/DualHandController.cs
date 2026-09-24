using UnityEngine;

public class DualHandController : MonoBehaviour
{
    [Header("Hand References")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Base Positioning")]
    [SerializeField] private Vector3 containerOffset = new Vector3(0f, -0.15f, 0.25f);
    [SerializeField] private float handSeparation = 0.35f;

    [Header("Look Down Compensation")]
    [Tooltip("Ajuste de Y para bajar las manos al mirar al suelo.")]
    [SerializeField] private float dropYOnLookDown = 0.08f;
    [Tooltip("Ajuste de Z para evitar que se alejen de la pantalla al mirar abajo.")]
    [SerializeField] private float pushZOnLookDown = 0.05f;
    [Tooltip("Inclinación de compensación en X (grados) al mirar hacia abajo.")]
    [SerializeField] private float pitchCompensation = 15f;

    [SerializeField] private float smoothSpeed = 12f;

    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (Mathf.Approximately(Time.timeScale, 0f)) return;

        if (mainCam == null) mainCam = Camera.main;

        // 1. Calculamos el factor de inclinación hacia abajo (0 al frente, 1 abajo)
        float lookDownFactor = 0f;
        if (mainCam != null)
        {
            lookDownFactor = Mathf.Clamp01(Vector3.Dot(mainCam.transform.forward, Vector3.down));
        }

        // 2. Ajustamos la posición para mantener las manos pegadas a la lente
        Vector3 targetLocalPos = containerOffset;
        targetLocalPos.y -= lookDownFactor * dropYOnLookDown;
        targetLocalPos.z += lookDownFactor * pushZOnLookDown; // Empuja en Z para contrarrestar la separación

        // 3. Ajustamos la rotación local para que las manos se inclinen suavemente hacia arriba
        Quaternion targetLocalRot = Quaternion.Euler(-lookDownFactor * pitchCompensation, 0f, 0f);

        // 4. Aplicamos los cambios con interpolación suave
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * smoothSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocalRot, Time.deltaTime * smoothSpeed);

        UpdateHandSeparation();
    }

    private void UpdateHandSeparation()
    {
        if (leftHand != null)
            leftHand.localPosition = new Vector3(-handSeparation * 0.5f, 0f, 0f);

        if (rightHand != null)
            rightHand.localPosition = new Vector3(handSeparation * 0.5f, 0f, 0f);
    }

    private void OnValidate()
    {
        UpdateHandSeparation();
    }
}