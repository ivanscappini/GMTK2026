using UnityEngine;
using UnityEngine.SceneManagement;

public class KillPlayerToRemove : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            SceneManager.LoadScene("GameOver");
            Debug.Log("Player Collision");
        }
    }
}
