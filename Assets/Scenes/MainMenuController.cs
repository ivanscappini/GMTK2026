using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEngine;

public class MainMenuController : MonoBehaviour
{
   [Header("Scene Routing")] 
   [SerializeField] private string gameplaySceneName = "02_FPSBase";

   public void Button_StartGame()
   {
      Debug.Log("Starting Game");
      SceneManager.LoadScene(gameplaySceneName);
   }
}
