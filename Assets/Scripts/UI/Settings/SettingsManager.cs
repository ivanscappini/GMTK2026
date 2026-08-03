using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using Hermes;

public class SettingsManager : MonoBehaviour
{
    public static bool TutorialPopupsEnabled => PlayerPrefs.GetInt("TutorialPopups", 1) == 1;
    public static float SavedSensitivity => PlayerPrefs.GetFloat("MouseSensitivity", 0.5f);

    [Header("Audio Mixer")]
    [Tooltip("The MasterMixer asset. Expose volume parameters via right-click > 'Expose to script' in the AudioMixer window.")]
    [SerializeField] private AudioMixer masterMixer;

    [Header("Music")]
    [SerializeField] private SegmentedSlider musicSlider;
    [Tooltip("Name of the exposed parameter on the BGM group (set in AudioMixer).")]
    [SerializeField] private string musicParam = "BGMVolume";

    [Header("SFX")]
    [SerializeField] private SegmentedSlider sfxSlider;
    [Tooltip("Name of the exposed parameter on the SFXMaster group (set in AudioMixer).")]
    [SerializeField] private string sfxParam = "SFXMasterVolume";

    [Header("Mouse Sensitivity")]
    [SerializeField] private Slider sensitivitySlider;
    [Tooltip("Sensitivity when slider is at 0.")]
    [SerializeField] private float minSensitivity = 0.01f;
    [Tooltip("Sensitivity when slider is at 1.")]
    [SerializeField] private float maxSensitivity = 0.3f;

    [Header("Tutorial Popups")]
    [SerializeField] private Toggle tutorialPopupsToggle;

    [Header("Click Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    [Header("CRT Warp Reduction")]
    [Tooltip("The CRT material. When settings are open, fisheye is reduced so pointer positions match visuals.")]
    [SerializeField] private Material crtMaterial;
    [Tooltip("Fisheye strength while settings panel is visible.")]
    [SerializeField, Range(0f, 1f)] private float menuFisheyeStrength = 0.05f;

    private float savedFisheyeStrength;
    private const float MinDb = -80f;
    private const float DefaultMinSens = 0.01f;
    private const float DefaultMaxSens = 0.3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplySavedSensitivityOnLoad()
    {
        var fp = FindObjectOfType<FPController>();
        if (fp == null) return;
        float sens = Mathf.Lerp(DefaultMinSens, DefaultMaxSens, SavedSensitivity);
        fp.Preset.LookSensitivity = new Vector2(sens, sens);
    }

    void OnEnable()
    {
        if (crtMaterial != null)
        {
            savedFisheyeStrength = crtMaterial.GetFloat("_FisheyeStrength");
            crtMaterial.SetFloat("_FisheyeStrength", menuFisheyeStrength);
        }

        LoadSettings();

        if (musicSlider != null)
        {
            musicSlider.OnValueChanged.AddListener(OnMusicChanged);
            musicSlider.OnSegmentStepChanged.AddListener(PlayClickSound);
            musicSlider.OnMuteToggled.AddListener(PlayClickSound);
        }
        if (sfxSlider != null)
        {
            sfxSlider.OnValueChanged.AddListener(OnSFXChanged);
            sfxSlider.OnSegmentStepChanged.AddListener(PlayClickSound);
            sfxSlider.OnMuteToggled.AddListener(PlayClickSound);
        }
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (tutorialPopupsToggle != null)
            tutorialPopupsToggle.onValueChanged.AddListener(OnTutorialPopupsChanged);
    }

    void OnDisable()
    {
        if (crtMaterial != null)
            crtMaterial.SetFloat("_FisheyeStrength", savedFisheyeStrength);

        if (musicSlider != null)
        {
            musicSlider.OnValueChanged.RemoveListener(OnMusicChanged);
            musicSlider.OnSegmentStepChanged.RemoveListener(PlayClickSound);
            musicSlider.OnMuteToggled.RemoveListener(PlayClickSound);
        }
        if (sfxSlider != null)
        {
            sfxSlider.OnValueChanged.RemoveListener(OnSFXChanged);
            sfxSlider.OnSegmentStepChanged.RemoveListener(PlayClickSound);
            sfxSlider.OnMuteToggled.RemoveListener(PlayClickSound);
        }
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        if (tutorialPopupsToggle != null)
            tutorialPopupsToggle.onValueChanged.RemoveListener(OnTutorialPopupsChanged);
    }

    public void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }

    void OnMusicChanged(float value)
    {
        ApplyMusicVolume();
        SaveSettings();
    }

    void OnSFXChanged(float value)
    {
        ApplySFXVolume();
        SaveSettings();
    }

    void OnSensitivityChanged(float value)
    {
        ApplySensitivity();
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
    }

    void ApplySensitivity()
    {
        if (sensitivitySlider == null) return;
        float sens = Mathf.Lerp(minSensitivity, maxSensitivity, sensitivitySlider.value);
        var fp = FindObjectOfType<FPController>();
        if (fp != null)
            fp.Preset.LookSensitivity = new Vector2(sens, sens);
    }

    void OnTutorialPopupsChanged(bool enabled)
    {
        PlayClickSound();
        PlayerPrefs.SetInt("TutorialPopups", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    void ApplyMusicVolume()
    {
        if (masterMixer == null || musicSlider == null) return;
        float vol = musicSlider.IsMuted ? 0f : musicSlider.Value;
        SetMixerVolume(musicParam, vol);
    }

    void ApplySFXVolume()
    {
        if (masterMixer == null || sfxSlider == null) return;
        float vol = sfxSlider.IsMuted ? 0f : sfxSlider.Value;
        SetMixerVolume(sfxParam, vol);
    }

    void SetMixerVolume(string param, float normalizedValue)
    {
        float db = normalizedValue > 0.0001f
            ? Mathf.Log10(normalizedValue) * 20f
            : MinDb;
        masterMixer.SetFloat(param, db);
    }

    void SaveSettings()
    {
        if (musicSlider != null)
        {
            PlayerPrefs.SetFloat("MusicVolume", musicSlider.Value);
            PlayerPrefs.SetInt("MusicMuted", musicSlider.IsMuted ? 1 : 0);
        }
        if (sfxSlider != null)
        {
            PlayerPrefs.SetFloat("SFXVolume", sfxSlider.Value);
            PlayerPrefs.SetInt("SFXMuted", sfxSlider.IsMuted ? 1 : 0);
        }
        if (sensitivitySlider != null)
            PlayerPrefs.SetFloat("MouseSensitivity", sensitivitySlider.value);
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        if (musicSlider != null)
        {
            float vol = PlayerPrefs.GetFloat("MusicVolume", 1f);
            bool muted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
            musicSlider.SetValue(vol, false);
            if (muted) musicSlider.SetActive(false);
            ApplyMusicVolume();
        }
        if (sfxSlider != null)
        {
            float vol = PlayerPrefs.GetFloat("SFXVolume", 1f);
            bool muted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
            sfxSlider.SetValue(vol, false);
            if (muted) sfxSlider.SetActive(false);
            ApplySFXVolume();
        }
        if (sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(SavedSensitivity);
            ApplySensitivity();
        }
        if (tutorialPopupsToggle != null)
            tutorialPopupsToggle.isOn = TutorialPopupsEnabled;
    }
}
