using UnityEngine;
using Hermes;

public class ActivityTrigger : MonoBehaviour
{
    [Header("Optional UI")]
    [Tooltip("Optional DialogueBoxController to show when the activity is unlocked")]
    [SerializeField] private DialogueBoxController dialogueBoxController;
    [Tooltip("Title to show in the dialogue box when unlocked")]
    [SerializeField] private string unlockTitle = "New Ability";
    [Tooltip("Description to show in the dialogue box when unlocked")]
    [SerializeField, TextArea] private string unlockDescription = "You unlocked a new ability.";

    [Header("Activity")]
    [SerializeField] private Activity activity;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        ActivityManager manager = other.GetComponentInParent<ActivityManager>();

        if (manager == null)
            return;

        if (activity != null)
        {
            manager.SetEnabled(activity, true);

            // If a DialogueBoxController is assigned, show the unlock message.
            if (dialogueBoxController != null)
            {
                dialogueBoxController.ShowDialogueBox(unlockTitle, unlockDescription);
            }
        }
        else
        {
            Debug.Log("No Activity assigned!");
        }
    }
}
