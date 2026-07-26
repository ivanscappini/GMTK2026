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

    private void OnTriggerEnter(Collider other)
    {
        //check if the colliding object is the player
        if (other.gameObject.CompareTag("Player"))
        {
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
