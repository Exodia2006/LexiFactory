using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    private static VolumeController instance;

    [Header("UI Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    // Llaves para guardar en PlayerPrefs
    private const string MasterKey = "MasterVolumePref";
    private const string MusicKey = "MusicVolumePref";
    private const string SFXKey = "SFXVolumePref";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ConfigureSlider(masterSlider);
        ConfigureSlider(musicSlider);
        ConfigureSlider(sfxSlider);

        masterSlider.value = PlayerPrefs.GetFloat(MasterKey, 1f);
        musicSlider.value = PlayerPrefs.GetFloat(MusicKey, 1f);
        sfxSlider.value = PlayerPrefs.GetFloat(SFXKey, 1f);

        SetMasterVolume(masterSlider.value);
        SetMusicVolume(musicSlider.value);
        SetSFXVolume(sfxSlider.value);

        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetMasterVolume(float sliderValue)
    {
        ApplyVolume("MasterVolume", sliderValue);
        PlayerPrefs.SetFloat(MasterKey, sliderValue);
    }

    public void SetMusicVolume(float sliderValue)
    {
        ApplyVolume("MusicVolume", sliderValue);
        PlayerPrefs.SetFloat(MusicKey, sliderValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        ApplyVolume("SFXVolume", sliderValue);
        PlayerPrefs.SetFloat(SFXKey, sliderValue);
    }

    private void ApplyVolume(string parameterName, float linearValue)
    {
        float dB = Mathf.Log10(Mathf.Clamp(linearValue, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(parameterName, dB);
    }

    private void ConfigureSlider(Slider slider)
    {
        if (slider != null)
        {
            slider.minValue = 0.0001f;
            slider.maxValue = 1f;
        }
    }

    private void OnDisable()
    {
        PlayerPrefs.Save();
    }
}