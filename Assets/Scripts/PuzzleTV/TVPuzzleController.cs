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

    [Header("Referencias de Interacción")]
    [SerializeField] private GenericPuzzleInteractable puzzleInteractable;

    [Header("Lista de Preguntas")]
    [SerializeField] private List<MultipleChoiceQuestion> questions = new List<MultipleChoiceQuestion>();
    private int currentQuestionIndex = 0;

    [Header("Audio & Flash")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip selectSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;

    [Header("GameManager Code Index")]
    [SerializeField] private int codeDigitIndex = 2; // Índice 2 = 3er dígito ("7392" -> índice 2 es '9')

    public bool IsActive { get; private set; } = false;

    private void Start()
    {
        if (tvCanvas != null) tvCanvas.SetActive(false);

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
        IsActive = true;
        currentQuestionIndex = 0;
        if (tvCanvas != null) tvCanvas.SetActive(true);
        DisplayCurrentQuestion();
    }

    private void DisplayCurrentQuestion()
    {
        if (currentQuestionIndex >= questions.Count) return;

        var q = questions[currentQuestionIndex];
        if (questionTextUI != null) questionTextUI.text = q.questionText;
        if (progressTextUI != null) progressTextUI.text = $"Pregunta {currentQuestionIndex + 1}/{questions.Count}";

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                if (i < q.options.Length)
                {
                    if (optionTexts[i] != null) optionTexts[i].text = q.options[i];
                    optionButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void OnOptionSelected(int selectedIndex)
    {
        if (!IsActive) return;

        if (audioSource != null && selectSound != null)
            audioSource.PlayOneShot(selectSound);

        var q = questions[currentQuestionIndex];

        // Validar opción seleccionada
        if (selectedIndex == q.correctOptionIndex)
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
        Debug.Log("¡PUZZLE DEL TV RESUELTO!");

        if (ScreenFlash.Instance != null) ScreenFlash.Instance.FlashSuccess();
        if (audioSource != null && successSound != null) audioSource.PlayOneShot(successSound);

        if (GameManager.Instance != null)
            GameManager.Instance.RevealDigit(codeDigitIndex);

        if (tvCanvas != null) tvCanvas.SetActive(false);

        // Salir del modo puzzle usando la referencia directa
        if (puzzleInteractable != null)
        {
            puzzleInteractable.ExitPuzzle();
        }
    }

    private void FailPuzzle()
    {
        IsActive = false;
        Debug.LogWarning("¡Error en la respuesta del TV! Reiniciando...");

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

    public void StopPuzzle()
    {
        IsActive = false;
        if (tvCanvas != null) tvCanvas.SetActive(false);
    }
}