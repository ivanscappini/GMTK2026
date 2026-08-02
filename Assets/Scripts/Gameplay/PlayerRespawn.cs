using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] GameObject Player;
    GameObject[] Monster;

    CharacterController cc;
    private Vector3 respawnLocation;
    private Quaternion respawnRotation;
    // initial start-of-level respawn (used when dying)
    private Vector3 initialLocation;
    private Quaternion initialRotation;
    private float initialTime;

    // checkpoint-specific respawn (used by rewind)
    private Vector3 checkpointLocation;
    private Quaternion checkpointRotation;
    private float savedTimeAtCheckpoint;

    public event Action OnRespawnToCheckpoint;

    private void Start()
    {
        if (Player == null)
        {
            Player = gameObject;
        }
        cc = Player.GetComponent<CharacterController>();

        // record initial start position/time
        initialLocation = Player.transform.position;
        initialRotation = Player.transform.rotation;
        initialTime = Timer.Instance != null ? Timer.Instance.TimerStartTime : 0f;

        // default respawn and checkpoint to initial
        respawnLocation = initialLocation;
        respawnRotation = initialRotation;
        checkpointLocation = initialLocation;
        checkpointRotation = initialRotation;
        savedTimeAtCheckpoint = initialTime;
    }

    public void RespawnPlayer()
    {
        cc.enabled = false;
        // Respawn to the level start (original behaviour when dying)
        Player.transform.position = initialLocation;
        Player.transform.rotation = initialRotation;
        cc.enabled = true;
        if (Timer.Instance != null)
        {
            Timer.Instance.SetTime(initialTime);
        }
    }

    /// <summary>
    /// Record a respawn point (typically called by a checkpoint/safe zone).
    /// <paramref name="target"/> is the checkpoint GameObject whose transform will be used.
    /// <paramref name="checkpointTime"/> is the absolute Timer.TheTime recorded when the checkpoint was hit.
    /// </summary>
    /// <summary>
    /// Record a respawn point (checkpoint). The checkpoint is used by the
    /// Rewind feature; death still returns the player to the level start.
    /// </summary>
    public void setRespawnLocation(GameObject target, float checkpointTime, GameObject[] monsters = null)
    {
        if (target == null) return;

        checkpointLocation = target.transform.position;
        checkpointRotation = target.transform.rotation;
        savedTimeAtCheckpoint = checkpointTime;
        Debug.Log($"Checkpoint recorded at {checkpointLocation} with time {savedTimeAtCheckpoint}");

        Monster = monsters;
    }

    /// <summary>
    /// Respawn the player to the last recorded checkpoint and restore the
    /// Timer to the checkpoint time. This is used by the Rewind feature.
    /// </summary>
    public void RespawnToCheckpoint()
    {
        cc.enabled = false;
        Player.transform.position = checkpointLocation;
        Player.transform.rotation = checkpointRotation;
        cc.enabled = true;
        if (Timer.Instance != null)
        {
            Timer.Instance.SetTime(savedTimeAtCheckpoint);
        }

        //activate monster at respawn
        foreach (var Mon in Monster)
        {
            Mon.GetComponent<SplineAnimate>().ElapsedTime = 0;
            Mon.SetActive(false);
        }

        foreach (var Mon in Monster)
        {
            Mon.SetActive(true);
        }

        OnRespawnToCheckpoint?.Invoke();
    }
}
