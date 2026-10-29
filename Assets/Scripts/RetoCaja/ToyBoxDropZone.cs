using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

public class ToyBoxDropZone : MonoBehaviour
{
    // Lista oficial predeterminada de los 6 objetos a recolectar en orden
    private static readonly List<string> DefaultSequence6 = new List<string>
    {
        "BarcoAzul",
        "Cubo morado",
        "cono verde",
        "carro rojo",
        "rectangulo naranja",
        "puente"
    };

    [Header("Configuración de la Solución (6 Objetos Secuenciales)")]
    [Tooltip("Lista ordenada con los 6 nombres/IDs de los objetos requeridos")]
    [SerializeField] private List<string> correctSequence = new List<string>
    {
        "BarcoAzul",
        "Cubo morado",
        "cono verde",
        "carro rojo",
        "rectangulo naranja",
        "puente"
    };

    [Header("Pistas de Audio Secuenciales (6 Pistas)")]
    [Tooltip("Clips de audio para cada pista en el mismo orden que la secuencia (Índices 0 al 5)")]
    [SerializeField] private List<AudioClip> clueAudios = new List<AudioClip>(6);
    [Tooltip("Audio de respaldo para la primera pista si no se asignó en clueAudios[0]")]
    [SerializeField] private AudioClip instructionAudio;

    [Header("Puntos de Aparición Iniciales (Respawn - 6 Puntos)")]
    [Tooltip("Puntos de spawn a los que vuelven los 6 objetos al fallar el reto (Índices 0 al 5)")]
    [SerializeField] private Transform[] toyInitialSpawnPoints = new Transform[6];

    [Tooltip("Lista de todos los juguetes de la escena (se autocompleta si está vacía)")]
    [SerializeField] private List<ToyItem> allToysInScene = new List<ToyItem>();

    [Header("UI y Sistema de Interacción")]
    [SerializeField] private GameObject crosshairUI;
    [SerializeField] private ToyInteractionSystem interactionSystem;
    [SerializeField] private GenericPuzzleInteractable puzzleInteractable;

    [Header("Visualización del Código en la Caja")]
    [SerializeField] private GameObject codeDisplayObject;
    [SerializeField] private TextMeshPro codeText3D;
    [SerializeField] private TextMeshProUGUI codeTextUI;
    [Tooltip("Separador entre dígitos del código mostrado (ej: ' ' → '7 ? 9 2', '' → '7?92')")]
    [SerializeField] private string codeSeparator = " ";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip depositSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip victorySound;

    [Header("GameManager Code Index")]
    [SerializeField] private int codeDigitIndex = 1;

    public bool IsPuzzleActive { get; private set; } = false;
    public int CurrentClueIndex => currentClueIndex;
    public int TotalObjectsCount => correctSequence != null ? correctSequence.Count : 6;
    public string CurrentExpectedName => (correctSequence != null && currentClueIndex >= 0 && currentClueIndex < correctSequence.Count) 
        ? correctSequence[currentClueIndex] 
        : string.Empty;

    private int currentClueIndex = 0;
    private Coroutine clueCoroutine;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (codeDisplayObject != null)
            codeDisplayObject.SetActive(false);

        if (interactionSystem == null)
            interactionSystem = FindFirstObjectByType<ToyInteractionSystem>();

        // Migración automática si el objeto en la escena tenía guardada la secuencia antigua de 3 ítems
        if (correctSequence == null || correctSequence.Count == 0 ||
            (correctSequence.Count == 3 && correctSequence.Contains("Ship") && correctSequence.Contains("Pelota")))
        {
            Debug.LogWarning("[ToyBoxDropZone] Actualizando secuencia de juguetes a la lista oficial de 6 objetos.");
            correctSequence = new List<string>(DefaultSequence6);
        }

        // Asegurar tamaño del array de puntos de spawn para evitar IndexOutOfRangeException
        if (toyInitialSpawnPoints == null || toyInitialSpawnPoints.Length < 6)
        {
            Array.Resize(ref toyInitialSpawnPoints, 6);
        }

