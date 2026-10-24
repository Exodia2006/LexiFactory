using System.Collections;
using UnityEngine;

public enum KeypadButtonType
{
    Digit,
    Clear,
    ClearAll,
    Submit,
    Close
}

public class DoorKeypadButton : MonoBehaviour
{
    [Header("Configuración del Botón 3D")]
    [SerializeField] private KeypadButtonType buttonType = KeypadButtonType.Digit;
    [SerializeField] private int digitValue = 0;
    [SerializeField] private DoorKeypadController keypadController;

    [Header("Feedback Visual (Opcional)")]
    [Tooltip("Animación ligera al pulsar el botón (movimiento hacia adentro)")]
    [SerializeField] private bool animatePress = true;
    [SerializeField] private float pressDepth = 0.01f;
    [SerializeField] private float pressDuration = 0.1f;

    private Vector3 originalLocalPos;
    private bool isPressing = false;

    private void Awake()
    {
        originalLocalPos = transform.localPosition;

        if (keypadController == null)
            keypadController = GetComponentInParent<DoorKeypadController>();
        if (keypadController == null)
            keypadController = FindFirstObjectByType<DoorKeypadController>();
    }

    public void OnClick()
    {
        if (animatePress && !isPressing && gameObject.activeInHierarchy)
        {
            StartCoroutine(PressAnimationRoutine());
        }

        if (keypadController == null) return;

        switch (buttonType)
        {
            case KeypadButtonType.Digit:
                keypadController.PressDigitButton(digitValue.ToString());
                break;
            case KeypadButtonType.Clear:
                keypadController.PressClearButton();
                break;
            case KeypadButtonType.ClearAll:
                keypadController.PressClearAllButton();
                break;
            case KeypadButtonType.Submit:
                keypadController.PressSubmitButton();
                break;
            case KeypadButtonType.Close:
                keypadController.PressCloseButton();
                break;
        }
    }

    private IEnumerator PressAnimationRoutine()
    {
        isPressing = true;
        Vector3 pressedPos = originalLocalPos - transform.forward * pressDepth;
        transform.localPosition = pressedPos;

        yield return new WaitForSeconds(pressDuration);

        transform.localPosition = originalLocalPos;
        isPressing = false;
    }
}
