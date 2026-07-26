using UnityEngine;

public class DeadZone : MonoBehaviour
{
    [SerializeField] private PlayerRespawn playerRef;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerRef.RespawnToCheckpoint();
    }
}
