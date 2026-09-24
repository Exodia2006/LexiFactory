using System.Collections;
using UnityEngine;

public class RubikBlock : MonoBehaviour, IInteractable
{
    [SerializeField] private string colorName;
    [SerializeField] private RubikPuzzleController puzzleController;

    // Posiciones en el eje Z
    private float defaultZ = -1.724542f;
    private float pressedZ = -1.749f;
    private float moveSpeed = 10f; // Velocidad del movimiento

    private bool isPressed = false;
    private Coroutine moveCoroutine;

    public void ClickBlock()
    {
        if (isPressed) return; // Evitar presionar dos veces el mismo botón mientras está hundido

        isPressed = true;

        // Animar el movimiento hacia la posición presionada en Z
        StartBlockMove(pressedZ);

        if (puzzleController != null)
        {
            puzzleController.OnBlockClicked(colorName);
        }
    }

    // Método para devolver el botón a su posición inicial
    public void ResetPosition()
    {
        isPressed = false;
        StartBlockMove(defaultZ);
    }

    private void StartBlockMove(float targetZ)
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(AnimateMove(targetZ));
    }

    private IEnumerator AnimateMove(float targetZ)
    {
        Vector3 currentPos = transform.localPosition;
        Vector3 targetPos = new Vector3(currentPos.x, currentPos.y, targetZ);

        while (Vector3.Distance(transform.localPosition, targetPos) > 0.001f)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.localPosition = targetPos;
    }

    // --- MÉTODOS DE LA INTERFAZ IINTERACTABLE ---
    public void Interact() => ClickBlock();
    public string GetHoverText() => $"Presionar {colorName}";
    public void OnHandEnter() { }
    public void OnHandExit() { }
}