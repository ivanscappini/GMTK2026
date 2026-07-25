using UnityEngine;
using Hermes;

public class ActivityTrigger : MonoBehaviour
{
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private Activity activity;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        ActivityManager manager =
            other.GetComponentInParent<ActivityManager>();

        if (manager == null)
            return;

        if (activity != null)
        {
            manager.SetEnabled(activity, true);
            //dialogueBox.SetActive(true);
        }
        else
        {
            Debug.Log("No Activity assigned!");
        }
    }
}