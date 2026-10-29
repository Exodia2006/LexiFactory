using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public enum GameState
{
    MainMenu,
    Gameplay,
    Paused,
    GameOver,
    Victory
}

/// <summary>
/// Lead Gameplay Architecture:
/// Gestor de partida centralizado con resolución limpia de ciclo de vida de escenas,
/// prevención de singletons duplicados con preservación de referencias serializadas del nivel,
/// y gestión robusta de estados de puzzles sin fugas estáticas ni objetos zombis.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Persistence & Lifecycle")]
    [Tooltip("Si es falso, cada escena tiene su propio GameManager fresco y limpio. Si es verdadero, persiste entre escenas.")]
    [SerializeField] private bool persistAcrossScenes = false;

    [Header("Settings")]
    [SerializeField] private GameState initialMode = GameState.Gameplay;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "Game";

    public GameState CurrentState { get; private set; }
    public static event Action<GameState> OnStateChanged;
    public static event Action<bool> OnPauseToggled;

    [Header("Door Code Puzzle System")]
    [SerializeField] private TextMeshProUGUI codeDisplayText; // Arrastra aquí el texto de la UI que muestra "? ? ? ?"
    [SerializeField] private string secretCode = "7392";
    public string SecretCode => secretCode;
    private char[] currentDiscoveredCode = new char[] { '?', '?', '?', '?' };

    [Header("Level Timer & High Score System")]
    [Tooltip("Tiempo transcurrido en segundos en el nivel")]
    [SerializeField] private float elapsedTime = 0f;
    [Tooltip("Indica si el cronómetro global de la partida está activo")]
    [SerializeField] private bool isTimerRunning = false;
    [Tooltip("Indica si la partida actual superó el récord histórico previo")]
    [SerializeField] private bool isNewRecord = false;
    [Tooltip("Tiempo mínimo (segundos) considerado un récord válido. Valores menores guardados en PlayerPrefs se descartan como corruptos.")]
    [SerializeField] private float minValidRecordSeconds = 20f;

    private const string BEST_TIME_KEY = "BestEscapeTime";

    public float ElapsedTime => elapsedTime;
    public bool IsTimerRunning => isTimerRunning;
    public bool IsNewRecord => isNewRecord;

    // --- CONTROL DE CONCURRENCIA DE PUZZLES (EXCLUSIVIDAD Y SEGURIDAD ESTÁTICA) ---
    // Aceptamos 'object' para soportar interfaces como IInteractable y componentes MonoBehaviour.
    // Usamos 'is UnityEngine.Object unityObj && unityObj == null' para detectar si el objeto
    // fue destruido por Unity al cambiar de escena, purgando la referencia zombi automáticamente.
    public static object ActivePuzzle { get; private set; }

    public static bool IsAnyPuzzleActive
    {
        get
        {
            if (ActivePuzzle == null) return false;
            if (ActivePuzzle is UnityEngine.Object unityObj && unityObj == null)
            {
                ActivePuzzle = null;
                return false;
            }
            return true;
        }
    }

    public static bool RegisterActivePuzzle(object puzzle)
    {
        if (puzzle == null) return false;

        // Auto-limpiar referencia destruida por Unity si la escena anterior cambió
        if (ActivePuzzle is UnityEngine.Object currentObj && currentObj == null)
        {
            ActivePuzzle = null;
        }

        if (ActivePuzzle != null && ActivePuzzle != puzzle)
        {
            string newName = (puzzle as UnityEngine.Object)?.name ?? puzzle.GetType().Name;
            string currName = (ActivePuzzle as UnityEngine.Object)?.name ?? ActivePuzzle.GetType().Name;
            Debug.LogWarning($"[GameManager] Bloqueada activación de '{newName}' porque '{currName}' ya está activo.");
            return false;
        }

        ActivePuzzle = puzzle;
        return true;
    }

    public static void UnregisterActivePuzzle(object puzzle)
    {
        if (ActivePuzzle == null || (ActivePuzzle is UnityEngine.Object currentObj && currentObj == null))
        {
            ActivePuzzle = null;
            return;
        }

        if (ActivePuzzle == puzzle)
        {
            ActivePuzzle = null;
        }
    }

    public static bool IsThisPuzzleActive(object puzzle)
    {
        if (ActivePuzzle == null || (ActivePuzzle is UnityEngine.Object currentObj && currentObj == null))
        {
            ActivePuzzle = null;
            return false;
        }
        return ActivePuzzle == puzzle;
    }

    public static void ClearActivePuzzle()
    {
        ActivePuzzle = null;
    }

    // --- CONSUMO DE LA TECLA ESCAPE (EVITA QUE PUZZLE Y PAUSA LA PROCESEN EN EL MISMO FRAME) ---
    private static int escapeConsumedFrame = -1;

    /// <summary>
    /// Un puzzle debe llamar a este método cuando usa la tecla Escape para salir.
    /// Así PauseController ignora esa misma pulsación, sin importar el orden de los Update().
    /// </summary>
    public static void ConsumeEscape()
    {
        escapeConsumedFrame = Time.frameCount;
    }

    public static bool WasEscapeConsumedThisFrame => escapeConsumedFrame == Time.frameCount;

    /// <summary>Se dispara cada vez que cambia el código descubierto (revelar dígito o reset).</summary>
    public static event Action OnCodeChanged;

    private void Awake()
    {
        InitializeSingleton();
        CurrentState = initialMode;
    }

    private void Start()
    {
        // Asegurar que el código secreto no esté vacío
        if (string.IsNullOrEmpty(secretCode) || secretCode.Length < 4)
        {
            secretCode = "7392";
            Debug.LogWarning("[GameManager] secretCode estaba vacío o incompleto. Se reestableció por defecto a '7392'.");
        }

        UpdateCodeUI();

        // Iniciar cronómetro si la partida comienza en estado Gameplay
        if (CurrentState == GameState.Gameplay)
        {
            StartTimer();
        }

        // Sanitizar el estado de cámaras y cursores de la escena
        SanitizeSceneCameras();
    }

    private void Update()
    {
        // Si el temporizador está corriendo y el juego no está en pausa, victoria o game over, acumular tiempo
        if (isTimerRunning && CurrentState != GameState.Paused && CurrentState != GameState.Victory && CurrentState != GameState.GameOver && !Mathf.Approximately(Time.timeScale, 0f))
        {
            elapsedTime += Time.deltaTime;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            // Solución Arquitectónica al Conflicto de Singletons:
            // Si la nueva escena tiene su propio GameManager con referencias serializadas de nivel
            // (códigos, UI de escena, configuración fresca), destruimos la instancia anterior para
            // que esta nueva instancia con datos vivos tome el control.
            Debug.Log($"[GameManager] Detectada nueva instancia de GameManager en escena '{gameObject.scene.name}'. Reemplazando instancia previa para preservar referencias serializadas.");
            Destroy(Instance.gameObject);
        }

        Instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Restaurar Time.timeScale a 1f por seguridad ante pausas previas
        SetTimeScale(1f);

        // 2. Limpiar rigurosamente cualquier puzzle activo residual de la escena anterior
        ClearActivePuzzle();

        // 3. Resetear el código descubierto en la nueva escena
        ResetPuzzleCode();

        // 4. Re-enlazar UI y referencias de la escena activa
        RebindSceneReferences(scene);

        // 5. Configurar flujo según tipo de escena
        if (scene.name == mainMenuSceneName)
        {
            ChangeState(GameState.MainMenu);
            CancelTimer();
            ResetTimer();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            ChangeState(GameState.Gameplay);
            ResetTimer();
            StartTimer();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 6. Sanitizar cámaras de la escena activa
        SanitizeSceneCameras();
    }

    /// <summary>
    /// Re-enlaza automáticamente las referencias de la escena activa usando búsqueda defensiva.
    /// </summary>
    public void RebindSceneReferences(Scene scene)
    {
        // Si codeDisplayText es nulo o fue destruido con la escena anterior, buscarlo
        if (codeDisplayText == null || !codeDisplayText)
        {
            GameObject codeUIObj = GameObject.FindWithTag("CodeUI");
            if (codeUIObj != null)
            {
                codeDisplayText = codeUIObj.GetComponent<TextMeshProUGUI>();
            }

            if (codeDisplayText == null)
            {
                TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var txt in allTexts)
                {
                    string n = txt.gameObject.name.ToLowerInvariant();
                    if (n.Contains("code") || n.Contains("codigo") || n.Contains("displaytext"))
                    {
                        codeDisplayText = txt;
                        break;
                    }
                }
            }
        }

        UpdateCodeUI();
    }

    public void BindLevelData(TextMeshProUGUI customCodeText, string customSecretCode = null)
    {
        if (customCodeText != null)
        {
            codeDisplayText = customCodeText;
            UpdateCodeUI();
        }

        if (!string.IsNullOrEmpty(customSecretCode) && customSecretCode.Length >= 4)
        {
            secretCode = customSecretCode;
        }

        Debug.Log("[GameManager] Datos del nivel vinculados exitosamente.");
    }

    /// <summary>
    /// Limpia el estado de todas las cámaras al entrar/recargar la escena:
    /// apaga cámaras secundarias de puzzle y asegura que la cámara del jugador esté activa.
    /// </summary>
    public static void SanitizeSceneCameras()
    {
        // 1. Resetear gestor de transiciones suaves
        if (CameraTransitionManager.Instance != null)
        {
            CameraTransitionManager.Instance.ResetTransitionState();
        }

        // 2. Apagar cámaras de puzzles que pudiesen haber quedado activas en la jerarquía
        GenericPuzzleInteractable[] puzzles = FindObjectsByType<GenericPuzzleInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in puzzles)
        {
            if (p != null) p.EnsureCleanState();
        }

        DoorKeypadController[] keypads = FindObjectsByType<DoorKeypadController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var k in keypads)
        {
            if (k != null) k.EnsureCleanState();
        }

        RubikPuzzleInteractable[] rubiks = FindObjectsByType<RubikPuzzleInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var r in rubiks)
        {
            if (r != null) r.EnsureCleanState();
        }

        // 3. Asegurar que la cámara principal del jugador esté activa
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            MouseLook mouseLook = FindFirstObjectByType<MouseLook>();
            if (mouseLook != null)
            {
                mainCam = mouseLook.GetComponent<Camera>();
            }
        }

        if (mainCam != null && !mainCam.gameObject.activeSelf)
        {
            mainCam.gameObject.SetActive(true);
        }
    }

    // --- CONTROL DEL CRONÓMETRO GLOBAL Y RÉCORD HISTÓRICO (LEVEL TIMER & BEST TIME) ---

    public void StartTimer()
    {
        isTimerRunning = true;
        isNewRecord = false;
        Debug.Log($"[GameManager] Cronómetro global iniciado. Tiempo actual: {GetCurrentFormattedTime()} | Récord vigente: {GetBestFormattedTime()}");
    }

    /// <summary>
    /// Finaliza la partida (victoria): detiene el cronÃ³metro y evalÃºa el rÃ©cord histÃ³rico.
    /// Solo registra rÃ©cord si el nivel se completÃ³ de verdad (todos los cÃ³digos descubiertos).
    /// </summary>
    public void StopTimer()
    {
        if (!isTimerRunning) return;

        isTimerRunning = false;

        if (AreAllPuzzlesSolved())
        {
            CheckAndUpdateRecord();
        }
        else
        {
            isNewRecord = false;
            Debug.LogWarning("[GameManager] CronÃ³metro detenido sin completar todos los puzzles. No se registra rÃ©cord.");
        }

        Debug.Log($"[GameManager] CronÃ³metro global detenido en: {GetCurrentFormattedTime()} ({elapsedTime:F2}s) | RÃ©cord: {GetBestFormattedTime()} | Â¿Nuevo rÃ©cord?: {isNewRecord}");
    }

    /// <summary>
    /// Detiene el cronÃ³metro SIN evaluar rÃ©cord. Ãšsalo al reiniciar, salir al menÃº o abandonar la partida.
    /// </summary>
    public void CancelTimer()
    {
        isTimerRunning = false;
        isNewRecord = false;
    }

    /// <summary>
    /// Lee el rÃ©cord guardado. Si el valor es invÃ¡lido o imposiblemente bajo (corrupto por pruebas anteriores),
    /// lo elimina y devuelve -1.
    /// </summary>
    private float GetStoredBestTime()
    {
        if (!PlayerPrefs.HasKey(BEST_TIME_KEY)) return -1f;

        float stored = PlayerPrefs.GetFloat(BEST_TIME_KEY);
        if (stored < minValidRecordSeconds)
        {
            Debug.LogWarning($"[GameManager] RÃ©cord guardado invÃ¡lido ({stored:F2}s < {minValidRecordSeconds:F0}s). Se descarta.");
            PlayerPrefs.DeleteKey(BEST_TIME_KEY);
            PlayerPrefs.Save();
            return -1f;
        }

        return stored;
    }

    private void CheckAndUpdateRecord()
    {
        // Un tiempo menor al mÃ­nimo vÃ¡lido no puede ser una partida real: no se guarda
        if (elapsedTime < minValidRecordSeconds)
        {
            isNewRecord = false;
            Debug.LogWarning($"[GameManager] Tiempo {elapsedTime:F2}s inferior al mÃ­nimo vÃ¡lido ({minValidRecordSeconds:F0}s). No se registra rÃ©cord.");
            return;
        }

        float bestTime = GetStoredBestTime();

        if (bestTime < 0f)
        {
            // Primer rÃ©cord registrado
            PlayerPrefs.SetFloat(BEST_TIME_KEY, elapsedTime);
            PlayerPrefs.Save();
            isNewRecord = true;
            Debug.Log($"[GameManager] Â¡Primer rÃ©cord histÃ³rico registrado! Tiempo: {GetCurrentFormattedTime()}");
        }
        else if (elapsedTime < bestTime)
        {
            // RÃ©cord superado
            PlayerPrefs.SetFloat(BEST_TIME_KEY, elapsedTime);
            PlayerPrefs.Save();
            isNewRecord = true;
            Debug.Log($"[GameManager] Â¡NUEVO RÃ‰CORD HISTÃ“RICO! Anterior: {FormatTime(bestTime)} | Nuevo: {GetCurrentFormattedTime()}");
        }
        else
        {
            isNewRecord = false;
            Debug.Log($"[GameManager] Partida finalizada en {GetCurrentFormattedTime()}. No supera el rÃ©cord vigente de {FormatTime(bestTime)}.");
        }
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        isTimerRunning = false;
        isNewRecord = false;
        Debug.Log("[GameManager] CronÃ³metro global reiniciado a 00:00.");
    }

    public string GetCurrentFormattedTime()
    {
        return FormatTime(elapsedTime);
    }

    public string GetFormattedElapsedTime()
    {
        return GetCurrentFormattedTime();
    }

    public string GetBestFormattedTime()
    {
        float best = GetStoredBestTime();
        return best >= 0f ? FormatTime(best) : "--:--";
    }

    public float GetBestTime()
    {
        return GetStoredBestTime();
    }

    public bool HasNewRecordAchieved()
    {
        return isNewRecord;
    }

    [ContextMenu("Reset Best Time (PlayerPrefs)")]
    public void ResetBestTime()
    {
        PlayerPrefs.DeleteKey(BEST_TIME_KEY);
        PlayerPrefs.Save();
        isNewRecord = false;
        Debug.Log("[GameManager] Récord histórico ('BestEscapeTime') eliminado de PlayerPrefs.");
    }

    public static string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds < 0f) timeInSeconds = 0f;

        int totalSeconds = Mathf.FloorToInt(timeInSeconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        if (hours > 0)
        {
            return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
        }
        else
        {
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    // --- LÓGICA DE REVELACIÓN DE CÓDIGO ---
    public void RevealDigit(int index)
    {
        if (index >= 0 && index < currentDiscoveredCode.Length && index < secretCode.Length)
        {
            currentDiscoveredCode[index] = secretCode[index];
            Debug.Log($"[GameManager] ¡Dígito revelado en índice {index}! Número: '{secretCode[index]}'. Código actual: {new string(currentDiscoveredCode)}");

            UpdateCodeUI();
            OnCodeChanged?.Invoke();
            CheckVictoryCondition();
        }
        else
        {
            Debug.LogError($"[GameManager] Índice fuera de rango al intentar revelar dígito: {index}");
        }
    }

    /// <summary>
    /// Devuelve el progreso actual del código (ej: "7 ? 9 2") para mostrarlo en cualquier UI/objeto 3D.
    /// </summary>
    public string GetDiscoveredCodeString(string separator = " ")
    {
        if (currentDiscoveredCode == null || currentDiscoveredCode.Length == 0) return string.Empty;
        return string.Join(separator, currentDiscoveredCode);
    }

    public char GetSecretDigitChar(int index)
    {
        if (index >= 0 && index < secretCode.Length)
        {
            return secretCode[index];
        }
        return '?';
    }

    public void ResetPuzzleCode()
    {
        currentDiscoveredCode = new char[] { '?', '?', '?', '?' };
        OnCodeChanged?.Invoke();
        UpdateCodeUI();
    }

    private void UpdateCodeUI()
    {
        if (codeDisplayText != null && codeDisplayText)
        {
            codeDisplayText.text = $"{currentDiscoveredCode[0]} {currentDiscoveredCode[1]} {currentDiscoveredCode[2]} {currentDiscoveredCode[3]}";

            Transform current = codeDisplayText.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }

            codeDisplayText.ForceMeshUpdate();
        }
    }

    private void CheckVictoryCondition()
    {
        if (AreAllPuzzlesSolved())
        {
            Debug.Log("[GameManager] ¡Todos los acertijos resueltos! El teclado de la puerta final ha sido habilitado.");
        }
    }

    public bool AreAllPuzzlesSolved()
    {
        if (currentDiscoveredCode == null || currentDiscoveredCode.Length == 0) return false;
        for (int i = 0; i < currentDiscoveredCode.Length; i++)
        {
            if (currentDiscoveredCode[i] == '?')
                return false;
        }
        return true;
    }

    public string GetSecretCode() => secretCode;

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void GameStart()
    {
        SetTimeScale(1f);
        ClearActivePuzzle();
        ResetPuzzleCode();
        ResetTimer();
        ChangeState(GameState.Gameplay);
        StartTimer();
        SceneManager.LoadScene(gameSceneName);
    }

    public void GoToMainMenu()
    {
        SetTimeScale(1f);
        CancelTimer();
        ResetTimer();
        ClearActivePuzzle();
        ChangeState(GameState.MainMenu);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    public void RestartLevel()
    {
        SetTimeScale(1f);
        CancelTimer();
        ResetTimer();
        ClearActivePuzzle();
        ResetPuzzleCode();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }
}
