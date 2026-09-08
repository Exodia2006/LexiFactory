using UnityEngine;

public class InteractableObject : MonoBehaviour, IInteractable
{
    [SerializeField] private string hoverMessage = "[E] Inspeccionar borrador";
    [SerializeField] private string objectName = "Borrador de tablero";

    public string GetHoverText()
    {
        return hoverMessage;
    }

    public void OnHandEnter()
    {
        // Opcional: Feedback visual como cambiar de color o resaltar borde
        GetComponent<Renderer>().material.color = Color.yellow;
    }

    public void OnHandExit()
    {
        GetComponent<Renderer>().material.color = Color.white;
    }

    public void Interact()
    {
        Debug.Log($"Interactuaste con: {objectName}");
        // Aquí luego abres un acertijo, recoges un ítem, abres un cajón, etc.
    }
}