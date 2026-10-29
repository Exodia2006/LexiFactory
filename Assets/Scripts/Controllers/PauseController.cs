using UnityEngine;

public class PauseController : MonoBehaviour
{
    [SerializeField] private Level1Controller level1Controller;
    [SerializeField] private GameObject pauseMenuUI; 
    private bool isPaused = false;

    private void Start()
    {
        // Asegurar que el juego arranque en tiempo normal y el menú de pausa esté oculto
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
    }

    private void OnDisable()
    {
        // Asegurar que el tiempo se restablezca a 1f si la escena cambia estando en pausa
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // Si un puzzle ya usó esta pulsación de Escape para salir, no abrir la pausa en el mismo frame
        // (el orden de ejecución de los Update() entre scripts no está garantizado).
        if (GameManager.WasEscapeConsumedThisFrame) return;

        if (isPaused)
        {
            ResumeGame();
            return;
        }

        // Con un puzzle activo, Escape sirve para salir del puzzle, no para pausar
        if (GameManager.IsAnyPuzzleActive) return;

        PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
