using UnityEngine;


public enum TimeChangeType
{
    Add = 0,
    Remove = 1,
}
public class TimePickup : MonoBehaviour
{
    //Should this gameobject add or remove time
    [SerializeField] TimeChangeType changeType;
    //How much time is added or removed when triggered
    [SerializeField] float timeChange = 5;
    [SerializeField] AudioClip pickupSound;

    public AudioSource childAudioSource;

    void Awake()
    {
        // Find and assign the audio source on a child object. Include inactive children
        childAudioSource = GetComponentInChildren<AudioSource>(true);

        if (childAudioSource == null)
        {
            Debug.LogWarning($"No AudioSource found on any child of '{gameObject.name}'!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //check if the colliding object is the player
        if (other.gameObject.CompareTag("Player"))
        {
            if (pickupSound != null)
            {
                var go = new GameObject("TempAudio");
                var src = go.AddComponent<AudioSource>();
                src.spatialBlend = 0f;
                src.clip = pickupSound;
                src.Play();
                Object.Destroy(go, pickupSound.length + 0.1f);
            }

            Debug.Log(other.name);
            if (changeType == TimeChangeType.Add)
            {
                Timer.Instance.AddTime(timeChange);
            }
            else
            {
                //Needs to be one of two options:
                //1-timechange is negative and nothing need to be changed
                //2-timechange is a positive value and you need to remove timeChange and add -timeChange
                Timer.Instance.AddTime(timeChange);
            }
            //After being triggered the game object is destroyed
            Destroy(this.gameObject);
        }
    }
}
