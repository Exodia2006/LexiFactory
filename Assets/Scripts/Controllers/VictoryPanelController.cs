using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;

/// <summary>
/// Lead Gameplay Architecture:
/// Controlador modular para el panel de victoria con soporte de tiempo actual y mejor tiempo histórico (High Score).
/// </summary>
public class VictoryPanelController : MonoBehaviour
{
    [Header("Referencias Visuales")]
    [Tooltip("Objeto raíz del panel de victoria (si está vacío, usa este mismo GameObject)")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("Texto TMP para proyectar el tiempo demorado en la partida actual")]
    [FormerlySerializedAs("victoryTimeText")]
    [SerializeField] private TextMeshProUGUI currentTimeText;

    [Tooltip("Texto TMP para proyectar el mejor tiempo histórico")]
    [SerializeField] private TextMeshProUGUI bestTimeText;

    [Tooltip("Insignia / icono / texto opcional que se activa únicamente si se batió el récord")]
    [SerializeField] private GameObject newRecordBadge;

    [Header("Botones de Navegación")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }
    }

    private void Start()
    {
        // Conectar botones con callbacks limpios
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    /// <summary>
    /// Despliega el panel de victoria, detiene el cronómetro y proyecta el tiempo actual y el récord histórico.
    /// </summary>
    public void ShowVictoryPanel()
    {
        // 1. Detener cronómetro global inmediatamente
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StopTimer();
        }

        // 2. Activar panel de victoria
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        // 3. Obtener tiempos formateados y verificar si hay nuevo récord
        string curTime = GameManager.Instance != null
            ? GameManager.Instance.GetCurrentFormattedTime()
            : GameManager.FormatTime(Time.timeSinceLevelLoad);

        string bestTime = GameManager.Instance != null
            ? GameManager.Instance.GetBestFormattedTime()
            : curTime;

        bool isNewRecord = GameManager.Instance != null && GameManager.Instance.HasNewRecordAchieved();

        // 4. Asignar texto de tiempo actual
        if (currentTimeText != null)
        {
            currentTimeText.text = curTime; // Solo el número: las etiquetas ya existen en el Canvas
        }
        else
        {
            Debug.LogWarning("[VictoryPanelController] 'currentTimeText' no está asignado en el Inspector.");
        }

        // 5. Asignar texto de mejor tiempo histórico
        if (bestTimeText != null)
        {
            bestTimeText.text = bestTime; // Solo el número: las etiquetas ya existen en el Canvas
        }
        else
        {
            Debug.LogWarning("[VictoryPanelController] 'bestTimeText' no está asignado en el Inspector.");
        }

        // 6. Activar o desactivar insignia de nuevo récord
        if (newRecordBadge != null)
        {
            newRecordBadge.SetActive(isNewRecord);
        }

        // 7. Notificar estado Victory a GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Victory);
        }

        // 8. Liberar cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log($"[VictoryPanelController] ¡Victoria mostrada! Tiempo actual: {curTime} | Mejor tiempo: {bestTime} | ¿Nuevo récord?: {isNewRecord}");
    }

    /// <summary>
    /// Menú contextual para resetear el récord guardado en PlayerPrefs durante pruebas.
    /// </summary>
    [ContextMenu("Reset Best Time (PlayerPrefs)")]
    public void ResetBestTime()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetBestTime();
        }
        else
        {
            PlayerPrefs.DeleteKey("BestEscapeTime");
            PlayerPrefs.Save();
            Debug.Log("[VictoryPanelController] PlayerPrefs 'BestEscapeTime' eliminado.");
        }
    }

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
}
