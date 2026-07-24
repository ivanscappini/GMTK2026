using UnityEngine;

public class KillPlayerToRemove : MonoBehaviour
{


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            GameManager.Instance.player.GetComponent<PlayerRespawn>().RespawnPlayer();
            Debug.Log("Player Collision");
        }
        Debug.Log("Not Player Collision");

    }
}
