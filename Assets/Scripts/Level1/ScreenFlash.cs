using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFlash : MonoBehaviour
{
    public static ScreenFlash Instance { get; private set; }

    [SerializeField] private Image flashImage;
    [SerializeField] private float flashDuration = 0.5f; // Duración en segundos

    private Coroutine flashCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (flashImage == null)
            flashImage = GetComponent<Image>();

        // Asegurar que comience invisible
        SetAlpha(0f);
    }

    public void FlashSuccess()
    {
        // Verde con un 40% de opacidad para que no tape completamente la vista
        Color successColor = new Color(0f, 1f, 0.2f, 0.4f);
        TriggerFlash(successColor);
    }

    public void FlashError()
    {
        // Rojo con un 40% de opacidad
        Color errorColor = new Color(1f, 0f, 0.1f, 0.4f);
        TriggerFlash(errorColor);
    }

    private void TriggerFlash(Color targetColor)
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(DoFlash(targetColor));
    }

    private IEnumerator DoFlash(Color targetColor)
    {
        if (flashImage == null) yield break;

        flashImage.color = targetColor;

        float elapsedTime = 0f;
        Color initialColor = targetColor;
        Color finalColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);

        // Desvanecer el color paulatinamente hasta volver a invisible (Alpha 0)
        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            flashImage.color = Color.Lerp(initialColor, finalColor, elapsedTime / flashDuration);
            yield return null;
        }

        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        if (flashImage != null)
        {
            Color c = flashImage.color;
            c.a = alpha;
            flashImage.color = c;
        }
    }
}