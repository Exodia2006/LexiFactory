using UnityEngine;

public class PlayerHandManager : MonoBehaviour
{
    [Header("Punto de la Mano")]
    [SerializeField] private Transform handHoldPoint;

    [Header("Raycast Config")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float pickupDistance = 10f; // Distancia aumentada para pruebas
    [SerializeField] private LayerMask interactableLayer;

    public ToyItem HeldItem { get; private set; }
    public bool IsHoldingItem => HeldItem != null;

    private void Update()
    {
        // 1. Dibujar el Raycast de la cámara en la pestaña Scene todo el tiempo
        if (playerCamera == null) playerCamera = Camera.main;

        if (playerCamera != null)
        {
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
            Debug.DrawRay(ray.origin, ray.direction * pickupDistance, Color.red);
        }

        // 2. Clic Izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            HandleInteraction();
        }

        // 3. Clic Derecho o 'G' para soltar al suelo
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
                else
                {
                }
            }
            // CASO B: Mano vacía -> Recoger juguete
            else
            {
                if (hit.collider.TryGetComponent<ToyItem>(out var item))
                {
                    HeldItem = item;
                    HeldItem.Pickup(handHoldPoint);
                    Debug.Log($"[HAND] Objeto recogido: {item.ItemID}");
                }
            }
        }
        else
        {
        }
    }

    public void DropItemToGround()
    {
        if (!IsHoldingItem) return;

        HeldItem.transform.SetParent(null);

        Collider[] colliders = HeldItem.GetComponentsInChildren<Collider>();
        foreach (var col in colliders) col.enabled = true;

        if (HeldItem.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;

        HeldItem = null;
    }

    public ToyItem ClearHand()
    {
        ToyItem itemToReturn = HeldItem;
        HeldItem = null;
        return itemToReturn;
    }
}