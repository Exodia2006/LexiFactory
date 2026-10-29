using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lead Gameplay Architecture:
/// Gestor centralizado para transiciones suaves e interpoladas entre cámaras (Player Camera <-> Puzzle Cameras).
/// Limpia rigurosamente su estado en cambios o reinicios de escena para evitar cámaras congeladas.
/// </summary>
public class CameraTransitionManager : MonoBehaviour
{
    private static CameraTransitionManager instance;
    public static CameraTransitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CameraTransitionManager>();
                if (instance == null)
                {
                    GameObject managerObj = new GameObject("CameraTransitionManager");
                    instance = managerObj.AddComponent<CameraTransitionManager>();
                    DontDestroyOnLoad(managerObj);
                }
            }
            return instance;
        }
    }

    [Header("Configuración de Transición")]
    [Tooltip("Duración predeterminada en segundos de la interpolación de cámara")]
    [Range(0.1f, 3f)]
    [SerializeField] private float defaultDuration = 0.6f;

    [Tooltip("Usa Mathf.SmoothStep para un suavizado armónico de aceleración/desaceleración")]
    [SerializeField] private bool useSmoothStep = true;

    [Tooltip("Curva personalizada en caso de que useSmoothStep sea falso")]
    [SerializeField] private AnimationCurve customCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Cámara Intermedia / Temporal")]
    [Tooltip("Cámara dedicada a la interpolación (se auto-genera si no se asigna)")]
    [SerializeField] private Camera transitionCamera;

    public bool IsTransitioning => isTransitioning;
    public float DefaultDuration => defaultDuration;

    public static event Action<Camera, Camera> OnTransitionStarted;
    public static event Action<Camera, Camera> OnTransitionCompleted;

    private bool isTransitioning = false;
    private Coroutine activeTransitionCoroutine;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureTransitionCamera();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetTransitionState();
    }

    /// <summary>
    /// Resetea inmediatamente cualquier transición activa y apaga la cámara intermedia.
    /// Invocado automáticamente al cambiar de escena o reiniciar el nivel.
    /// </summary>
    public void ResetTransitionState()
    {
        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
            activeTransitionCoroutine = null;
        }

        isTransitioning = false;

        if (transitionCamera != null)
        {
            transitionCamera.enabled = false;
            transitionCamera.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Garantiza la existencia de una cámara auxiliar dedicada exclusivamente a interpolaciones.
    /// </summary>
    private void EnsureTransitionCamera()
    {
        if (transitionCamera == null)
        {
            GameObject camObj = new GameObject("[CameraTransition_IntermediateCam]");
            camObj.transform.SetParent(transform);
            transitionCamera = camObj.AddComponent<Camera>();
            transitionCamera.enabled = false;
            camObj.SetActive(false);

            // Garantizar que no tenga AudioListener para evitar conflicto con el del jugador
            AudioListener al = camObj.GetComponent<AudioListener>();
            if (al != null)
            {
                Destroy(al);
            }
        }
    }

    /// <summary>
    /// Inicia una transición suave de la cámara de origen a la cámara de destino.
    /// </summary>
    /// <param name="fromCamera">Cámara actual</param>
    /// <param name="toCamera">Cámara destino</param>
    /// <param name="duration">Duración opcional. Si es negativa o 0, usa defaultDuration</param>
    /// <param name="onComplete">Callback ejecutado al finalizar la transición</param>
    public void Transition(Camera fromCamera, Camera toCamera, float duration = -1f, Action onComplete = null)
    {
        if (toCamera == null)
        {
            Debug.LogWarning("[CameraTransitionManager] La cámara destino (toCamera) es nula. Abortando transición.");
            onComplete?.Invoke();
            return;
        }

        if (fromCamera == null)
        {
            Debug.LogWarning("[CameraTransitionManager] La cámara origen (fromCamera) es nula. Activando toCamera directamente.");
            toCamera.gameObject.SetActive(true);
            toCamera.enabled = true;
            onComplete?.Invoke();
            return;
        }

        if (fromCamera == toCamera)
        {
            toCamera.gameObject.SetActive(true);
            toCamera.enabled = true;
            onComplete?.Invoke();
            return;
        }

        float actualDuration = duration > 0f ? duration : defaultDuration;

        EnsureTransitionCamera();

        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
            activeTransitionCoroutine = null;
        }

        activeTransitionCoroutine = StartCoroutine(TransitionRoutine(fromCamera, toCamera, actualDuration, onComplete));
    }

    public void Transition(Camera fromCamera, Camera toCamera, Action onComplete)
    {
        Transition(fromCamera, toCamera, defaultDuration, onComplete);
    }

    private IEnumerator TransitionRoutine(Camera fromCam, Camera toCam, float duration, Action onComplete)
    {
        isTransitioning = true;
        OnTransitionStarted?.Invoke(fromCam, toCam);

        Debug.Log($"[CameraTransitionManager] Iniciando transición: '{fromCam.name}' -> '{toCam.name}' ({duration:F2}s)");

        // 1. Clonar propiedades visuales clave de la cámara origen
        transitionCamera.clearFlags = fromCam.clearFlags;
        transitionCamera.backgroundColor = fromCam.backgroundColor;
        transitionCamera.cullingMask = fromCam.cullingMask;
        transitionCamera.orthographic = fromCam.orthographic;
        transitionCamera.nearClipPlane = fromCam.nearClipPlane;
        transitionCamera.farClipPlane = fromCam.farClipPlane;
        transitionCamera.rect = fromCam.rect;
        transitionCamera.depth = Mathf.Max(fromCam.depth, toCam.depth) + 1f;

        Vector3 startPos = fromCam.transform.position;
        Quaternion startRot = fromCam.transform.rotation;
        float startFov = fromCam.fieldOfView;

        if (transitionCamera.gameObject.activeSelf)
        {
            startPos = transitionCamera.transform.position;
            startRot = transitionCamera.transform.rotation;
            startFov = transitionCamera.fieldOfView;
        }
        else
        {
            transitionCamera.transform.position = startPos;
            transitionCamera.transform.rotation = startRot;
            transitionCamera.fieldOfView = startFov;
        }

        // 2. Activar cámara intermedia y ocultar las demás para evitar parpadeos
        transitionCamera.gameObject.SetActive(true);
        transitionCamera.enabled = true;

        fromCam.gameObject.SetActive(false);
        toCam.gameObject.SetActive(false);

        // 3. Interpolación progresiva frame a frame
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothedT = useSmoothStep ? Mathf.SmoothStep(0f, 1f, t) : customCurve.Evaluate(t);

            Vector3 targetPos = toCam.transform.position;
            Quaternion targetRot = toCam.transform.rotation;
            float targetFov = toCam.fieldOfView;

            transitionCamera.transform.position = Vector3.Lerp(startPos, targetPos, smoothedT);
            transitionCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothedT);
            transitionCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, smoothedT);

            yield return null;
        }

        // 4. Asegurar transformación final exacta
        transitionCamera.transform.position = toCam.transform.position;
        transitionCamera.transform.rotation = toCam.transform.rotation;
        transitionCamera.fieldOfView = toCam.fieldOfView;

        // 5. Encender cámara real de destino y apagar la de transición
        toCam.gameObject.SetActive(true);
        toCam.enabled = true;

        transitionCamera.gameObject.SetActive(false);
        transitionCamera.enabled = false;

        isTransitioning = false;
        activeTransitionCoroutine = null;

        Debug.Log($"[CameraTransitionManager] Transición completada con éxito hacia '{toCam.name}'.");

        OnTransitionCompleted?.Invoke(fromCam, toCam);
        onComplete?.Invoke();
    }
}
