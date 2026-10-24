using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class MultipleChoiceQuestion
{
    [TextArea(2, 4)]
    public string questionText;
    public string[] options = new string[4]; // A, B, C, D
    public int correctOptionIndex; // 0 para A, 1 para B, 2 para C, 3 para D
}

public class TVPuzzleController : MonoBehaviour
{
    [Header("UI del TV")]
    [SerializeField] private GameObject tvCanvas;
    [SerializeField] private TextMeshProUGUI questionTextUI;
    [SerializeField] private TextMeshProUGUI progressTextUI;
    [SerializeField] private Button[] optionButtons; // 4 Botones (A, B, C, D)
    [SerializeField] private TextMeshProUGUI[] optionTexts; // Textos dentro de los 4 botones

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
    [SerializeField] private List<MultipleChoiceQuestion> questions = new List<MultipleChoiceQuestion>();
    private List<int> availableQuestionIndices = new List<int>();
    private int currentQuestionIndex = 0;
    private int completedQuestionsCount = 0;

    [Header("Audio & Flash")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioClip correctAnswerSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;

    [Header("GameManager Code Index")]
    [SerializeField] private int codeDigitIndex = 2; // Índice 2 = 3er dígito ("7392" -> índice 2 es '9')

    public bool IsActive { get; private set; } = false;

    private void Start()
    {
        if (tvCanvas != null) tvCanvas.SetActive(false);

        ResetErrorCrosses();

        // Asignar eventos a los 4 botones automáticamente
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            if (optionButtons[i] != null)
            {
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
            }
        }
    }

    public void StartPuzzle()
    {
        GameManager.RegisterActivePuzzle(this);
        IsActive = true;
        ResetPuzzleState();

        if (tvCanvas != null) tvCanvas.SetActive(true);

        if (availableQuestionIndices.Count > 0)
        {
            currentQuestionIndex = availableQuestionIndices[0];
            DisplayCurrentQuestion();
        }
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

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypewriterRoutine(q));
    }

    private IEnumerator TypewriterRoutine(MultipleChoiceQuestion q)
    {
        // Limpiar textos y preparar botones
        if (questionTextUI != null) questionTextUI.text = "";

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                if (i < q.options.Length)
                {
                    if (optionTexts[i] != null) optionTexts[i].text = "";
                    optionButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        // Efecto máquina de escribir para la pregunta
        if (questionTextUI != null && !string.IsNullOrEmpty(q.questionText))
        {
            for (int i = 0; i < q.questionText.Length; i++)
            {
                questionTextUI.text += q.questionText[i];
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        // Efecto máquina de escribir para las opciones
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.options.Length && optionTexts[i] != null && !string.IsNullOrEmpty(q.options[i]))
            {
                string optionStr = q.options[i];
                for (int j = 0; j < optionStr.Length; j++)
                {
                    optionTexts[i].text += optionStr[j];
                    yield return new WaitForSeconds(typingSpeed);
                }
            }
        }

        typingCoroutine = null;
    }

    public void OnOptionSelected(int selectedIndex)
    {
        if (!IsActive) return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (audioSource != null && selectSound != null)
            audioSource.PlayOneShot(selectSound);

        var q = questions[currentQuestionIndex];

        // Validar opción seleccionada
        if (selectedIndex == q.correctOptionIndex)
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

        // Activar la 'X' correspondiente antes de incrementar currentErrors
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
        Debug.Log("¡PUZZLE DEL TV RESUELTO!");

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (ScreenFlash.Instance != null) ScreenFlash.Instance.FlashSuccess();
        if (audioSource != null && successSound != null) audioSource.PlayOneShot(successSound);

        if (GameManager.Instance != null)
            GameManager.Instance.RevealDigit(codeDigitIndex);

        if (tvCanvas != null) tvCanvas.SetActive(false);

        // Marcar como resuelto y salir del modo puzzle usando la referencia directa
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
        Debug.LogWarning("¡Límite de 3 errores alcanzado en el TV! Reiniciando o saliendo...");

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

        if (tvCanvas != null) tvCanvas.SetActive(false);

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

    public void StopPuzzle()
    {
        IsActive = false;
        GameManager.UnregisterActivePuzzle(this);

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (tvCanvas != null) tvCanvas.SetActive(false);
    }
}