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

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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
    private char[] currentDiscoveredCode = new char[] { '?', '?', '?', '?' };

    private void Awake()
    {
        InitializeSingleton();
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
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Si al cambiar de escena el texto de la UI se pierde, puedes buscarlo nuevamente
        if (codeDisplayText == null)
        {
            GameObject codeUIObj = GameObject.FindWithTag("CodeUI");
            if (codeUIObj != null)
            {
                codeDisplayText = codeUIObj.GetComponent<TextMeshProUGUI>();
            }
        }
        UpdateCodeUI();
    }

    // --- LÓGICA DE REVELACIÓN DE CÓDIGO ---
    public void RevealDigit(int index)
    {
        if (index >= 0 && index < currentDiscoveredCode.Length && index < secretCode.Length)
        {
            currentDiscoveredCode[index] = secretCode[index];
            Debug.Log($"[GameManager] ¡Dígito revelado en índice {index}! Número: '{secretCode[index]}'. Código actual: {new string(currentDiscoveredCode)}");

            UpdateCodeUI();
            CheckVictoryCondition();
        }
        else
        {
            Debug.LogError($"[GameManager] Índice fuera de rango al intentar revelar dígito: {index}");
        }
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
        UpdateCodeUI();
    }

    private void UpdateCodeUI()
    {
        if (codeDisplayText != null)
        {
            // 1. Asignar el texto actualizado
            codeDisplayText.text = $"{currentDiscoveredCode[0]} {currentDiscoveredCode[1]} {currentDiscoveredCode[2]} {currentDiscoveredCode[3]}";

            // 2. Activar el texto y TODOS sus objetos padres hacia arriba
            Transform current = codeDisplayText.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                    Debug.Log($"[GameManager] Se activó el objeto padre: '{current.name}'");
                }
                current = current.parent;
            }

            // 3. Forzar actualización visual
            codeDisplayText.ForceMeshUpdate();
        }
    }

    private void CheckVictoryCondition()
    {
        string current = new string(currentDiscoveredCode);
        if (current == secretCode)
        {
            Debug.Log("¡Todos los acertijos resueltos! Puerta desbloqueada.");
            ChangeState(GameState.Victory);
        }
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void GameStart()
    {
        SetTimeScale(1f);
        ResetPuzzleCode();
        ChangeState(GameState.Gameplay);
        SceneManager.LoadScene(gameSceneName);
    }

    public void GoToMainMenu()
    {
        SetTimeScale(1f);
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
        ResetPuzzleCode();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }
}