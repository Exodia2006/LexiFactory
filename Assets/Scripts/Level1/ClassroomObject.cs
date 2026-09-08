using UnityEngine;

public class ClassroomObject : MonoBehaviour, IInteractable
{
    [SerializeField] private string objectName = "Cuaderno de apuntes";

    public string GetHoverText()
    {
        return objectName; // Este es el texto que flotará al lado del objeto
    }

    public void OnHandEnter() { }
    public void OnHandExit() { }

    public void Interact()
    {
        Debug.Log($"Interactuaste con: {objectName}");
    }
}