using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Hermes;

public class SafeZoneScripts : MonoBehaviour
{
    // tracks which safe-zone tutorials have been shown this session (in-memory only)
    private static System.Collections.Generic.HashSet<int> s_shownTutorials = new System.Collections.Generic.HashSet<int>();

    [SerializeField] GameObject safeZoneSpawnPoint;
    [SerializeField] GameObject[] monsters;
    //How long the safe zone is considered safe
    [SerializeField] float safeZoneSetTimer = 20.0f;

    [Header("Optional Unlock / Tutorial")]
    [Tooltip("Optional activity to enable when entering this safe zone (e.g. new ability).")]
    [SerializeField] private Activity activityToUnlock;
    [Tooltip("Optional DialogueBoxController to show when the activity is unlocked")]
    [SerializeField] private DialogueBoxController dialogueBoxController;
    [SerializeField] private string unlockTitle = "New Ability";
    [SerializeField, TextArea] private string unlockDescription = "You unlocked a new ability.";

    [Tooltip("Optional tutorial/dialogue to show when entering this safe zone (shown even if no activity is unlocked).")]
    [SerializeField] private DialogueBoxController tutorialDialogueController;
    [SerializeField] private string tutorialTitle = "";
    [SerializeField, TextArea] private string tutorialDescription = "";
    [Tooltip("If true, the tutorial/dialogue will only be shown once.")]
    [SerializeField] private bool tutorialOneShot = true;


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
            var respawn = playerRef.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                float checkpointTime = Timer.Instance != null ? Timer.Instance.TheTime : 0f;
                respawn.setRespawnLocation(safeZoneSpawnPoint, checkpointTime, monsters);
                Debug.Log($"SafeZone: recorded checkpoint time {checkpointTime}");
            }

            foreach (var m in monsters)
            {
                m.SetActive(true);
            }

            // Optional: unlock an activity/ability. Only show the unlock dialog if it wasn't already enabled.
            if (activityToUnlock != null)
            {
                var manager = playerRef.GetComponentInParent<ActivityManager>();
                if (manager != null)
                {
                    bool alreadyEnabled = manager.IsEnabled(activityToUnlock);
                    manager.SetEnabled(activityToUnlock, true);
                    if (!alreadyEnabled && dialogueBoxController != null)
                    {
                        dialogueBoxController.ShowDialogueBox(unlockTitle, unlockDescription);
                    }
                }
                else
                {
                    Debug.LogWarning("SafeZone: no ActivityManager found on player to enable activity.");
                }
            }

            // Optional tutorial/dialogue that can be shown on entering this safe zone.
            // This is independent of activity unlocks and will be shown if configured.
            if (tutorialDialogueController != null && (!string.IsNullOrEmpty(tutorialTitle) || !string.IsNullOrEmpty(tutorialDescription)))
            {
                bool shouldShow = true;
                int id = gameObject.GetInstanceID();
                if (tutorialOneShot)
                {
                    shouldShow = !s_shownTutorials.Contains(id);
                }

                if (shouldShow)
                {
                    tutorialDialogueController.ShowDialogueBox(tutorialTitle, tutorialDescription);
                    if (tutorialOneShot)
                    {
                        s_shownTutorials.Add(id);
                    }
                }
            }
        }
    }
}
