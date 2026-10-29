using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    [Header("Sistema de Vidas / Errores")]
    [SerializeField] private int maxErrors = 3;
    [SerializeField] private Image[] errorCrossImages; // 3 'X' del panel de errores
    private int currentErrors = 0;

    [Header("Efecto Máquina de Escribir")]
    [SerializeField] private float typingSpeed = 0.03f;
    private Coroutine typingCoroutine;

    [Header("Referencias de Interacción")]
    [SerializeField] private GenericPuzzleInteractable puzzleInteractable;

    [Header("Lista de Preguntas")]
    [SerializeField] private List<WrittenQuestion> questions = new List<WrittenQuestion>();
    private List<int> availableQuestionIndices = new List<int>();
    private int currentQuestionIndex = 0;
    private int completedQuestionsCount = 0;

    [Header("Audio & Flash")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip submitSound;
    [SerializeField] private AudioClip correctAnswerSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;

    [Header("GameManager Code Index")]
    [SerializeField] private int codeDigitIndex = 3; // 4to dígito del código ("7392" -> índice 3 es '2')

    public bool IsActive { get; private set; } = false;

    private void Start()
    {
        if (deskCanvas != null) deskCanvas.SetActive(false);

        ResetErrorCrosses();

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
        GameManager.RegisterActivePuzzle(this);
        IsActive = true;
        ResetPuzzleState();

        if (deskCanvas != null) deskCanvas.SetActive(true);

        if (availableQuestionIndices.Count > 0)
        {
            currentQuestionIndex = availableQuestionIndices[0];
            DisplayCurrentQuestion();
        }
    }

    public void StopPuzzle()
    {
        IsActive = false;
        GameManager.UnregisterActivePuzzle(this);

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (deskCanvas != null) deskCanvas.SetActive(false);
    }

    private void OnDisable()
    {
        StopPuzzle();
    }

    private void OnDestroy()
    {
        StopPuzzle();
    }

    private void ResetPuzzleState()
    {
        ResetErrorCrosses();
        completedQuestionsCount = 0;
        InitializeAvailableQuestions();
    }

    private void ResetErrorCrosses()
    {
        currentErrors = 0;
        if (errorCrossImages != null)
        {
            for (int i = 0; i < errorCrossImages.Length; i++)
            {
                if (errorCrossImages[i] != null)
                {
                    errorCrossImages[i].gameObject.SetActive(false);
                }
            }
        }
    }

    private void InitializeAvailableQuestions()
    {
        availableQuestionIndices.Clear();
        for (int i = 0; i < questions.Count; i++)
        {
            availableQuestionIndices.Add(i);
        }

        // Barajar preguntas disponibles (Fisher-Yates shuffle)
        for (int i = availableQuestionIndices.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = availableQuestionIndices[i];
            availableQuestionIndices[i] = availableQuestionIndices[randomIndex];
            availableQuestionIndices[randomIndex] = temp;
        }
    }

    private void DisplayCurrentQuestion()
    {
        if (availableQuestionIndices == null || availableQuestionIndices.Count == 0) return;
        if (currentQuestionIndex < 0 || currentQuestionIndex >= questions.Count) return;

        var q = questions[currentQuestionIndex];

        if (progressTextUI != null)
            progressTextUI.text = $"Pregunta {completedQuestionsCount + 1}/{questions.Count}";

        if (answerInputField != null)
        {
            answerInputField.text = "";
            answerInputField.interactable = true;
            answerInputField.Select();
            answerInputField.ActivateInputField(); // Abrir el foco directamente en la caja de texto
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Para el Escritorio: aplica el efecto de máquina de escribir solo al texto de la pregunta
        typingCoroutine = StartCoroutine(TypewriterRoutine(q.questionText));
    }

    private IEnumerator TypewriterRoutine(string textToType)
    {
        if (questionTextUI != null) questionTextUI.text = "";

        if (questionTextUI != null && !string.IsNullOrEmpty(textToType))
        {
            for (int i = 0; i < textToType.Length; i++)
            {
                questionTextUI.text += textToType[i];
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        typingCoroutine = null;
    }

    public void SubmitAnswer()
    {
        if (!IsActive) return;

        if (answerInputField == null) return;

        string userText = answerInputField.text.Trim();
        if (string.IsNullOrEmpty(userText)) return; // Ignorar si presiona Enter estando vacío

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (audioSource != null && submitSound != null)
            audioSource.PlayOneShot(submitSound);

        var q = questions[currentQuestionIndex];

        // Comparación insensible a mayúsculas/minúsculas y espacios innecesarios
        if (string.Equals(userText, q.correctAnswer.Trim(), System.StringComparison.OrdinalIgnoreCase))
        {
            completedQuestionsCount++;

            if (availableQuestionIndices.Count > 0)
            {
                availableQuestionIndices.RemoveAt(0);
            }

            // Feedback positivo inmediato (destello verde + sonido de acierto)
            if (ScreenFlash.Instance != null)
                ScreenFlash.Instance.FlashSuccess();

            AudioClip soundToPlay = correctAnswerSound != null ? correctAnswerSound : successSound;
            if (audioSource != null && soundToPlay != null)
                audioSource.PlayOneShot(soundToPlay);

            if (completedQuestionsCount >= questions.Count || availableQuestionIndices.Count == 0)
            {
                WinPuzzle();
            }
            else
            {
                if (answerInputField != null)
                    answerInputField.interactable = false;

                StartCoroutine(NextQuestionAfterSuccessRoutine());
            }
        }
        else
        {
            HandleWrongAnswer();
        }
    }

    private IEnumerator NextQuestionAfterSuccessRoutine()
    {
        IsActive = false;
        yield return new WaitForSeconds(0.6f);
        IsActive = true;

        if (availableQuestionIndices.Count > 0)
        {
            currentQuestionIndex = availableQuestionIndices[0];
            DisplayCurrentQuestion();
        }
    }

    private void HandleWrongAnswer()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // Activa la 'X' correspondiente antes de incrementar currentErrors
        if (errorCrossImages != null && currentErrors >= 0 && currentErrors < errorCrossImages.Length)
        {
            if (errorCrossImages[currentErrors] != null)
            {
                errorCrossImages[currentErrors].gameObject.SetActive(true);
            }
        }

        currentErrors++;

        if (currentErrors >= maxErrors)
        {
            FailPuzzle();
        }
        else
        {
            IsActive = false;

            if (answerInputField != null)
            {
                answerInputField.interactable = false;
            }

            if (ScreenFlash.Instance != null) ScreenFlash.Instance.FlashError();
            if (audioSource != null && errorSound != null) audioSource.PlayOneShot(errorSound);

            // Dejar la pregunta actual para después y saltar a otra disponible
            if (availableQuestionIndices.Count > 1)
            {
                int failedIndex = availableQuestionIndices[0];
                availableQuestionIndices.RemoveAt(0);
                availableQuestionIndices.Add(failedIndex);
            }

            StartCoroutine(NextQuestionAfterErrorRoutine());
        }
    }

    private IEnumerator NextQuestionAfterErrorRoutine()
    {
        yield return new WaitForSeconds(0.8f);
        IsActive = true;

        if (availableQuestionIndices.Count > 0)
        {
            currentQuestionIndex = availableQuestionIndices[0];
            DisplayCurrentQuestion();
        }
    }

    private void WinPuzzle()
    {
        IsActive = false;
        GameManager.UnregisterActivePuzzle(this);
        Debug.Log("¡PUZZLE DEL ESCRITORIO RESUELTO!");

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (ScreenFlash.Instance != null) ScreenFlash.Instance.FlashSuccess();
        if (audioSource != null && successSound != null) audioSource.PlayOneShot(successSound);

        if (GameManager.Instance != null)
            GameManager.Instance.RevealDigit(codeDigitIndex);

        if (deskCanvas != null) deskCanvas.SetActive(false);

        // Marcar como resuelto y devolver la cámara al jugador mediante referencia directa
        GenericPuzzleInteractable interactable = puzzleInteractable != null ? puzzleInteractable : GetComponentInParent<GenericPuzzleInteractable>();
        if (interactable == null) interactable = GetComponent<GenericPuzzleInteractable>();

        if (interactable != null)
        {
            interactable.MarkAsSolved();
            interactable.ExitPuzzle();
        }
    }

    private void FailPuzzle()
    {
        IsActive = false;
        GameManager.UnregisterActivePuzzle(this);
        Debug.LogWarning("¡Límite de 3 errores alcanzado en el Escritorio! Reiniciando o saliendo...");

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (ScreenFlash.Instance != null) ScreenFlash.Instance.FlashError();
        if (audioSource != null && errorSound != null) audioSource.PlayOneShot(errorSound);

        StartCoroutine(FailPuzzleRoutine());
    }

    private IEnumerator FailPuzzleRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        ResetPuzzleState();

        if (deskCanvas != null) deskCanvas.SetActive(false);

        GenericPuzzleInteractable interactable = puzzleInteractable != null ? puzzleInteractable : GetComponentInParent<GenericPuzzleInteractable>();
        if (interactable == null) interactable = GetComponent<GenericPuzzleInteractable>();

        if (interactable != null)
        {
            interactable.ExitPuzzle();
        }
        else
        {
            StartPuzzle();
        }
    }
}