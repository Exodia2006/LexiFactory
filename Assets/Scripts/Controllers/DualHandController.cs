using UnityEngine;

public class DualHandController : MonoBehaviour
{
    [Header("Hand References")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Positioning")]
    [SerializeField] private Vector3 containerOffset = new Vector3(0f, -0.15f, 0.25f);
    [SerializeField] private float handSeparation = 0.35f;

    [Header("Floor Protection")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Aumenta este valor si las manos siguen tocando el piso.")]
    [SerializeField] private float safetyRadius = 0.3f; // Distancia de seguridad desde la cámara al piso

    private Camera mainCam;
    private Vector3 lastHitPoint;
    private bool isCollidingWithFloor;

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        if (Mathf.Approximately(Time.timeScale, 0f)) return;

        Vector3 targetLocalPos = containerOffset;

        // Proteger contra el piso usando una esfera desde la cámara
        targetLocalPos = PreventFloorOverlap(targetLocalPos);

        transform.localPosition = targetLocalPos;

        UpdateHandSeparation();
    }

    private Vector3 PreventFloorOverlap(Vector3 desiredLocalPos)
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return desiredLocalPos;

        // Posición global a la que las manos intentan ir
        Vector3 targetWorldPos = mainCam.transform.TransformPoint(desiredLocalPos);
        Vector3 origin = mainCam.transform.position;
        Vector3 direction = (targetWorldPos - origin).normalized;
        float maxDistance = Vector3.Distance(origin, targetWorldPos) + safetyRadius;

        // Lanzamos una esfera de detección en lugar de una línea delgada
        if (Physics.SphereCast(origin, 0.1f, direction, out RaycastHit hit, maxDistance, groundLayer))
        {
            isCollidingWithFloor = true;
            lastHitPoint = hit.point;

            // Calculamos cuánto se debe retraer Y localmente
            float distanceCorrection = maxDistance - hit.distance;
            desiredLocalPos.y += distanceCorrection;
            desiredLocalPos.z -= distanceCorrection * 0.5f; // También las retrae un poco hacia el pecho
        }
        else
        {
            isCollidingWithFloor = false;
        }

        return desiredLocalPos;
    }

    private void UpdateHandSeparation()
    {
        if (leftHand != null)
            leftHand.localPosition = new Vector3(-handSeparation * 0.5f, 0f, 0f);

        if (rightHand != null)
            rightHand.localPosition = new Vector3(handSeparation * 0.5f, 0f, 0f);
    }

    // Dibuja una esfera en la vista Scene para depurar la colisión
    private void OnDrawGizmos()
    {
        if (mainCam == null) return;

        Gizmos.color = isCollidingWithFloor ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
}