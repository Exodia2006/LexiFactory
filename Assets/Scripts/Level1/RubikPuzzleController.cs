using System.Collections.Generic;
using UnityEngine;

public class RubikPuzzleController : MonoBehaviour
{
    public bool IsActive { get; private set; } = false;

    [Header("Configuración de la Solución")]
    // Asegúrate de que los strings coincidan EXACTAMENTE (mayúsculas/minúsculas)
    [SerializeField] private List<string> correctSequence = new List<string> { "Red", "Blue", "Green" };
    private List<string> currentInputSequence = new List<string>();

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip failureSound; 

    [Header("Ajuste de Código Global")]
    [SerializeField] private int codeDigitIndex = 0; // Índice del dígito a desbloquear (ej. 0 para el 1er número)

    public void ActivatePuzzle()
    {
        IsActive = true;
        currentInputSequence.Clear(); // Limpiar intentos previos
    }

    public void DeactivatePuzzle()
    {
        IsActive = false;
        currentInputSequence.Clear();
    }

    public void OnBlockClicked(string colorName)
    {
        if (!IsActive) return;

        // 1. Reproducir sonido de clic
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // 2. Registrar el color ingresado
        currentInputSequence.Add(colorName);

        // 3. Validar si ya se completó la cantidad de colores requerida
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

            // Flash VERDE en pantalla
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
            audioSource.PlayOneShot(failureSound);
            // Flash ROJO en pantalla al equivocarse
            if (ScreenFlash.Instance != null)
                ScreenFlash.Instance.FlashError();
            currentInputSequence.Clear();
        }
    }
}