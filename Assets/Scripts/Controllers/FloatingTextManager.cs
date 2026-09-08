using TMPro;
using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Vector3 offset = new Vector3(0.4f, 0.3f, 0f); // Desplazamiento al lado del objeto

    private Camera mainCam;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        mainCam = Camera.main;
        Hide();
    }

    private void LateUpdate()
    {
        if (gameObject.activeSelf && mainCam != null)
        {
            transform.rotation = mainCam.transform.rotation;
        }
    }

    public void ShowText(string text, Vector3 objectPosition)
    {
        if (labelText != null)
            labelText.text = text;

        transform.position = objectPosition + offset;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}