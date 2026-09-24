using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void OnPlayButtonPressed()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameStart();
        }
        else
        {
            // Respaldo por si se inicia la prueba directamente en el MainMenu sin GameManager
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        }
    }

    public void OnQuitButtonPressed()
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
}