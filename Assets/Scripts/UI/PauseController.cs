using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PauseController : MonoBehaviour, ISerializationCallbackReceiver
{
    public static PauseController Instance { get; private set; }

    public event Action OnPauseStarted;

    [Header("UI Reference")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("PlayerInput Map")]
    [SerializeField] private PlayerInput playerInput;

    [FormerlySerializedAs("gameplaySceneAsset")]
    [Header("Scene Routing")]
#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset mainMenuSceneAsset;
#endif
    [HideInInspector]
    [SerializeField] private string mainMenuSceneAssetName;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void Button_MainMenu()
    {
        ResumeGame();

        if (string.IsNullOrEmpty(mainMenuSceneAssetName))
        {
            Debug.LogError("Gameplay scene name is empty! Make sure a scene is assigned in the Inspector.");
            return;
        }

        Debug.Log($"Starting Game: {mainMenuSceneAssetName}");
        SceneManager.LoadScene(mainMenuSceneAssetName);
    }

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        if (mainMenuSceneAsset != null)
            mainMenuSceneAssetName = mainMenuSceneAsset.name;
        else
            mainMenuSceneAssetName = string.Empty;
#endif
    }

    public void OnAfterDeserialize() { }


    private void Start()
    {
        if (playerInput == null)
        {
            Debug.LogError("Player input is null");
        }
    }

    public void OnPause()
    {
        TogglePause();
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;

        if (IsPaused)
        {
            OnPauseStarted?.Invoke();
        }

        Time.timeScale = IsPaused ? 0f : 1f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(IsPaused);
        }

        Cursor.lockState = IsPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsPaused;

        if (playerInput != null)
        {
            if (IsPaused)
            {
                playerInput.currentActionMap.Disable();
            }
            else
            {
                playerInput.currentActionMap.Enable();
            }
        }
    }

    public void ResumeGame()
    {
        if (IsPaused)
        {
            TogglePause();
        }
    }

    // Called by the Rewind button in the pause menu: respawn player to last
    // checkpoint and restore the timer value recorded when the checkpoint was hit.
    public void Button_RewindToLastCheckpoint()
    {
        // play select feedback
        PlaySelectSound();

        // attempt to find the player respawn component in scene
        var player = FindObjectOfType<PlayerRespawn>();
        if (player == null)
        {
            Debug.LogWarning("No PlayerRespawn found for rewind.");
            return;
        }

        // PlayerRespawn stores a separate checkpoint. Use RespawnToCheckpoint
        // so rewind moves the player to the last safe area and restores time.
        player.RespawnToCheckpoint();

        // unpause the game after rewinding
        ResumeGame();
    }

}
