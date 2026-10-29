using System;
using System.Collections.Generic;
using UnityEngine;

public class ToyInteractionSystem : MonoBehaviour
{
    public static ToyInteractionSystem Instance { get; private set; }

    [Header("Referencias de Cámara y UI")]
    [Tooltip("Cámara principal en primera persona")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("UI de la mira central")]
    [SerializeField] private GameObject crosshairUI;
    [Tooltip("Punto frente a la cámara donde se posiciona el juguete sostenido")]
    [SerializeField] private Transform holdPosition;

    [Header("Configuración del Raycast")]
    [Tooltip("Distancia máxima para detectar e interactuar con juguetes")]
    [SerializeField] private float rayDistance = 4f;
    [Tooltip("LayerMask para filtrar los juguetes interactuables")]
    [SerializeField] private LayerMask toyLayerMask = ~0;

    [Header("Shader / Material de Highlight")]
    [Tooltip("Material de contorno (outline) aplicado al apuntar a un juguete")]
    [SerializeField] private Material outlineMaterial;

    [Header("Estado de Interacción")]
    [SerializeField] private bool isActive = true;

    // Juguete actualmente sostenido
    public ToyItem HeldToy { get; private set; }
    public bool IsHoldingToy => HeldToy != null;

    // Juguete y renderers apuntados actualmente
    private ToyItem currentHoveredToy;
    private readonly List<RendererMaterialBackup> materialBackups = new List<RendererMaterialBackup>();

    private struct RendererMaterialBackup
    {
        public Renderer renderer;
        public Material[] originalMaterials;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        // Si no se asignó holdPosition, crear uno automáticamente frente a la cámara
        if (holdPosition == null && playerCamera != null)
        {
            GameObject autoHold = new GameObject("AutoHoldPosition");
            autoHold.transform.SetParent(playerCamera.transform);
            autoHold.transform.localPosition = new Vector3(0f, -0.25f, 0.7f);
            autoHold.transform.localRotation = Quaternion.identity;
            holdPosition = autoHold.transform;
        }

        if (crosshairUI != null && !isActive)
        {
            crosshairUI.SetActive(false);
        }
    }

    private void OnDisable()
    {
        ClearHighlight();
    }

    private void OnDestroy()
    {
        ClearHighlight();
        if (Instance == this) Instance = null;
    }

    public void StartInteraction()
    {
        isActive = true;
        SetCrosshairActive(true);
    }

    public void StopInteraction()
    {
        isActive = false;
        ClearHighlight();
        SetCrosshairActive(false);

        if (IsHoldingToy)
        {
            DropHeldToy();
        }
    }

    public void SetCrosshairActive(bool active)
    {
        if (crosshairUI != null)
        {
            crosshairUI.SetActive(active);
        }
    }

    private void Update()
    {
        if (!isActive) return;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return;
        }