        // Si la lista de juguetes no se asignó en el Inspector o contiene nulos, asegurar todos los juguetes de la escena
        if (allToysInScene == null)
        {
            allToysInScene = new List<ToyItem>();
        }
        allToysInScene.RemoveAll(t => t == null);

        var foundToys = FindObjectsByType<ToyItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in foundToys)
        {
            if (t != null && !allToysInScene.Contains(t))
            {
                allToysInScene.Add(t);
            }
        }
    }

    private void Start()
    {
        // 4. Gestión de los 6 objetos: asegurarse de que comiencen completamente DESACTIVADOS en la escena
        IsPuzzleActive = false;
        currentClueIndex = 0;
        SetAllToysActive(false);
        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (interactionSystem != null) interactionSystem.StopInteraction();
        RefreshCodeDisplay();
    }

    private void OnValidate()
    {
        if (correctSequence == null || correctSequence.Count == 0)
        {
            correctSequence = new List<string>(DefaultSequence6);
        }

        if (toyInitialSpawnPoints == null || toyInitialSpawnPoints.Length < 6)
        {
            Array.Resize(ref toyInitialSpawnPoints, 6);
        }
    }

    [ContextMenu("Restablecer Secuencia Oficial (6 Objetos)")]
    public void ResetToOfficial6ItemsSequence()
    {
        correctSequence = new List<string>(DefaultSequence6);
        if (toyInitialSpawnPoints == null || toyInitialSpawnPoints.Length < 6)
        {
            Array.Resize(ref toyInitialSpawnPoints, 6);
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log("[ToyBoxDropZone] Secuencia oficial de 6 objetos restablecida con éxito.");
    }

    public void StartPuzzle()
    {
        GameManager.RegisterActivePuzzle(this);
        IsPuzzleActive = true;
        currentClueIndex = 0;

        // Activar los 6 objetos en sus posiciones iniciales de spawn únicamente cuando el reto inicia
        ResetAllToys();

        // Activar la mira (Crosshair UI)
        if (crosshairUI != null) crosshairUI.SetActive(true);
        if (interactionSystem != null) interactionSystem.StartInteraction();

        // Reproducir de inmediato la pista actual desde AudioSource
        PlayCurrentClueAudio();

        Debug.Log($"[ToyBox] Reto iniciado. Esperando objeto 1/{correctSequence.Count}: '{correctSequence[0]}'. Juguetes activados en escena.");
    }

    public void StopPuzzle()
    {
        IsPuzzleActive = false;
        GameManager.UnregisterActivePuzzle(this);

        if (clueCoroutine != null)
        {
            StopCoroutine(clueCoroutine);
            clueCoroutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Apagar por completo el sistema de interacción: oculta la mira, suelta el juguete y detiene el raycast
        if (interactionSystem != null) interactionSystem.StopInteraction();
        if (crosshairUI != null) crosshairUI.SetActive(false);

        // Si el jugador tenía algún juguete agarrado, soltarlo
        if (interactionSystem != null && interactionSystem.IsHoldingToy)
        {
            interactionSystem.DropHeldToy();
        }

        // Si el reto se cancela o no está activo, mantenerlos ocultos para no saturar la habitación
        SetAllToysActive(false);
    }

    private void OnEnable()
    {
        // Mantener el cajón sincronizado cuando OTROS puzzles revelan sus dígitos después
        GameManager.OnCodeChanged += RefreshCodeDisplay;
    }

    private void OnDisable()
    {
        GameManager.OnCodeChanged -= RefreshCodeDisplay;
        StopPuzzle();
    }

    /// <summary>
    /// Muestra en el cajón el progreso completo del código (ej: "7 ? 9 2").
    /// </summary>
    private void RefreshCodeDisplay()
    {
        if (GameManager.Instance == null) return;

        string progress = GameManager.Instance.GetDiscoveredCodeString(codeSeparator ?? string.Empty);
        if (codeText3D != null) codeText3D.text = progress;
        if (codeTextUI != null) codeTextUI.text = progress;
    }

    private void OnDestroy()
    {
        StopPuzzle();
    }

    public void SetAllToysActive(bool active)
    {
        if (allToysInScene == null) return;
        for (int i = 0; i < allToysInScene.Count; i++)
        {
            if (allToysInScene[i] != null)
            {
                allToysInScene[i].gameObject.SetActive(active);
            }
        }
    }

    // Trigger de la caja para cuando el jugador deposite o suelte un juguete dentro
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPuzzleActive) return;

        ToyItem toy = other.GetComponent<ToyItem>();
        if (toy == null) toy = other.GetComponentInParent<ToyItem>();

        if (toy != null)
        {
            // Si el jugador tenía el juguete agarrado en su mano, liberarlo
            if (interactionSystem != null && interactionSystem.HeldToy == toy)
            {
                interactionSystem.ClearHeldToy();
            }

            DepositToy(toy);
        }
    }

    // Método para depositar un juguete (compatible con Trigger, interacción directa o PlayerHandManager)
    public void DepositToy(ToyItem toy)
    {
        if (!IsPuzzleActive || toy == null) return;

        if (currentClueIndex >= correctSequence.Count) return;

        if (audioSource != null && depositSound != null)
            audioSource.PlayOneShot(depositSound);

        string expectedName = correctSequence[currentClueIndex];
        string depositedID = toy.ItemID ?? string.Empty;
        string depositedName = toy.gameObject != null ? toy.gameObject.name : string.Empty;

        Debug.Log($"[ToyBox] Juguete ingresado: '{depositedName}' (ID: '{depositedID}') | Esperado paso {currentClueIndex + 1}/{correctSequence.Count}: '{expectedName}'");

        // 1. Validar si el juguete corresponde al orden esperado (con comparación flexible y por subcadena)
        if (IsMatchingToy(toy, currentClueIndex))
        {
            HandleCorrectToy(toy);
        }
        // 2. Si el juguete es incorrecto (falla el reto):
        else
        {
            HandleWrongToy();
        }
    }

    private void HandleCorrectToy(ToyItem toy)
    {
        // Reproducir sonido de acierto
        if (audioSource != null && successSound != null)
            audioSource.PlayOneShot(successSound);

        // Efecto ScreenFlash verde
        if (ScreenFlash.Instance != null)
            ScreenFlash.Instance.FlashSuccess();

        // Ocultar el juguete depositado
        toy.gameObject.SetActive(false);

        currentClueIndex++;

        // Si se completaron los 6 juguetes requeridos
        if (currentClueIndex >= correctSequence.Count)
        {
            WinPuzzle();
        }
        else
        {
            // Reproducir automáticamente la pista de audio del siguiente juguete
            if (clueCoroutine != null) StopCoroutine(clueCoroutine);
            clueCoroutine = StartCoroutine(PlayNextClueAfterDelay(0.7f));
        }
    }

    private void HandleWrongToy()
    {
        Debug.LogWarning($"[ToyBox] ¡Secuencia incorrecta en el paso {currentClueIndex + 1}! Reiniciando reto...");

        // 1. Reproducir sonido de error
        if (audioSource != null && errorSound != null)
            audioSource.PlayOneShot(errorSound);

        // 2. ScreenFlash rojo
        if (ScreenFlash.Instance != null)
            ScreenFlash.Instance.FlashError();

        // 3. Si el jugador tenía algún juguete agarrado, soltarlo
        if (interactionSystem != null && interactionSystem.IsHoldingToy)
        {
            interactionSystem.DropHeldToy();
        }

        // 4. Regresar todos los 6 objetos a sus respectivas posiciones iniciales de spawn y resetear velocidad lineal/angular
        ResetAllToys();

        // 5. Reiniciar índice objetivo a 0
        currentClueIndex = 0;

        // 6. Volver a reproducir la pista de audio 1 ("BarcoAzul") tras un retardo de feedback
        if (clueCoroutine != null) StopCoroutine(clueCoroutine);
        clueCoroutine = StartCoroutine(PlayNextClueAfterDelay(1.0f));
    }

    private void ResetAllToys()
    {
        if (allToysInScene == null) return;

        for (int i = 0; i < allToysInScene.Count; i++)
        {
            ToyItem toy = allToysInScene[i];
            if (toy == null) continue;

            // Reactivar el GameObject por si estaba oculto al haber sido depositado
            toy.gameObject.SetActive(true);

            Transform spawnPoint = GetSpawnPointForToy(toy, i);

            if (spawnPoint != null)
            {
                toy.transform.SetParent(null);
                toy.transform.position = spawnPoint.position;
                toy.transform.rotation = spawnPoint.rotation;

                // Restaurar capa interactuable
                int toyLayer = LayerMask.NameToLayer("Toy");
                toy.SetLayerRecursively(toy.gameObject, toyLayer != -1 ? toyLayer : 0);

                // Resetear velocidad lineal y angular del Rigidbody
                if (toy.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.WakeUp();
                }

                Collider[] colliders = toy.GetComponentsInChildren<Collider>(true);
                foreach (var col in colliders)
                {
                    if (col != null) col.enabled = true;
                }
            }
            else
            {
                // Fallback seguro: usar el reseteo propio de ToyItem si no hay spawn point configurado
                toy.ResetToInitialPosition();
            }
        }
    }

    private Transform GetSpawnPointForToy(ToyItem toy, int fallbackIndex)
    {
        if (toy == null || toyInitialSpawnPoints == null || toyInitialSpawnPoints.Length == 0)
            return null;

        // 1. Buscar si el juguete corresponde semánticamente a uno de los 6 pasos (0 a 5)
        for (int k = 0; k < correctSequence.Count; k++)
        {
            if (IsMatchingToy(toy, k))
            {
                if (k < toyInitialSpawnPoints.Length && toyInitialSpawnPoints[k] != null)
                {
                    return toyInitialSpawnPoints[k];
                }
                break;
            }
        }

        // 2. Si el índice de posición directa es válido y no nulo
        if (fallbackIndex >= 0 && fallbackIndex < toyInitialSpawnPoints.Length && toyInitialSpawnPoints[fallbackIndex] != null)
        {
            return toyInitialSpawnPoints[fallbackIndex];
        }

        // 3. Búsqueda por similitud de nombre en los puntos de spawn
        string toyNorm = StripSeparators(NormalizeString(toy.gameObject.name));
        foreach (var pt in toyInitialSpawnPoints)
        {
            if (pt != null)
            {
                string ptNorm = StripSeparators(NormalizeString(pt.gameObject.name));
                if (!string.IsNullOrEmpty(ptNorm) && (ptNorm.Contains(toyNorm) || toyNorm.Contains(ptNorm)))
                {
                    return pt;
                }
            }
        }

        return null;
    }

    private void PlayCurrentClueAudio()
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = null;

        if (clueAudios != null && currentClueIndex >= 0 && currentClueIndex < clueAudios.Count)
        {
            clipToPlay = clueAudios[currentClueIndex];
        }

        // Fallback: si clueAudios[0] no está asignado pero existe instructionAudio
        if (clipToPlay == null && currentClueIndex == 0 && instructionAudio != null)
        {
            clipToPlay = instructionAudio;
        }

        if (clipToPlay != null)
        {
            audioSource.Stop();
            audioSource.clip = clipToPlay;
            audioSource.Play();
            Debug.Log($"[ToyBox] Reproduciendo pista {currentClueIndex + 1}/{correctSequence.Count}: '{clipToPlay.name}' para '{CurrentExpectedName}'");
        }
        else
        {
            Debug.LogWarning($"[ToyBox] Pista {currentClueIndex + 1}/{correctSequence.Count} ('{CurrentExpectedName}') sin AudioClip asignado en clueAudios[{currentClueIndex}].");
        }
    }

    private IEnumerator PlayNextClueAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayCurrentClueAudio();
    }

    private void WinPuzzle()
    {
        IsPuzzleActive = false;
        GameManager.UnregisterActivePuzzle(this);
        Debug.Log("[ToyBox] ¡PUZZLE DE LA CAJA RESUELTO CORRECTAMENTE! Los 6 objetos fueron depositados en orden.");

        if (clueCoroutine != null)
        {
            StopCoroutine(clueCoroutine);
            clueCoroutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // ScreenFlash verde de éxito
        if (ScreenFlash.Instance != null)
            ScreenFlash.Instance.FlashSuccess();

        // 1. Reproducir sonido de victoria final del puzzle
        AudioClip winAudio = victorySound != null ? victorySound : successSound;
        if (audioSource != null && winAudio != null)
        {
            audioSource.PlayOneShot(winAudio);
        }

        // 2. Ocultar la retícula central (Crosshair UI)
        if (crosshairUI != null) crosshairUI.SetActive(false);
        if (interactionSystem != null) interactionSystem.StopInteraction();

        // 3. Revelar el dígito correspondiente en GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RevealDigit(codeDigitIndex);
        }

        // Mostrar el progreso COMPLETO del código (ej: "7 ? 9 2"), no solo el dígito de este puzzle
        RefreshCodeDisplay();

        if (codeDisplayObject != null)
            codeDisplayObject.SetActive(true);

        // 4. Marcar el puzzle como completado (MarkAsSolved())
        GenericPuzzleInteractable interactable = puzzleInteractable != null ? puzzleInteractable : GetComponent<GenericPuzzleInteractable>();
        if (interactable == null) interactable = GetComponentInParent<GenericPuzzleInteractable>();

        if (interactable != null)
        {
            interactable.MarkAsSolved();
        }
    }

    // --- SISTEMA DE VALIDACIÓN FLEXIBLE DE NOMBRES Y SUBCADENAS ---

    public bool IsMatchingToy(ToyItem toy, int stepIndex)
    {
        if (toy == null || correctSequence == null || stepIndex < 0 || stepIndex >= correctSequence.Count)
            return false;

        string expectedName = correctSequence[stepIndex];
        string rawId = toy.ItemID ?? string.Empty;
        string rawObjName = toy.gameObject != null ? toy.gameObject.name : string.Empty;

        // Normalizar textos (remover diacríticos/tildes, pasar a minúsculas y trim)
        string normExpected = NormalizeString(expectedName);
        string cleanExpected = StripSeparators(normExpected);

        string normId = NormalizeString(rawId);
        string cleanId = StripSeparators(normId);

        string normObjName = NormalizeString(rawObjName);
        string cleanObjName = StripSeparators(normObjName);

        // 1. Verificación por subcadena o coincidencia compacta directa
        if (!string.IsNullOrEmpty(cleanExpected))
        {
            // Coincidencia con ItemID
            if (!string.IsNullOrEmpty(cleanId))
            {
                if (cleanId == cleanExpected || cleanId.Contains(cleanExpected) || cleanExpected.Contains(cleanId))
                    return true;
            }

            // Coincidencia con Nombre de GameObject (ej: "Toy_BarcoAzul", "carro rojo (Clone)")
            if (!string.IsNullOrEmpty(cleanObjName))
            {
                if (cleanObjName == cleanExpected || cleanObjName.Contains(cleanExpected))
                    return true;
            }

            // Verificación por palabras/tokens individuales (ej: "carro rojo" -> contiene "carro" Y "rojo")
            string[] tokens = normExpected.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 1)
            {
                bool allTokensInId = !string.IsNullOrEmpty(normId);
                bool allTokensInName = !string.IsNullOrEmpty(normObjName);

                foreach (string token in tokens)
                {
                    if (allTokensInId && !normId.Contains(token)) allTokensInId = false;
                    if (allTokensInName && !normObjName.Contains(token)) allTokensInName = false;
                }

                if (allTokensInId || allTokensInName)
                    return true;
            }
        }

        // 2. Verificación semántica / sinónimos oficiales de los 6 pasos (soporte bilingüe ES / EN)
        return CheckSemanticMatch(stepIndex, cleanId, cleanObjName, normId, normObjName);
    }

    private bool CheckSemanticMatch(int stepIndex, string cleanId, string cleanName, string normId, string normName)
    {
        string combined = $"{cleanId} {cleanName} {normId} {normName}";

        switch (stepIndex)
        {
            case 0: // 1. "BarcoAzul" (o barco azul / ship blue)
                bool isShip = combined.Contains("barco") || combined.Contains("bote") || combined.Contains("ship") || combined.Contains("boat") || combined.Contains("navio");
                bool isBlue = combined.Contains("azul") || combined.Contains("blue");
                if (isShip && isBlue) return true;
                if (combined.Contains("barcoazul") || combined.Contains("blueship") || combined.Contains("blueboat")) return true;
                if (cleanId == "ship" || cleanId == "barco" || cleanName == "barco" || cleanName == "ship") return true;
                break;

            case 1: // 2. "Cubo morado" (cubo morado / cube purple)
                bool isCube = combined.Contains("cubo") || combined.Contains("cube");
                bool isPurple = combined.Contains("morado") || combined.Contains("purple") || combined.Contains("violeta") || combined.Contains("purpura");
                if (isCube && isPurple) return true;
                if (combined.Contains("cubomorado") || combined.Contains("cubepurple") || combined.Contains("purplecube")) return true;
                break;

            case 2: // 3. "cono verde" (cono verde / cone green)
                bool isCone = combined.Contains("cono") || combined.Contains("cone");
                bool isGreen = combined.Contains("verde") || combined.Contains("green");
                if (isCone && isGreen) return true;
                if (combined.Contains("conoverde") || combined.Contains("conegreen") || combined.Contains("greencone")) return true;
                break;

            case 3: // 4. "carro rojo" (carro rojo / red car)
                bool isCar = combined.Contains("carro") || combined.Contains("car") || combined.Contains("coche") || combined.Contains("auto") || combined.Contains("vehiculo");
                bool isRed = combined.Contains("rojo") || combined.Contains("red");
                if (isCar && isRed) return true;
                if (combined.Contains("carrorojo") || combined.Contains("redcar") || combined.Contains("cocherojo")) return true;
                break;

            case 4: // 5. "rectangulo naranja" (rectángulo naranja / orange rectangle)
                bool isRect = combined.Contains("rectangulo") || combined.Contains("rectangle") || combined.Contains("bloque") || combined.Contains("block") || combined.Contains("prisma") || combined.Contains("prism");
                bool isOrange = combined.Contains("naranja") || combined.Contains("orange");
                if (isRect && isOrange) return true;
                if (combined.Contains("rectangulonaranja") || combined.Contains("orangerectangle")) return true;
                break;

            case 5: // 6. "puente" (puente / bridge)
                if (combined.Contains("puente") || combined.Contains("bridge")) return true;
                break;
        }

        return false;
    }

    public static string NormalizeString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Normalización FormD para separar marcas diacríticas (tildes: á, é, í, ó, ú, etc.)
        string normalized = input.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();

        foreach (char c in normalized)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
    }

    public static string StripSeparators(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Replace(" ", "").Replace("_", "").Replace("-", "").Replace("(", "").Replace(")", "");
    }

    // Compatibilidad con PlayerHandManager si aún se utiliza en el proyecto
    public void TryDepositItem(PlayerHandManager playerHand)
    {
        if (playerHand == null || !playerHand.IsHoldingItem) return;

        ToyItem item = playerHand.ClearHand();
        DepositToy(item);
    }
}