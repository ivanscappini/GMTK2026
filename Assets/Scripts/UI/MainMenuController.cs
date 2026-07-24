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

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void Button_StartGame()
    {
        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            Debug.LogError("Gameplay scene name is empty! Make sure a scene is assigned in the Inspector.");
            return;
        }

        Debug.Log($"Starting Game: {gameplaySceneName}");
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void Button_QuitGame()
    {
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