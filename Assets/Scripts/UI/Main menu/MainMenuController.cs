using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour, ISerializationCallbackReceiver
{
    [Header("Scene Routing")]
#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset gameplaySceneAsset;
#endif

    [HideInInspector]
    [SerializeField] private string gameplaySceneName;

    [Header("Panels")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }

    public void Button_StartGame()
    {
        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            Debug.LogError("Gameplay scene name is empty! Make sure a scene is assigned in the Inspector.");
            return;
        }

        PlayClickSound();
        Debug.Log($"Starting Game: {gameplaySceneName}");
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void Button_ToggleOptions()
    {
        PlayClickSound();
        if (optionsPanel != null)
            optionsPanel.SetActive(!optionsPanel.activeSelf);
    }

    public void Button_OpenCredits()
    {
        PlayClickSound();
        if (creditsPanel != null)
            creditsPanel.SetActive(true);
    }

    public void Button_CloseCredits()
    {
        PlayClickSound();
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }

    public void Button_QuitGame()
    {
        PlayClickSound();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        if(gameplaySceneAsset != null)
            gameplaySceneName = gameplaySceneAsset.name;
        else
            gameplaySceneName = string.Empty;
#endif
    }

    public void OnAfterDeserialize() { }
}