        // Raycast continuo desde el centro de la pantalla (Viewport 0.5, 0.5)
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!IsHoldingToy)
        {
            HandleHoverRaycast(ray);
            HandlePickupInput(ray);
        }
        else
        {
            // Si ya tiene un objeto en mano, quitamos el highlight de cualquier otro
            ClearHighlight();
            HandleHeldItemActions(ray);
        }
    }

    private ToyItem FindToyUnderRay(Ray ray)
    {
        // RaycastAll para que triggers intermedios o el collider del Player no bloqueen la detección
        RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance, ~0, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0) return null;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            // Ignorar el propio jugador y sus colliders
            if (hit.collider.transform.root == transform.root || hit.collider.CompareTag("Player"))
                continue;

            ToyItem toy = hit.collider.GetComponent<ToyItem>();
            if (toy == null) toy = hit.collider.GetComponentInParent<ToyItem>();

            if (toy != null && toy.gameObject.activeInHierarchy)
            {
                return toy;
            }

            // Si es un objeto sólido (no trigger) frente a la cámara y no es un juguete, bloquea la visión
            if (!hit.collider.isTrigger)
            {
                break;
            }
        }

        return null;
    }

    private void HandleHoverRaycast(Ray ray)
    {
        ToyItem toy = FindToyUnderRay(ray);
        if (toy != null)
        {
            HighlightToy(toy);
        }
        else
        {
            ClearHighlight();
        }
    }

    private void HandlePickupInput(Ray ray)
    {
        // Agarrar con tecla 'E' o Clic Izquierdo
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            ToyItem toyToGrab = currentHoveredToy != null ? currentHoveredToy : FindToyUnderRay(ray);
            if (toyToGrab != null)
            {
                ClearHighlight(); // Restaurar materiales antes de agarrar
                PickupToy(toyToGrab);
            }
        }
    }

    private void HandleHeldItemActions(Ray ray)
    {
        // Soltar objeto con Clic Derecho o 'Q'
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Q))
        {
            DropHeldToy();
            return;
        }

        // Permitir depositar directamente si se apunta al Trigger/Collider de la caja y se presiona 'E' o Clic Izquierdo
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, ~0, QueryTriggerInteraction.Collide))
            {
                ToyBoxDropZone boxZone = hit.collider.GetComponent<ToyBoxDropZone>();
                if (boxZone == null) boxZone = hit.collider.GetComponentInParent<ToyBoxDropZone>();

                if (boxZone != null && boxZone.IsPuzzleActive)
                {
                    ToyItem item = ClearHeldToy();
                    boxZone.DepositToy(item);
                }
            }
        }
    }

    public void PickupToy(ToyItem toy)
    {
        if (toy == null) return;

        HeldToy = toy;
        HeldToy.Pickup(holdPosition);

        // Poner su Rigidbody en isKinematic = true
        if (HeldToy.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[ToyInteractionSystem] Agarrado: {toy.ItemID}");
    }

    public void DropHeldToy()
    {
        if (HeldToy == null) return;

        Debug.Log($"[ToyInteractionSystem] Soltado: {HeldToy.ItemID}");
        ToyItem itemToDrop = HeldToy;
        HeldToy = null;

        // Posicionar al soltar ligeramente adelante de la cámara para que no colisione con el CharacterController
        if (playerCamera != null)
        {
            Vector3 dropPosition = playerCamera.transform.position + playerCamera.transform.forward * 0.8f;
            itemToDrop.transform.position = dropPosition;
        }

        itemToDrop.Drop();

        // Aplicar leve impulso hacia adelante para una caída natural y visible
        if (itemToDrop.TryGetComponent<Rigidbody>(out var rb) && playerCamera != null)
        {
            rb.linearVelocity = playerCamera.transform.forward * 1.5f + Vector3.up * 0.5f;
        }

        ClearHighlight();
    }

    public ToyItem ClearHeldToy()
    {
        ToyItem item = HeldToy;
        HeldToy = null;
        return item;
    }

    // --- SISTEMA DE HIGHLIGHT / OUTLINE SHADER ---
    private void HighlightToy(ToyItem toy)
    {
        if (toy == null || currentHoveredToy == toy) return;

        ClearHighlight();

        currentHoveredToy = toy;
        if (outlineMaterial == null) return;

        Renderer[] renderers = toy.GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            Material[] origMats = rend.materials; // Instancias de materiales del renderer
            materialBackups.Add(new RendererMaterialBackup
            {
                renderer = rend,
                originalMaterials = origMats
            });

            // Añadir outlineMaterial al final del array sin perder los materiales originales
            Material[] newMats = new Material[origMats.Length + 1];
            for (int i = 0; i < origMats.Length; i++)
            {
                newMats[i] = origMats[i];
            }
            newMats[newMats.Length - 1] = outlineMaterial;

            rend.materials = newMats;
        }
    }

    private void ClearHighlight()
    {
        if (materialBackups.Count > 0)
        {
            foreach (var backup in materialBackups)
            {
                if (backup.renderer != null && backup.originalMaterials != null)
                {
                    backup.renderer.materials = backup.originalMaterials;
                }
            }
            materialBackups.Clear();
        }

        currentHoveredToy = null;
    }
}
