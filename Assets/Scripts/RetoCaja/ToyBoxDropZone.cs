using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ToyBoxDropZone : MonoBehaviour
{
    [Header("Configuración de la Solución")]
    [Tooltip("Lista ordenada con los ItemID exactos que deben meterse en orden")]
    [SerializeField] private List<string> correctSequence = new List<string> { "Ship", "Carro", "Pelota" };
    private List<string> currentSequence = new List<string>();

    [Header("Referencias del Mapa")]
    [SerializeField] private Transform boxInsideSpot;
    [SerializeField] private List<ToyItem> allToysInScene = new List<ToyItem>();

    [Header("Visualización del Código en la Caja")]
    [SerializeField] private GameObject codeDisplayObject;
    [SerializeField] private TextMeshPro codeText3D;
    [SerializeField] private TextMeshProUGUI codeTextUI;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip instructionAudio;
    [SerializeField] private AudioClip depositSound;
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;

    [Header("Dígito GameManager")]
    [SerializeField] private int codeDigitIndex = 1;

    public bool IsPuzzleActive { get; private set; } = false;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (codeDisplayObject != null)
            codeDisplayObject.SetActive(false);
    }

    public void StartPuzzle()
    {
        IsPuzzleActive = true;
        currentSequence.Clear();

        if (audioSource != null && instructionAudio != null)
        {
            audioSource.PlayOneShot(instructionAudio);
        }

        Debug.Log("[ToyBox] Reto Iniciado activado por Canvas/Botón.");
    }

    public void TryDepositItem(PlayerHandManager playerHand)
    {
        if (playerHand == null || !playerHand.IsHoldingItem)
        {
            Debug.LogWarning("[ToyBox] No se puede depositar: La mano del jugador está vacía.");
            return;
        }

        // Si por alguna razón no se presionó "Empezar", activamos el puzzle para no bloquear el test
        if (!IsPuzzleActive)
        {
            Debug.Log("[ToyBox] Activar puzzle automáticamente al recibir el primer objeto.");
            IsPuzzleActive = true;
        }

        // 1. Quitar de la mano
        ToyItem item = playerHand.ClearHand();

        // 2. Colocar dentro de la caja
        item.PlaceInBox(boxInsideSpot);

        // 3. Sonido de depósito
        if (audioSource != null && depositSound != null)
            audioSource.PlayOneShot(depositSound);

        int currentIndex = currentSequence.Count;
        string itemIDIngresado = item.ItemID.Trim();
        string itemIDEsperado = (currentIndex < correctSequence.Count) ? correctSequence[currentIndex].Trim() : "";

        currentSequence.Add(itemIDIngresado);

        Debug.Log($"[ToyBox] Objeto {currentIndex + 1}: Recibido '{itemIDIngresado}' | Esperado '{itemIDEsperado}'");

        // 4. Validar en tiempo real con la secuencia correcta
        if (string.Equals(itemIDIngresado, itemIDEsperado, System.StringComparison.OrdinalIgnoreCase) == false)
        {
            Debug.LogWarning($"[ToyBox] SECUENCIA INCORRECTA: Se recibió '{itemIDIngresado}' pero se esperaba '{itemIDEsperado}'.");
            FailPuzzle();
            return;
        }

        // 5. Si ya puso todos los objetos requeridos y todos son correctos
        if (currentSequence.Count == correctSequence.Count)
        {
            WinPuzzle();
        }
    }

    private void WinPuzzle()
    {
        Debug.Log("¡PUZZLE DE LA CAJA RESUELTO CORRECTAMENTE!");

        if (ScreenFlash.Instance != null)
            ScreenFlash.Instance.FlashSuccess();

        if (audioSource != null && successSound != null)
            audioSource.PlayOneShot(successSound);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RevealDigit(codeDigitIndex);

            char revealedDigit = GameManager.Instance.GetSecretDigitChar(codeDigitIndex);
            if (codeText3D != null) codeText3D.text = revealedDigit.ToString();
            if (codeTextUI != null) codeTextUI.text = revealedDigit.ToString();
        }

        if (codeDisplayObject != null)
            codeDisplayObject.SetActive(true);

        IsPuzzleActive = false;
    }

    private void FailPuzzle()
    {
        IsPuzzleActive = false;

        if (ScreenFlash.Instance != null)
            ScreenFlash.Instance.FlashError();

        if (audioSource != null && errorSound != null)
            audioSource.PlayOneShot(errorSound);

        StartCoroutine(ResetPuzzleWithDelay());
    }

    private IEnumerator ResetPuzzleWithDelay()
    {
        yield return new WaitForSeconds(0.8f);

        currentSequence.Clear();

        // Devolver los juguetes a sus posiciones iniciales
        foreach (var toy in allToysInScene)
        {
            if (toy != null)
            {
                toy.ResetToInitialPosition();
            }
        }

        IsPuzzleActive = true;
        Debug.Log("[ToyBox] Juguetes restablecidos a sus posiciones iniciales. Reto listo para reintentar.");
    }
}