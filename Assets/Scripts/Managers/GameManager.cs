using System;
using UnityEngine;
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
    public GameState CurrentState { get; private set; }
    public static event Action<GameState> OnStateChanged;
    public static event Action<bool> OnPauseToggled;

    [Header("Door Code Puzzle System")]
    [SerializeField] private TextMeshProUGUI codeDisplayText;
    [SerializeField] private string secretCode = "7392"; // Tu código secreto de 4 dígitos
    private char[] currentDiscoveredCode = new char[] { '?', '?', '?', '?' };

    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        UpdateCodeUI();
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
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }

    public void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    public void RestartLevel()
    {
        SetTimeScale(1f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }


}