using UnityEngine;

public class ToyBlock : MonoBehaviour
{
    [Header("Identificador del Bloque")]
    [SerializeField] private string blockID = "RedBlock"; // Nombre o color del bloque

    [Header("Referencia al Controlador")]
    [SerializeField] private ToyBoxPuzzleController puzzleController;

    [Header("Efecto Visual (Opcional)")]
    [SerializeField] private Vector3 pressedOffset = new Vector3(0, -0.05f, 0);
    private Vector3 originalPosition;

    private void Awake()
    {
        originalPosition = transform.localPosition;
    }

    public void OnClickBlock()
    {
        // Pequeño feedback visual de pulsación
        transform.localPosition = originalPosition + pressedOffset;
        Invoke(nameof(ResetPosition), 0.15f);

        if (puzzleController != null)
        {
            puzzleController.OnBlockClicked(blockID);
        }
    }

    public void ResetPosition()
    {
        transform.localPosition = originalPosition;
    }
}