using UnityEngine;
using UnityEngine.Audio;
using Hermes;

public class SettingsLoader : MonoBehaviour
{
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private string musicParam = "BGMVolume";
    [SerializeField] private string sfxParam = "SFXMasterVolume";
    [SerializeField] private float minSensitivity = 0.01f;
    [SerializeField] private float maxSensitivity = 0.3f;

    private const float MinDb = -80f;

    void Awake()
    {
        ApplyAudio();
        ApplySensitivity();
    }

    void ApplyAudio()
    {
        if (masterMixer == null) return;

        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
        bool musicMuted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
        SetVolume(musicParam, musicMuted ? 0f : musicVol);

        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);
        bool sfxMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
        SetVolume(sfxParam, sfxMuted ? 0f : sfxVol);
    }

    void ApplySensitivity()
    {
        float saved = PlayerPrefs.GetFloat("MouseSensitivity", 0.5f);
        float sens = Mathf.Lerp(minSensitivity, maxSensitivity, saved);
        var fp = FindObjectOfType<FPController>();
        if (fp != null)
            fp.Preset.LookSensitivity = new Vector2(sens, sens);
    }

    void SetVolume(string param, float normalizedValue)
    {
        float db = normalizedValue > 0.0001f
            ? Mathf.Log10(normalizedValue) * 20f
            : MinDb;
        masterMixer.SetFloat(param, db);
    }
}
