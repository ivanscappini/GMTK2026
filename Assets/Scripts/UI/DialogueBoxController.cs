using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueBoxController : MonoBehaviour
{
    [Header("UI Reference")]
    // The panel representing the dialogue box. Kept as the existing field so
    // it can be wired in the inspector.
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Text fields")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    /// <summary>
    /// Called by the UI "continue" button.
    /// Hides the dialogue box and resumes the game if it was paused by the box.
    /// </summary>
    public void Button_GoOn()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        // If PauseController exists and the game is currently paused, resume it.
        if (PauseController.Instance != null && PauseController.Instance.IsPaused)
        {
            PauseController.Instance.ResumeGame();
        }
    }

    /// <summary>
    /// Show the dialogue box and optionally pause the game. Also sets the
    /// title and description text so callers (e.g. safe zone) can display
    /// contextual information like unlocked abilities.
    /// </summary>
    public void ShowDialogueBox(string title, string description)
    {
        if (titleText != null)
            titleText.text = title;

        if (descriptionText != null)
            descriptionText.text = description;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
        // Always pause the game when showing the dialogue box so the player
        // has time to read the message without the timer running.
        // Ensure any active gameplay bark is interrupted and fades while paused.
        if (BarksGameplayManager.Instance != null)
        {
            BarksGameplayManager.Instance.FadeActiveGameplayBark();
        }

        if (PauseController.Instance != null && !PauseController.Instance.IsPaused)
        {
            PauseController.Instance.OnPause();
        }
    }

    /// <summary>
    /// Hide the dialogue box without changing pause state.
    /// </summary>
    public void HideDialogueBox()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }
}
