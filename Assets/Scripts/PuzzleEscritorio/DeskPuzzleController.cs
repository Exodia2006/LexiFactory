using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class WrittenQuestion
{
    [TextArea(2, 4)]
    public string questionText;
    public string correctAnswer; // Respuesta esperada
}

public class DeskPuzzleController : MonoBehaviour
{
    [Header("UI del Escritorio")]
    [SerializeField] private GameObject deskCanvas;
    [SerializeField] private TextMeshProUGUI questionTextUI;
    [SerializeField] private TextMeshProUGUI progressTextUI;
    [SerializeField] private TMP_InputField answerInputField;

    [Header("Referencias de Interacción")]
    [SerializeField] private GenericPuzzleInteractable puzzleInteractable;

    [Header("Lista de Preguntas")]
    [SerializeField] private List<WrittenQuestion> questions = new List<WrittenQuestion>();
    private int currentQuestionIndex = 0;

    [Header("Audio & Flash")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip submitSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;

    [Header("GameManager Code Index")]
    [SerializeField] private int codeDigitIndex = 3; // 4to dígito del código ("7392" -> índice 3 es '2')

    public bool IsActive { get; private set; } = false;

    private void Start()
    {
        if (deskCanvas != null) deskCanvas.SetActive(false);

        // Permitir presionar Enter para enviar la respuesta
        if (answerInputField != null)
        {
            answerInputField.onSubmit.RemoveAllListeners();
            answerInputField.onSubmit.AddListener(delegate { SubmitAnswer(); });
        }

        if (deskCanvas != null) deskCanvas.SetActive(false);

        if (answerInputField != null)
        {
            // Forzar Raycast Target en todos los elementos gráficos dentro del InputField
            UnityEngine.UI.Graphic[] childGraphics = answerInputField.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            foreach (var graphic in childGraphics)
            {
                graphic.raycastTarget = true;
            }

            answerInputField.onSubmit.RemoveAllListeners();
            answerInputField.onSubmit.AddListener(delegate { SubmitAnswer(); });
        }
    }

    public void StartPuzzle()
    {
        IsActive = true;
        currentQuestionIndex = 0;
        if (deskCanvas != null) deskCanvas.SetActive(true);
        DisplayCurrentQuestion();
    }

    public void StopPuzzle()
    {
        IsActive = false;
        if (deskCanvas != null) deskCanvas.SetActive(false);
    }

    private void DisplayCurrentQuestion()
    {
        if (currentQuestionIndex >= questions.Count) return;

        var q = questions[currentQuestionIndex];
        if (questionTextUI != null) questionTextUI.text = q.questionText;
        if (progressTextUI != null) progressTextUI.text = $"Pregunta {currentQuestionIndex + 1}/{questions.Count}";

        if (answerInputField != null)
        {
            answerInputField.text = "";

            // Asignar e interactuar directamente
            answerInputField.interactable = true;
            answerInputField.Select();
            answerInputField.ActivateInputField(); // Abrir el foco directamente en la caja de texto
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SubmitAnswer()
    {
        if (!IsActive) return;

        if (answerInputField == null) return;

        string userText = answerInputField.text.Trim();
        if (string.IsNullOrEmpty(userText)) return; // Ignorar si presiona Enter estando vacío

        if (audioSource != null && submitSound != null)
            audioSource.PlayOneShot(submitSound);

        var q = questions[currentQuestionIndex];

        // Comparación insensible a mayúsculas/minúsculas y espacios innecesarios
        if (string.Equals(userText, q.correctAnswer.Trim(), System.StringComparison.OrdinalIgnoreCase))
        {
            currentQuestionIndex++;

            if (currentQuestionIndex >= questions.Count)
            {
                WinPuzzle();
            }
            else
            {
                DisplayCurrentQuestion();
            }
        }
        else
        {
            FailPuzzle();
        }
    }

    private void WinPuzzle()
    {
        IsActive = false;
        Debug.Log("¡PUZZLE DEL ESCRITORIO RESUELTO!");

        if (ScreenFlash.Instance != null) ScreenFlash.Instance.FlashSuccess();
        if (audioSource != null && successSound != null) audioSource.PlayOneShot(successSound);

        if (GameManager.Instance != null)
            GameManager.Instance.RevealDigit(codeDigitIndex);

        if (deskCanvas != null) deskCanvas.SetActive(false);

        // Devolver la cámara al jugador mediante referencia directa
        if (puzzleInteractable != null)
        {
            puzzleInteractable.ExitPuzzle();
        }
        else
        {
            GenericPuzzleInteractable interactable = GetComponentInParent<GenericPuzzleInteractable>();
            if (interactable != null) interactable.ExitPuzzle();
        }
    }

    private void FailPuzzle()
    {
        IsActive = false;
        Debug.LogWarning("¡Respuesta escrita incorrecta! Reiniciando...");

        if (ScreenFlash.Instance != null) ScreenFlash.Instance.FlashError();
        if (audioSource != null && errorSound != null) audioSource.PlayOneShot(errorSound);

        StartCoroutine(ResetPuzzleWithDelay());
    }

    private IEnumerator ResetPuzzleWithDelay()
    {
        yield return new WaitForSeconds(0.8f);
        currentQuestionIndex = 0;
        IsActive = true;
        DisplayCurrentQuestion();
    }


}