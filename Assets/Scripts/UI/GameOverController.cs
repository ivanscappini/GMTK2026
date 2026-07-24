using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{

    private void OnEnable()
    {
        OnGameOver();
    }
    
    public void OnGameOver()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Button_Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        gameObject.SetActive(false);
    }

    public void Button_MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleMenu");
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
}