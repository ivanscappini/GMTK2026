using TMPro;
using UnityEngine;

public class SafeZoneScripts : MonoBehaviour
{
    [SerializeField] GameObject safeZoneSpawnPoint;

    //How long the safe zone is considered safe
    [SerializeField] float safeZoneAddTimer = 20.0f;


    //A game object that has a TextMeshPro-TextUI that is connected to the player and is used to display on screen the timer left before the enemy catches you
    [SerializeField] TMP_Text gameTimerText; //To be changed for sure, but i need to see how the game timer was implemented

    //This variable is used to determine if the collision happened with the player or not, might need to be changed if the player is spawned and is not present from the beginning
    private GameObject playerRef;

    private void Start()
    {
        playerRef = GameManager.Instance.player;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.Equals(playerRef))
        {
            playerRef.GetComponent<PlayerRespawn>().setRespawnLocation(safeZoneSpawnPoint,safeZoneAddTimer);
        }
    }
}
