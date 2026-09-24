using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RubikPuzzleController : MonoBehaviour
{
    public bool IsActive { get; private set; } = false;

    [Header("Configuración de la Solución")]
    [SerializeField] private List<string> correctSequence = new List<string> { "Red", "Blue", "Green" };
    private List<string> currentInputSequence = new List<string>();

    [Header("Referencias de Bloques")]
    [SerializeField] private List<RubikBlock> allBlocks = new List<RubikBlock>(); // Asigna los 9 bloques aquí

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip successSound;

    [Header("Ajuste de Código Global")]
    [SerializeField] private int codeDigitIndex = 0;

    public void ActivatePuzzle()
    {
        IsActive = true;
        currentInputSequence.Clear();
        ResetAllBlocks(); // Asegurar que inicien en posición de reposo
    }

    public void DeactivatePuzzle()
    {
        IsActive = false;
        currentInputSequence.Clear();
        ResetAllBlocks();
    }

    public void OnBlockClicked(string colorName)
    {
        if (!IsActive) return;

        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        currentInputSequence.Add(colorName);

        if (currentInputSequence.Count == correctSequence.Count)
        {
            CheckSolution();
        }
    }

    private void CheckSolution()
    {
        bool isCorrect = true;

        for (int i = 0; i < correctSequence.Count; i++)
        {
            if (currentInputSequence[i] != correctSequence[i])
            {
                isCorrect = false;
                break;
            }
        }

        if (isCorrect)
        {
            Debug.Log("¡ACERTIJO RESUELTO!");

            if (ScreenFlash.Instance != null)
                ScreenFlash.Instance.FlashSuccess();

            if (audioSource != null && successSound != null)
                audioSource.PlayOneShot(successSound);

            if (GameManager.Instance != null)
                GameManager.Instance.RevealDigit(codeDigitIndex);

            RubikPuzzleInteractable puzzleInteractable = GetComponentInParent<RubikPuzzleInteractable>();
            if (puzzleInteractable != null)
                puzzleInteractable.ExitPuzzle();
            else
                DeactivatePuzzle();
        }
        else
        {
            Debug.LogWarning("Secuencia incorrecta. Reiniciando botones...");

            if (ScreenFlash.Instance != null)
                ScreenFlash.Instance.FlashError();

            // Esperar un pequeño instante (0.3s) antes de devolver los botones a su lugar
            StartCoroutine(ResetSequenceWithDelay());
        }
    }

    private IEnumerator ResetSequenceWithDelay()
    {
        yield return new WaitForSeconds(0.3f);

        currentInputSequence.Clear();
        ResetAllBlocks();
    }

    private void ResetAllBlocks()
    {
        foreach (var block in allBlocks)
        {
            if (block != null)
            {
                block.ResetPosition();
            }
        }
    }
}