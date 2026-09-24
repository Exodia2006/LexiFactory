using UnityEngine;

public class HandInteraction : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private Transform detectionPoint;
    [SerializeField] private float detectionRadius = 1.2f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private IInteractable currentInteractable;

    private void Update()
    {
        DetectInteractables();
        HandleInput();
    }

    private void DetectInteractables()
    {
        Transform point = detectionPoint != null ? detectionPoint : transform;
        Collider[] hits = Physics.OverlapSphere(point.position, detectionRadius, interactableLayer);

        IInteractable foundInteractable = null;
        Collider targetCollider = null;

        // Buscamos en TODOS los colliders detectados por la esfera, no solo en el primero
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IInteractable>(out var interactable))
            {
                foundInteractable = interactable;
                targetCollider = hit;
                break; // Encontramos el interactuable válido
            }
        }

        if (foundInteractable != null)
        {
            if (currentInteractable != foundInteractable)
            {
                currentInteractable?.OnHandExit();
                currentInteractable = foundInteractable;
                currentInteractable.OnHandEnter();

                Vector3 targetPos = targetCollider.ClosestPoint(point.position) + Vector3.up * 0.3f;
                FloatingTextManager.Instance?.ShowText(currentInteractable.GetHoverText(), targetPos);
            }
            return;
        }

        // Si alejamos la mano o dejamos de detectar interactuables, ocultamos el texto
        if (currentInteractable != null)
        {
            currentInteractable.OnHandExit();
            currentInteractable = null;
            FloatingTextManager.Instance?.Hide();
        }
    }

    private void HandleInput()
    {
        if (currentInteractable != null && (Input.GetKeyDown(interactKey)))
        {
            currentInteractable.Interact();
        }
    }

    private void OnDrawGizmos()
    {
        Transform point = detectionPoint != null ? detectionPoint : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(point.position, detectionRadius);
    }
}