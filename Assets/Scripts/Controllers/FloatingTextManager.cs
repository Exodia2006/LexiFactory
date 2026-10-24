using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [Header("UI Prompt")]
    [Tooltip("Contenedor general del cartel flotante")]
    [SerializeField] private GameObject promptContainer;
    [Tooltip("Imagen donde se renderiza el sprite de la tecla [E]")]
    [SerializeField] private Image keyIconImage;
    [Tooltip("Sprite opcional de la tecla [E]")]
    [SerializeField] private Sprite keyIconSprite;
    [Tooltip("Texto opcional descriptivo de la acción")]
    [SerializeField] private TextMeshProUGUI actionDescriptionText;
    [SerializeField] private TMP_Text labelText; // Compatibilidad con Inspector anterior

    [Header("Settings")]
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

        if (promptContainer == null)
            promptContainer = gameObject;

        if (actionDescriptionText == null && labelText is TextMeshProUGUI tmp)
            actionDescriptionText = tmp;

        if (keyIconImage != null && keyIconSprite != null)
            keyIconImage.sprite = keyIconSprite;

        Hide();
    }

    private void LateUpdate()
    {
        if (gameObject.activeSelf)
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam != null)
            {
                transform.rotation = mainCam.transform.rotation;
            }
        }
    }

    public void ShowText(string text, Vector3 objectPosition)
    {
        if (actionDescriptionText != null)
            actionDescriptionText.text = text;
        else if (labelText != null)
            labelText.text = text;

        if (keyIconImage != null)
            keyIconImage.gameObject.SetActive(true);

        transform.position = objectPosition + offset;

        if (promptContainer != null)
            promptContainer.SetActive(true);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (promptContainer != null && promptContainer != gameObject)
            promptContainer.SetActive(false);

        gameObject.SetActive(false);
    }
}