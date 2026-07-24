using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour, ISerializationCallbackReceiver
{

    [Header("Scene Routing")]
#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset mainMenuSceneAsset;
    [SerializeField] private UnityEditor.SceneAsset gameplaySceneAsset;
#endif
    [HideInInspector] 
    [SerializeField] private string mainMenuSceneAssetName;
    [HideInInspector] 
    [SerializeField] private string gameplaySceneAssetName;
    
    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void Button_Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneAssetName);
    }

    public void Button_MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneAssetName);
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
    
    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        if(mainMenuSceneAsset != null)
            mainMenuSceneAssetName = mainMenuSceneAsset.name;
        else
            mainMenuSceneAssetName = string.Empty; 
        
        if(gameplaySceneAsset != null)
            gameplaySceneAssetName = gameplaySceneAsset.name;
        else
            gameplaySceneAssetName = string.Empty;
#endif
    }

    public void OnAfterDeserialize() { }

}