using UnityEngine;
using TMPro;

/// <summary>
/// Lead Gameplay Architecture:
/// Contenedor de datos y referencias de escena para vincular de forma desacoplada la UI,
/// cámaras y configuraciones del nivel con el GameManager al cargar o recargar escenas.
/// Evita la pérdida de referencias en transiciones de escena.
/// </summary>
public class LevelDataBinder : MonoBehaviour
{
    [Header("Referencias de UI del Nivel")]
    [Tooltip("Texto en la UI del Canvas que muestra los dígitos descubiertos '? ? ? ?'")]
    [SerializeField] private TextMeshProUGUI codeDisplayText;

    [Header("Cámara y Jugador")]
    [Tooltip("Cámara principal en primera persona del jugador")]
    [SerializeField] private Camera playerCamera;

    [Header("Configuración Opcional")]
    [Tooltip("Código secreto de la puerta para este nivel (deja vacío para mantener el de GameManager)")]
    [SerializeField] private string secretCodeOverride = "";

    public TextMeshProUGUI CodeDisplayText => codeDisplayText;
    public Camera PlayerCamera => playerCamera;
    public string SecretCodeOverride => secretCodeOverride;

    private void Awake()
    {
        RegisterWithGameManager();
    }

    private void Start()
    {
        RegisterWithGameManager();
    }

    private void RegisterWithGameManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.BindLevelData(codeDisplayText, secretCodeOverride);
        }
    }
}
