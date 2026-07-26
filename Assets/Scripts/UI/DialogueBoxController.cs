using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueBoxController : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject pauseMenuPanel;
   
    public void Button_GoOn()
    {
        pauseMenuPanel.SetActive(false);
        PauseController.Instance.PlaySelectSound();
        PauseController.Instance.ResumeGame();
    }

    public void ShowDialogueBox()
    {
        PauseController.Instance.OnPause();
        pauseMenuPanel.SetActive(true);
    }
  
}
