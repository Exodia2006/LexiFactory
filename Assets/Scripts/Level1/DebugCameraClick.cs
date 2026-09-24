using UnityEngine;

public class DebugCameraClick : MonoBehaviour
{
    [SerializeField] private Camera cam;

    private void Start()
    {
        if (cam == null) cam = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 20f, Color.red, 3f);

            // RaycastAll obtiene TODOS los objetos que el rayo atraviesa a lo largo de su trayectoria
            RaycastHit[] hits = Physics.RaycastAll(ray, 20f);

            bool foundBlock = false;

            foreach (var hit in hits)
            {
                // Buscamos si alguno de los objetos atravesados tiene el componente RubikBlock
                if (hit.collider.TryGetComponent<RubikBlock>(out var block))
                {
                    Debug.Log($"[DEBUG] ¡BLOQUE ENCONTRADO!: {hit.collider.gameObject.name}");
                    block.ClickBlock(); // Ejecuta el clic del bloque
                    foundBlock = true;
                    break; // Ya encontramos el bloque, salimos del ciclo
                }
            }

            if (!foundBlock)
            {
                Debug.LogWarning("[DEBUG] El rayo no atravesó ningún objeto con el componente RubikBlock.");
            }
        }
    }
}