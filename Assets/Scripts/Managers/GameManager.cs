using System;
using UnityEngine;

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

    private void Awake()
    {
        InitializeSingleton();
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