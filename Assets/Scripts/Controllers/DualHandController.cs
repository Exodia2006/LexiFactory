using UnityEngine;

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
        if (Mathf.Approximately(Time.timeScale, 0f))
            return;

        transform.localPosition = containerOffset;

        UpdateHandSeparation();
    }

    private void UpdateHandSeparation()
    {
        if (leftHand != null)
        {
            leftHand.localPosition = new Vector3(
                -handSeparation * 0.5f,
                0f,
                0f
            );
        }

        if (rightHand != null)
        {
            rightHand.localPosition = new Vector3(
                handSeparation * 0.5f,
                0f,
                0f
            );
        }
    }
}