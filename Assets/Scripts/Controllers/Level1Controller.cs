using UnityEngine;
using UnityEngine.SceneManagement;

public class Level1Controller : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu"; 

   
    public void GoToMainMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetTimeScale(1f);
        }
        else
        {
            Time.timeScale = 1f;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
