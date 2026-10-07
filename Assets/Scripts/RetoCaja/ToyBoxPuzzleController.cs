using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToyBoxPuzzleController : MonoBehaviour, IPuzzleController
{
    public bool IsActive { get; private set; } = false;

    [Header("Configuración de la Solución")]
    [Tooltip("Secuencia exacta de IDs que el jugador debe presionar")]
    [SerializeField] private List<string> correctSequence = new List<string> { "RedBlock", "BlueBlock", "YellowBlock", "ShipToy", "CarToy", "GreenBlock", "PurpleBlock" };
    private List<string> currentInputSequence = new List<string>();

    [Header("Referencias")]
    [SerializeField] private List<ToyBlock> allBlocks = new List<ToyBlock>();

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;

    [Header("Dígito a Revelar en GameManager")]
    [SerializeField] private int codeDigitIndex = 1; // 1 para el segundo dígito del código

    public void ActivatePuzzle()
    {
        IsActive = true;
        currentInputSequence.Clear();
        ResetAllBlocks();
    }

    public void DeactivatePuzzle()
    {
        IsActive = false;
        currentInputSequence.Clear();
        ResetAllBlocks();
    }

    public void OnBlockClicked(string blockID)
    {
        if (!IsActive) return;

        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        currentInputSequence.Add(blockID);

        // Cuando la secuencia ingresada alcanza el tamaño de la secuencia correcta
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
            Debug.Log("¡PUZZLE DE LA CAJA RESUELTO!");

            if (ScreenFlash.Instance != null)
                ScreenFlash.Instance.FlashSuccess();

            if (audioSource != null && successSound != null)
                audioSource.PlayOneShot(successSound);

            if (GameManager.Instance != null)
                GameManager.Instance.RevealDigit(codeDigitIndex);

            // Notificar al interactuable para salir de la vista de puzzle
            GenericPuzzleInteractable interactable = GetComponent<GenericPuzzleInteractable>();
            if (interactable != null)
                interactable.CompletePuzzle();
            else
                DeactivatePuzzle();
        }
        else
        {
            Debug.LogWarning("Secuencia de la caja incorrecta. Reiniciando...");

            if (ScreenFlash.Instance != null)
                ScreenFlash.Instance.FlashError();

            if (audioSource != null && errorSound != null)
                audioSource.PlayOneShot(errorSound);

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
            if (block != null) block.ResetPosition();
        }
    }
}