public interface IInteractable
{
    string GetHoverText();        // El texto que aparecerá al pasar la mano (ej: "Examinar cuaderno")
    void OnHandEnter();           // Cuando la mano entra en contacto
    void OnHandExit();            // Cuando la mano se aleja
    void Interact();              // Acción al presionar Clic o la tecla de interacción
}