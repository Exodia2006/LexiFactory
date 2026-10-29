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

        int toyLayer = LayerMask.NameToLayer("Toy");
        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        // Si la capa era Ignore Raycast o Default (0) pero existe la capa "Toy" (8), asegurar pertenencia a "Toy"
        if (gameObject.layer == ignoreRaycastLayer || (gameObject.layer == 0 && toyLayer != -1))
        {
            initialLayer = toyLayer != -1 ? toyLayer : 0;
            SetLayerRecursively(gameObject, initialLayer);
        }
        else
        {
            initialLayer = gameObject.layer;
        }
    }

    public void Pickup(Transform handHoldPoint)
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Desactivar colliders mientras se sostiene para que no choquen con el jugador ni bloqueen el raycast
        allColliders = GetComponentsInChildren<Collider>(true);
        if (allColliders != null)
        {
            foreach (var col in allColliders)
            {
                if (col != null) col.enabled = false;
            }
        }

        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycast != -1)
        {
            SetLayerRecursively(gameObject, ignoreRaycast);
        }

        transform.SetParent(handHoldPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        transform.SetParent(null);

        // Restaurar capa interactuable válida (nunca dejar en Ignore Raycast)
        int toyLayer = LayerMask.NameToLayer("Toy");
        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        int layerToRestore = initialLayer;

        if (layerToRestore == ignoreRaycast || (layerToRestore == 0 && toyLayer != -1))
        {
            layerToRestore = toyLayer != -1 ? toyLayer : 0;
        }

        SetLayerRecursively(gameObject, layerToRestore);

        // Reactivar TODOS los colliders del objeto y sus hijos
        allColliders = GetComponentsInChildren<Collider>(true);
        if (allColliders != null)
        {
            foreach (var col in allColliders)
            {
                if (col != null) col.enabled = true;
            }
        }

        // Reactivar física en el Rigidbody
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.WakeUp();
        }
    }

    public void PlaceInBox(Transform targetBoxSpot)
    {
        // Cambiar parentesco a la caja
        transform.SetParent(targetBoxSpot);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        int toyLayer = LayerMask.NameToLayer("Toy");
        int layerToRestore = (initialLayer != LayerMask.NameToLayer("Ignore Raycast")) ? initialLayer : (toyLayer != -1 ? toyLayer : 0);
        SetLayerRecursively(gameObject, layerToRestore);

        // Reactivar colliders
        allColliders = GetComponentsInChildren<Collider>(true);
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

        int toyLayer = LayerMask.NameToLayer("Toy");
        int layerToRestore = (initialLayer != LayerMask.NameToLayer("Ignore Raycast")) ? initialLayer : (toyLayer != -1 ? toyLayer : 0);
        SetLayerRecursively(gameObject, layerToRestore);

        allColliders = GetComponentsInChildren<Collider>(true);
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
            rb.WakeUp();
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
