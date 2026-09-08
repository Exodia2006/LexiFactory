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

        if (hits.Length > 0)
        {
            if (hits[0].TryGetComponent<IInteractable>(out var interactable))
            {
                if (currentInteractable != interactable)
                {
                    currentInteractable?.OnHandExit();
                    currentInteractable = interactable;
                    currentInteractable.OnHandEnter();

                    // Mostrar el texto justo en la posición 3D del objeto detectado
                    Vector3 targetPos = hits[0].transform.position;
                    FloatingTextManager.Instance?.ShowText(currentInteractable.GetHoverText(), targetPos);
                }
                return;
            }
        }

        // Si alejamos la mano de los objetos, ocultamos el texto
        if (currentInteractable != null)
        {
            currentInteractable.OnHandExit();
            currentInteractable = null;
            FloatingTextManager.Instance?.Hide();
        }
    }

    private void HandleInput()
    {
        if (currentInteractable != null && (Input.GetKeyDown(interactKey) || Input.GetMouseButtonDown(0)))
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