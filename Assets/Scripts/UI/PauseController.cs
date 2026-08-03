using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hermes;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PauseController : MonoBehaviour, ISerializationCallbackReceiver
{
    public static PauseController Instance { get; private set; }

    public event Action OnPauseStarted;
    public event Action OnPauseEnded;

    [Header("UI Reference")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject optionsPanel;

    [SerializeField] private TMP_Text playText;
    [SerializeField] private Image playPauseButtonImage;
    [SerializeField] private Sprite playSprite;
    [SerializeField] private Sprite pauseSprite;
    // support a simple SpriteRenderer-based button (non-UI)
    [SerializeField] private SpriteRenderer playPauseSpriteRenderer;
    // optional: explicitly assign the GameObject that represents the play/pause icon
    [SerializeField] private GameObject playPauseButtonObject;

    [Header("PlayerInput Map")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pauseClip;
    [SerializeField] private AudioClip selectClip;
    [SerializeField] private AudioClip unpauseClip;

    [FormerlySerializedAs("gameplaySceneAsset")]
    [Header("Scene Routing")]
#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset mainMenuSceneAsset;
#endif
    [HideInInspector]
    [SerializeField] private string mainMenuSceneAssetName;

    public bool IsPaused { get; private set; }

    // cached FPController to disable camera/movement while paused
    private FPController fpController;
    private bool prevLookEnabled = true;
    private bool prevMovementEnabled = true;

    private void Awake()
    {
        Instance = this;
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }

    private void Start()
    {
        // try find FPController in scene to toggle look/movement on pause
        fpController = FindObjectOfType<FPController>();
        if (fpController != null)
        {
            prevLookEnabled = fpController.LookEnabled;
            prevMovementEnabled = fpController.MovementEnabled;
        }
        if (playerInput == null)
        {
            Debug.LogError("Player input is null");
        }

        // If an explicit play/pause GameObject was assigned, prefer its image/sprite renderer.
        if (playPauseButtonObject != null)
        {
            if (playPauseButtonImage == null)
                playPauseButtonImage = playPauseButtonObject.GetComponent<Image>();

            if (playPauseSpriteRenderer == null)
                playPauseSpriteRenderer = playPauseButtonObject.GetComponent<SpriteRenderer>();
        }

        // If neither was assigned explicitly, fall back to searching the pause menu hierarchy.
        if (playPauseButtonImage == null && playPauseSpriteRenderer == null && pauseMenuPanel != null)
        {
            var btn = pauseMenuPanel.GetComponentInChildren<UnityEngine.UI.Button>(true);
            if (btn != null)
            {
                playPauseButtonImage = btn.GetComponent<UnityEngine.UI.Image>();
            }

            if (playPauseButtonImage == null)
            {
                playPauseSpriteRenderer = pauseMenuPanel.GetComponentInChildren<SpriteRenderer>(true);
            }
        }
    }

    public void Button_ToggleOptions()
    {
        PlaySelectSound();
        if (optionsPanel != null)
            optionsPanel.SetActive(!optionsPanel.activeSelf);
    }

    public void Button_MainMenu()
    {
        PlaySelectSound();
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


    public void PlayPauseSound()
    {
        if (audioSource != null && pauseClip != null)
            audioSource.PlayOneShot(pauseClip);
    }

    public void PlaySelectSound()
    {
        if (audioSource != null && selectClip != null)
            audioSource.PlayOneShot(selectClip);
    }

    public void PlayUnpauseSound()
    {
        if (audioSource != null && unpauseClip != null)
            audioSource.PlayOneShot(unpauseClip);
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

        // update camera / movement enable state so camera stops while paused
        if (fpController != null)
        {
            if (IsPaused)
            {
                prevLookEnabled = fpController.LookEnabled;
                prevMovementEnabled = fpController.MovementEnabled;
                fpController.LookEnabled = false;
                fpController.MovementEnabled = false;
            }
            else
            {
                fpController.LookEnabled = prevLookEnabled;
                fpController.MovementEnabled = prevMovementEnabled;
            }
        }

        Time.timeScale = IsPaused ? 0f : 1f;

        // update play text and button sprite if assigned
        if (playText != null)
        {
            playText.text = IsPaused ? "PAUSE" : "PLAY";
        }

        if (playPauseButtonImage != null)
        {
            playPauseButtonImage.sprite = IsPaused ? pauseSprite : playSprite;
        }
        else if (playPauseSpriteRenderer != null)
        {
            playPauseSpriteRenderer.sprite = IsPaused ? pauseSprite : playSprite;
        }

        // play audio feedback
        if (IsPaused) PlayPauseSound(); else PlayUnpauseSound();

        if (!IsPaused)
        {
            OnPauseEnded?.Invoke();
        }

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

            // play pause/unpause sounds
            if (audioSource != null)
            {
                if (IsPaused && pauseClip != null)
                    audioSource.PlayOneShot(pauseClip);
                else if (!IsPaused && unpauseClip != null)
                    audioSource.PlayOneShot(unpauseClip);
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