using UnityEngine;

// Put this on trigger volumes marking checkpoints. When the player enters,
// it records the player's position/rotation and the current Timer.TheTime.
public class Checkpoint : MonoBehaviour
{
    [SerializeField] GameObject[] monsters;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var respawn = other.GetComponent<PlayerRespawn>();
        if (respawn != null)
        {
            // record this checkpoint's transform and the absolute time when it was reached
            float checkpointTime = Timer.Instance != null ? Timer.Instance.TheTime : 0f;
            respawn.setRespawnLocation(gameObject, checkpointTime, monsters);
            Debug.Log($"Checkpoint registered at time {checkpointTime}");
        }
    }
}
