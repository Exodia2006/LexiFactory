using UnityEngine;

public class ToyItem : MonoBehaviour
{
    [Header("Identificador del Objeto")]
    [SerializeField] private string itemID = "Ship";

    public string ItemID => itemID;

    // Datos para el reinicio
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Transform initialParent;
    private int initialLayer;

    private Collider[] allColliders;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        allColliders = GetComponentsInChildren<Collider>(true);

        // Guardar la transformación inicial en la escena
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialParent = transform.parent;
        initialLayer = gameObject.layer;
    }

    public void Pickup(Transform handHoldPoint)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Desactivar colliders para que no tapen el Raycast
        if (allColliders != null)
        {
            foreach (var col in allColliders)
            {
                if (col != null) col.enabled = false;
            }
        }

        SetLayerRecursively(gameObject, LayerMask.NameToLayer("Ignore Raycast"));

        transform.SetParent(handHoldPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        transform.SetParent(null);
        SetLayerRecursively(gameObject, initialLayer);

        if (allColliders != null)
        {
            foreach (var col in allColliders)
            {
                if (col != null) col.enabled = true;
            }
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void PlaceInBox(Transform targetBoxSpot)
    {
        // Cambiar parentesco a la caja
        transform.SetParent(targetBoxSpot);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Reactivar colliders
        if (allColliders != null)
        {
            foreach (var col in allColliders)
            {
                if (col != null) col.enabled = true;
            }
        }
    }

    // Método para devolver el juguete a su sitio original al fallar
    public void ResetToInitialPosition()
    {
        gameObject.SetActive(true);

        transform.SetParent(initialParent);
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        SetLayerRecursively(gameObject, initialLayer);

        if (allColliders != null)
        {
            foreach (var col in allColliders)
            {
                if (col != null) col.enabled = true;
            }
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child != null) SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}