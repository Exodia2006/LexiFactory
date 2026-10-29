using UnityEngine;

public class PlayerHandManager : MonoBehaviour
{
    [Header("Punto de la Mano")]
    [SerializeField] private Transform handHoldPoint;

    [Header("Raycast Config")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float pickupDistance = 10f;
    [SerializeField] private LayerMask interactableLayer;

    public ToyItem HeldItem { get; private set; }
    public bool IsHoldingItem => HeldItem != null;

    private void Awake()
    {
        // Si ToyInteractionSystem está presente en la escena, ToyInteractionSystem es el sistema oficial y activo.
        // Deshabilitamos este componente para evitar duplicidad de agarre, conflictos de input y corrupción de capas.
        if (FindFirstObjectByType<ToyInteractionSystem>() != null)
        {
            Debug.Log("[PlayerHandManager] Desactivado automáticamente porque ToyInteractionSystem está activo en la escena.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        if (playerCamera != null)
        {
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            Debug.DrawRay(ray.origin, ray.direction * pickupDistance, Color.red);
        }

        // Clic Izquierdo para interactuar o recoger
        if (Input.GetMouseButtonDown(0))
        {
            HandleInteraction();
        }

        // Clic Derecho o 'G' para soltar al suelo
        if ((Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.G)) && IsHoldingItem)
        {
            DropItemToGround();
        }
    }

    private void HandleInteraction()
    {
        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, pickupDistance, interactableLayer, QueryTriggerInteraction.Collide))
        {
            // CASO A: Tengo objeto en mano -> Intentar depositar en la caja
            if (IsHoldingItem)
            {
                ToyBoxDropZone boxZone = hit.collider.GetComponentInParent<ToyBoxDropZone>();
                if (boxZone == null)
                {
                    boxZone = hit.collider.GetComponentInChildren<ToyBoxDropZone>();
                }

                if (boxZone != null)
                {
                    boxZone.TryDepositItem(this);
                }
            }
            // CASO B: Mano vacía -> Recoger juguete
            else
            {
                ToyItem item = hit.collider.GetComponent<ToyItem>();
                if (item == null) item = hit.collider.GetComponentInParent<ToyItem>();

                if (item != null)
                {
                    HeldItem = item;
                    HeldItem.Pickup(handHoldPoint);
                    Debug.Log($"[HAND] Objeto recogido: {item.ItemID}");
                }
            }
        }
    }

    public void DropItemToGround()
    {
        if (!IsHoldingItem) return;

        ToyItem itemToDrop = HeldItem;
        HeldItem = null;
        itemToDrop.Drop();
    }

    public ToyItem ClearHand()
    {
        ToyItem itemToReturn = HeldItem;
        HeldItem = null;
        return itemToReturn;
    }
}
