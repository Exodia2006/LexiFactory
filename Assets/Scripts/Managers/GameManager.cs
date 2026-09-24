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
    [SerializeField] private TextMeshProUGUI codeDisplayText;
    [SerializeField] private string secretCode = "7392";
    private char[] currentDiscoveredCode = new char[] { '?', '?', '?', '?' };

    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        UpdateCodeUI();
    }

    private void OnEnable()
    {
        // Suscribirse al evento de cambio de escena para restaurar referencias de UI si es necesario
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
        // Al cargar la escena de juego, buscar el texto de la UI si la referencia se perdió
        if (scene.name == gameSceneName)
        {
            if (codeDisplayText == null)
            {
                GameObject textObj = GameObject.FindWithTag("CodeUI"); // Opcional: asigna la etiqueta "CodeUI" al texto en la escena
                if (textObj != null)
                {
                    codeDisplayText = textObj.GetComponent<TextMeshProUGUI>();
                }
            }
            UpdateCodeUI();
        }
    }

    // --- LÓGICA DE REVELACIÓN DE CÓDIGO ---
    public void RevealDigit(int index)
    {
        if (index >= 0 && index < 4)
        {
            currentDiscoveredCode[index] = secretCode[index];
            UpdateCodeUI();
            CheckVictoryCondition();
        }
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
            codeDisplayText.text = $"{currentDiscoveredCode[0]} {currentDiscoveredCode[1]} {currentDiscoveredCode[2]} {currentDiscoveredCode[3]}";
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
        ResetPuzzleCode(); // Limpiar el progreso previo
        ChangeState(GameState.Gameplay);
        SceneManager.LoadScene(gameSceneName);
    }

    public void GoToMainMenu()
    {
        SetTimeScale(1f);
        ChangeState(GameState.MainMenu);

        // Asegurar visibilidad del cursor en el menú principal
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