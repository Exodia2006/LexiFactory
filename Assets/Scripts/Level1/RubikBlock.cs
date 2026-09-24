using UnityEngine;

public class RubikBlock : MonoBehaviour, IInteractable
{
    [SerializeField] private string colorName;
    [SerializeField] private RubikPuzzleController puzzleController;

    private void OnMouseDown()
    {
        // Solo permitir clics si el acertijo está activo
        if (puzzleController != null && puzzleController.IsActive)
        {
            Debug.Log($"[RubikBlock] Clic en {gameObject.name} ({colorName})");
            puzzleController.OnBlockClicked(colorName);
        }
        else
        {
            Debug.Log("[RubikBlock] Clic ignorado: El acertijo no está activo.");
        }
    }

    public void ClickBlock()
    {
        Debug.Log($"[RubikBlock] Activado bloque: {gameObject.name} ({colorName})");

        if (puzzleController != null)
        {
            puzzleController.OnBlockClicked(colorName);
        }
    }

    public void Interact()
    {
        // Si interactúas directamente
        OnMouseDown();
    }

    public string GetHoverText() => puzzleController != null && puzzleController.IsActive ? $"Presionar {colorName}" : "";
    public void OnHandEnter() { }
    public void OnHandExit() { }
}