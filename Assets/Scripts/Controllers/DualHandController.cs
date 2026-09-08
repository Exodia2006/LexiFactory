using UnityEngine;

[ExecuteAlways] // Forzar a Unity a actualizar en tiempo real fuera de Play Mode
public class DualHandController : MonoBehaviour
{
    [Header("Hand References")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Positioning")]
    [SerializeField] private Vector3 containerOffset = new Vector3(0f, -0.15f, 0.25f);
    [SerializeField] private float handSeparation = 0.35f;

    private void Update()
    {
        // Forzar posición local fija sin Lerp ni condicionales
        transform.localPosition = containerOffset;

        if (leftHand != null)
            leftHand.localPosition = new Vector3(-handSeparation * 0.5f, 0f, 0f);

        if (rightHand != null)
            rightHand.localPosition = new Vector3(handSeparation * 0.5f, 0f, 0f);
    }
}