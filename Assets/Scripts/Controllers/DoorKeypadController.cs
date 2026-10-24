using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DoorKeypadController : MonoBehaviour, IInteractable
{
    [Header("Cámaras (Cámara Dedicada del Candado 3D)")]
    [Tooltip("Cámara dedicada que enfoca directamente al candado 3D")]
    [SerializeField] private Camera doorKeypadCamera;
    [Tooltip("Cámara principal en primera persona del jugador")]
    [SerializeField] private Camera mainPlayerCamera;

    [Header("Display del Candado 3D")]
    [Tooltip("Texto 3D digital en el candado físico de la escena (TextMeshPro)")]
    [SerializeField] private TextMeshPro displayText3D;
    [Tooltip("Texto digital si se usa Canvas World Space o UI (TextMeshProUGUI)")]
    [SerializeField] private TextMeshProUGUI displayText;
    [Tooltip("Color estándar del display")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("Color del display al ingresar un código erróneo")]
    [SerializeField] private Color errorColor = Color.red;
    [Tooltip("Color del display al ingresar el código correcto")]
    [SerializeField] private Color successColor = Color.green;

    [Header("Detección de Botones Físicos 3D por Raycast")]
    [Tooltip("LayerMask para interactuar con los botones 3D del candado")]
    [SerializeField] private LayerMask keypad3DLayerMask = ~0;
    [SerializeField] private float rayDistance3D = 20f;

    [Header("UI Opcional del Teclado (0 al 9 y Salir)")]
    [Tooltip("GameObject raíz del Canvas del teclado si existe")]
    [SerializeField] private GameObject keypadCanvas;
    [Tooltip("Botones del 0 al 9 en orden (índice 0 = botón 0, índice 9 = botón 9)")]
    [SerializeField] private Button[] digitButtons;
    [Tooltip("Botón opcional para salir/cerrar la vista del candado")]
    [SerializeField] private Button closeButton;
    [Tooltip("Botón opcional de borrar (no requerido con auto-validación)")]
    [SerializeField] private Button clearButton;
    [Tooltip("Botón opcional de confirmar (no requerido con auto-validación)")]
    [SerializeField] private Button submitButton;

    [Header("Configuración de Entrada")]
    [Tooltip("Límite estricto de caracteres del código")]
    [SerializeField] private int maxDigits = 4;
    [Tooltip("Si está activo, rellena los espacios vacíos con guiones (ej: '7 - - -')")]
    [SerializeField] private bool showPlaceholderDashes = true;

    [Header("Condición de Desbloqueo / Acceso Denegado")]
    [Tooltip("Audio reproducido si se intenta interactuar sin resolver todos los puzzles")]
    [SerializeField] private AudioClip lockedSound;
    [Tooltip("Texto o contenedor opcional en pantalla para feedback de acceso bloqueado")]
    [SerializeField] private GameObject lockedFeedbackContainer;
    [SerializeField] private TextMeshProUGUI lockedFeedbackText;
    [SerializeField] private string lockedMessage = "Aún no tienes todos los códigos. Resuelve los demás acertijos primero.";
    [SerializeField] private float lockedFeedbackDuration = 2.5f;

    [Header("Puerta Física y Animación")]
    [Tooltip("Transform de la bisagra de la puerta para rotación suave")]
    [SerializeField] private Transform doorHinge;
    [Tooltip("Ángulo relativo al abrir la puerta")]
    [SerializeField] private Vector3 openRotationOffset = new Vector3(0f, 90f, 0f);
    [Tooltip("Duración de la rotación de apertura")]
    [SerializeField] private float doorOpenDuration = 1.5f;
    [Tooltip("Animator de la puerta si usa clips de animación")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openDoorTriggerName = "OpenDoor";
    [Tooltip("Collider físico de la puerta a desactivar tras abrir")]
    [SerializeField] private Collider doorCollider;

    [Header("Pantalla de Victoria y Escape")]
    [Tooltip("Panel principal de Victoria en el Canvas del juego")]
    [SerializeField] private GameObject victoryPanel;
    [Tooltip("Texto donde se mostrará el tiempo de escape en formato mm:ss")]
    [SerializeField] private TextMeshProUGUI victoryTimeText;
    [Tooltip("Prefijo del texto de victoria (ej: 'Tiempo de escape: ')")]
    [SerializeField] private string victoryTimePrefix = "Tiempo de escape: ";
    [SerializeField] private Button victoryRestartButton;
    [SerializeField] private Button victoryMainMenuButton;
    [SerializeField] private Button victoryQuitButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip doorOpenSound;

    [Header("Prompt Flotante de Interacción")]
    [SerializeField] private GameObject promptContainer;
    [SerializeField] private Image keyIconImage;
    [SerializeField] private TextMeshProUGUI actionDescriptionText;
    [SerializeField] private string hoverActionText = "Inspeccionar candado";

    [Header("Scripts del Jugador a Pausar")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour mouseLookScript;

    // Estado del teclado
    public bool IsKeypadOpen { get; private set; } = false;
    public bool IsDoorOpened { get; private set; } = false;

    private string currentInput = "";
    private bool isProcessing = false;
    private bool isPlayerLooking = false;
    private Coroutine lockedFeedbackCoroutine;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (mainPlayerCamera == null)
            mainPlayerCamera = Camera.main;

        // Asegurar que la cámara dedicada del candado comience desactivada
        if (doorKeypadCamera != null)
            doorKeypadCamera.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (keypadCanvas != null) keypadCanvas.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (promptContainer != null) promptContainer.SetActive(false);
        if (lockedFeedbackContainer != null) lockedFeedbackContainer.SetActive(false);

        // Auto-detectar scripts de movimiento del jugador si están vacíos
        if (playerMovementScript == null)
            playerMovementScript = FindFirstObjectByType<PlayerMovement>();

        if (mouseLookScript == null)
            mouseLookScript = FindFirstObjectByType<MouseLook>();

        // Conectar botones si se asignaron directamente en el Inspector
        if (digitButtons != null)
        {
            for (int i = 0; i < digitButtons.Length; i++)
            {
                int digit = i;
                if (digitButtons[i] != null)
                {
                    digitButtons[i].onClick.RemoveAllListeners();
                    digitButtons[i].onClick.AddListener(() => PressDigitButton(digit.ToString()));
                }
            }
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(PressClearButton);
        }

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(PressSubmitButton);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(PressCloseButton);
        }

        // Conectar botones del panel de victoria
        if (victoryRestartButton != null)
        {
            victoryRestartButton.onClick.RemoveAllListeners();
            victoryRestartButton.onClick.AddListener(RestartGame);
        }

        if (victoryMainMenuButton != null)
        {
            victoryMainMenuButton.onClick.RemoveAllListeners();
            victoryMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (victoryQuitButton != null)
        {
            victoryQuitButton.onClick.RemoveAllListeners();
            victoryQuitButton.onClick.AddListener(QuitGame);
        }

        UpdateDisplay();
    }

    private void Update()
    {
        // 1. Orientar prompt flotante hacia la cámara principal del jugador
        if (isPlayerLooking && !IsKeypadOpen && !IsDoorOpened && promptContainer != null)
        {
            Camera activeCam = mainPlayerCamera != null ? mainPlayerCamera : Camera.main;
            if (activeCam != null)
            {
                promptContainer.transform.rotation = Quaternion.LookRotation(
                    promptContainer.transform.position - activeCam.transform.position
                );
            }
        }

        // 2. Si el teclado 3D está abierto, procesar clics 3D, atajos y salida con Escape
        if (IsKeypadOpen && !isProcessing)
        {
            // Salir con tecla Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                PressCloseButton();
                return;
            }

            // Detección de clics sobre los botones 3D del candado mediante Raycast
            HandleMouseRaycast3D();

            // Soporte físico opcional de teclado numérico (0 al 9)
            for (int i = 0; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    PressDigitButton(i.ToString());
                    return;
                }
            }

            // Borrar con Backspace o Delete
            if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
            {
                PressClearButton();
                return;
            }

            // Confirmar con Enter
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                PressSubmitButton();
                return;
            }
        }
    }

    private void HandleMouseRaycast3D()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = doorKeypadCamera != null ? doorKeypadCamera : (mainPlayerCamera != null ? mainPlayerCamera : Camera.main);
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance3D, keypad3DLayerMask, QueryTriggerInteraction.Collide))
            {
                // 1. Botón con componente DoorKeypadButton explícito
                DoorKeypadButton btn = hit.collider.GetComponent<DoorKeypadButton>();
                if (btn == null) btn = hit.collider.GetComponentInParent<DoorKeypadButton>();

                if (btn != null)
                {
                    btn.OnClick();
                    return;
                }

                // 2. Detección por nombre o tag del collider
                string colName = hit.collider.gameObject.name.ToLowerInvariant();

                if (colName.Contains("clear") || colName.Contains("borrar") || colName.Contains("delete"))
                {
                    PressClearButton();
                    return;
                }

                if (colName.Contains("submit") || colName.Contains("enter") || colName.Contains("confirm") || colName.Contains("ok"))
                {
                    PressSubmitButton();
                    return;
                }

                if (colName.Contains("exit") || colName.Contains("close") || colName.Contains("salir"))
                {
                    PressCloseButton();
                    return;
                }

                // Extraer dígitos (0 al 9) si el nombre contiene el número
                for (int i = 0; i <= 9; i++)
                {
                    if (colName.EndsWith(i.ToString()) || colName.Contains($"_{i}") || colName.Contains($"key_{i}") || colName == i.ToString())
                    {
                        PressDigitButton(i.ToString());
                        return;
                    }
                }
            }
        }
    }

    // =========================================================================
    // 1. CONDICIÓN ESTRICTA DE ACCESO Y APERTURA DE CÁMARA DEDICADA
    // =========================================================================

    public void Interact()
    {
        if (IsDoorOpened || isProcessing) return;

        // Comprobación de exclusividad de puzzles (no permitir si hay otro activo)
        if (GameManager.IsAnyPuzzleActive && !GameManager.IsThisPuzzleActive(this))
        {
            Debug.LogWarning("[DoorKeypad] No se puede interactuar: hay otro puzzle en progreso.");
            return;
        }

        // Comprobación estricta de todos los acertijos previos resueltos
        bool canOpen = CheckAccessPermission();

        if (canOpen)
        {
            OpenKeypad();
        }
        else
        {
            HandleAccessDenied();
        }
    }

    private bool CheckAccessPermission()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.AreAllPuzzlesSolved();
        }

        // Fallback si no existe GameManager en la escena
        return true;
    }

    private void HandleAccessDenied()
    {
        Debug.LogWarning($"[DoorKeypad] {lockedMessage}");

        // 1. Sonido de acceso denegado
        if (audioSource != null && lockedSound != null)
            audioSource.PlayOneShot(lockedSound);

        // 2. Destello sutil de error si existe ScreenFlash
        if (ScreenFlash.Instance != null)
            ScreenFlash.Instance.FlashError();

        // 3. Feedback visual flotante / HUD
        if (FloatingTextManager.Instance != null)
        {
            FloatingTextManager.Instance.ShowText(lockedMessage, transform.position + Vector3.up * 0.4f);
        }

        if (lockedFeedbackContainer != null || lockedFeedbackText != null)
        {
            if (lockedFeedbackCoroutine != null) StopCoroutine(lockedFeedbackCoroutine);
            lockedFeedbackCoroutine = StartCoroutine(ShowLockedFeedbackRoutine());
        }
    }

    private IEnumerator ShowLockedFeedbackRoutine()
    {
        if (lockedFeedbackText != null)
            lockedFeedbackText.text = lockedMessage;

        if (lockedFeedbackContainer != null)
            lockedFeedbackContainer.SetActive(true);

        yield return new WaitForSeconds(lockedFeedbackDuration);

        if (lockedFeedbackContainer != null)
            lockedFeedbackContainer.SetActive(false);
    }

    private void OpenKeypad()
    {
        // Registrar este candado como el puzzle activo global
        GameManager.RegisterActivePuzzle(this);

        IsKeypadOpen = true;
        isProcessing = false;

        if (promptContainer != null)
            promptContainer.SetActive(false);

        // Desactivar cámara principal y activar la cámara dedicada del candado 3D
        if (mainPlayerCamera == null) mainPlayerCamera = Camera.main;
        if (mainPlayerCamera != null) mainPlayerCamera.gameObject.SetActive(false);
        if (doorKeypadCamera != null) doorKeypadCamera.gameObject.SetActive(true);

        if (keypadCanvas != null)
            keypadCanvas.SetActive(true);

        // Bloquear movimiento y vista del jugador
        SetPlayerControls(false);

        // Desbloquear cursor para hacer clic directamente en los botones 3D del candado
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Limpiar display digital
        currentInput = "";
        SetDisplayColor(normalColor);
        UpdateDisplay();

        Debug.Log("[DoorKeypad] Candado 3D abierto. Cámara dedicada activada.");
    }

    public void PressCloseButton()
    {
        if (!IsKeypadOpen) return;

        IsKeypadOpen = false;
        isProcessing = false;

        // Liberar el estado de puzzle activo en GameManager
        GameManager.UnregisterActivePuzzle(this);

        if (keypadCanvas != null)
            keypadCanvas.SetActive(false);

        // Restaurar cámaras: apagar cámara dedicada del candado y reactivar cámara del jugador
        if (doorKeypadCamera != null) doorKeypadCamera.gameObject.SetActive(false);
        if (mainPlayerCamera != null) mainPlayerCamera.gameObject.SetActive(true);

        if (!IsDoorOpened)
        {
            // Restaurar controles del jugador y bloquear cursor en el centro
            SetPlayerControls(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (isPlayerLooking && promptContainer != null)
            {
                promptContainer.SetActive(true);
            }
        }

        Debug.Log("[DoorKeypad] Candado 3D cerrado. Cámara y control devueltos al jugador.");
    }

    // =========================================================================
    // 2. MÉTODOS PARA BOTONES 3D O BOTONES UI (ONCLICK)
    // =========================================================================

    public void PressDigitButton(string digit)
    {
        if (isProcessing || IsDoorOpened || !IsKeypadOpen) return;

        if (string.IsNullOrEmpty(digit)) return;

        // Límite estricto de 4 caracteres
        if (currentInput.Length < maxDigits)
        {
            currentInput += digit;

            if (audioSource != null && buttonClickSound != null)
                audioSource.PlayOneShot(buttonClickSound);

            // Actualizar inmediatamente el cuadro de texto del candado en tiempo real
            UpdateDisplay();

            // Al digitar el 4to número, validar automáticamente si está bien o mal
            if (currentInput.Length >= maxDigits)
            {
                ValidateCode();
            }
        }
    }

    public void PressDigitButton(int digit)
    {
        PressDigitButton(digit.ToString());
    }

    public void PressClearButton()
    {
        if (isProcessing || IsDoorOpened || !IsKeypadOpen) return;

        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);

            if (audioSource != null && buttonClickSound != null)
                audioSource.PlayOneShot(buttonClickSound);

            UpdateDisplay();
        }
    }

    public void PressClearAllButton()
    {
        if (isProcessing || IsDoorOpened || !IsKeypadOpen) return;

        currentInput = "";
        if (audioSource != null && buttonClickSound != null)
            audioSource.PlayOneShot(buttonClickSound);

        UpdateDisplay();
    }

    public void PressSubmitButton()
    {
        if (isProcessing || IsDoorOpened || !IsKeypadOpen) return;

        if (string.IsNullOrEmpty(currentInput)) return;

        ValidateCode();
    }

    // =========================================================================
    // 3. VALIDACIÓN DEL CÓDIGO
    // =========================================================================

    private void ValidateCode()
    {
        isProcessing = true;

        string targetCode = "7392";
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.GetSecretCode()))
        {
            targetCode = GameManager.Instance.GetSecretCode().Trim();
        }

        bool isCorrect = string.Equals(currentInput.Trim(), targetCode, System.StringComparison.OrdinalIgnoreCase);

        if (isCorrect)
        {
            HandleSuccess();
        }
        else
        {
            HandleError();
        }
    }

    private void HandleSuccess()
    {
        Debug.Log("[DoorKeypad] ¡CÓDIGO CORRECTO! Abriendo puerta final.");

        SetDisplayColor(successColor);
        UpdateDisplay();

        if (audioSource != null && successSound != null)
            audioSource.PlayOneShot(successSound);

        if (ScreenFlash.Instance != null)
            ScreenFlash.Instance.FlashSuccess();

        StartCoroutine(SuccessRoutine());
    }

    private IEnumerator SuccessRoutine()
    {
        yield return new WaitForSeconds(0.6f);

        // Cerrar vista del candado y restaurar cámara
        PressCloseButton();

        // Abrir la puerta física y desencadenar la victoria
        OpenDoorAndTriggerVictory();
    }

    private void HandleError()
    {
        Debug.LogWarning("[DoorKeypad] Código incorrecto ingresado.");

        SetDisplayColor(errorColor);
        UpdateDisplay(); // Muestra los 4 números ingresados en rojo para feedback visual inmediato

        if (audioSource != null && errorSound != null)
            audioSource.PlayOneShot(errorSound);

        if (ScreenFlash.Instance != null)
            ScreenFlash.Instance.FlashError();

        // Limpiar automáticamente tras 0.8s para permitir intentar de nuevo sin botón de borrar
        StartCoroutine(ResetInputAfterDelay(0.8f));
    }

    private IEnumerator ResetInputAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        currentInput = "";
        SetDisplayColor(normalColor);
        UpdateDisplay();
        isProcessing = false;
    }

    private void OpenDoorAndTriggerVictory()
    {
        IsDoorOpened = true;
        GameManager.UnregisterActivePuzzle(this);

        // Desactivar collider físico de la puerta
        if (doorCollider != null)
            doorCollider.enabled = false;

        if (audioSource != null && doorOpenSound != null)
            audioSource.PlayOneShot(doorOpenSound);

        // Animación por Animator o rotación de bisagra Lerp
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger(openDoorTriggerName);
        }
        else if (doorHinge != null)
        {
            StartCoroutine(AnimateDoorHingeRoutine());
        }

        StartCoroutine(VictorySequenceRoutine());
    }

    private IEnumerator AnimateDoorHingeRoutine()
    {
        Quaternion startRot = doorHinge.localRotation;
        Quaternion targetRot = startRot * Quaternion.Euler(openRotationOffset);
        float elapsed = 0f;

        while (elapsed < doorOpenDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / doorOpenDuration);
            t = Mathf.SmoothStep(0f, 1f, t);
            doorHinge.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        doorHinge.localRotation = targetRot;
    }

    // =========================================================================
    // 4. PANTALLA DE VICTORIA Y TIEMPO DE ESCAPE
    // =========================================================================

    private IEnumerator VictorySequenceRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        // 1. Activar panel de victoria
        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        // 2. Calcular y formatear tiempo de escape (mm:ss)
        float totalSeconds = Time.timeSinceLevelLoad;
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        string formattedTime = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (victoryTimeText != null)
        {
            victoryTimeText.text = $"{victoryTimePrefix}{formattedTime}";
        }

        // 3. Notificar estado Victory a GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Victory);
        }

        // 4. Liberar cursor y mantener jugador bloqueado
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPlayerControls(false);

        Debug.Log($"[DoorKeypad] ¡ESCAPE COMPLETADO! Tiempo final: {formattedTime}");
    }

    // Métodos públicos para botones del Panel de Victoria
    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartLevel();
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToMainMenu();
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    public void QuitGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
            Application.Quit();
        }
    }

    // =========================================================================
    // AUXILIARES DE DISPLAY Y CONTROL DE JUGADOR
    // =========================================================================

    private void SetPlayerControls(bool state)
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = state;

        if (mouseLookScript != null)
            mouseLookScript.enabled = state;
    }

    private void SetDisplayColor(Color col)
    {
        if (displayText != null) displayText.color = col;
        if (displayText3D != null) displayText3D.color = col;
    }

    private void SetDisplayText(string text)
    {
        if (displayText != null) displayText.text = text;
        if (displayText3D != null) displayText3D.text = text;
    }

    private void UpdateDisplay()
    {
        if (string.IsNullOrEmpty(currentInput))
        {
            SetDisplayText(showPlaceholderDashes ? "- - - -" : "");
            return;
        }

        if (!showPlaceholderDashes)
        {
            SetDisplayText(currentInput);
            return;
        }

        // Mostrar los números que se van digitando en tiempo real con espacios y guiones para los pendientes
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < maxDigits; i++)
        {
            if (i < currentInput.Length)
            {
                sb.Append(currentInput[i]);
            }
            else
            {
                sb.Append('-');
            }

            if (i < maxDigits - 1)
            {
                sb.Append(' ');
            }
        }

        SetDisplayText(sb.ToString());
    }

    // --- INTERFAZ IINTERACTABLE ---
    public string GetHoverText() => IsDoorOpened ? string.Empty : hoverActionText;

    public void OnHandEnter()
    {
        if (IsDoorOpened || IsKeypadOpen) return;
        if (GameManager.IsAnyPuzzleActive && !GameManager.IsThisPuzzleActive(this)) return;

        isPlayerLooking = true;

        if (promptContainer != null)
            promptContainer.SetActive(true);

        if (keyIconImage != null)
            keyIconImage.gameObject.SetActive(true);

        if (actionDescriptionText != null)
            actionDescriptionText.text = hoverActionText;
    }

    public void OnHandExit()
    {
        isPlayerLooking = false;

        if (promptContainer != null)
            promptContainer.SetActive(false);
    }
}

