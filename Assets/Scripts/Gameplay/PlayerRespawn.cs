using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] GameObject Player;
    CharacterController cc;

    private Vector3 respawnLocation;
    private Quaternion respawnRotation;
    private float timeToAddUponRespawn;

    private void Start()
    {
        cc = Player.GetComponent<CharacterController>();
        respawnLocation = Player.transform.position;
        respawnRotation = Player.transform.rotation;
        if (!Player)
        {
            Player = gameObject;
        }
    }

    public void RespawnPlayer()
    {
        cc.enabled = false;
        Player.transform.position = respawnLocation;
        Player.transform.rotation = respawnRotation;
        cc.enabled = true;
        Timer.Instance.SetTime(timeToAddUponRespawn);
    }

    public void setRespawnLocation(GameObject target, float timeToAdd)
    {
        respawnLocation = target.transform.position; //- new Vector3(0,(target.GetComponent<BoxCollider>().bounds.min.y + 3),0);
        respawnRotation = target.transform.rotation;
        timeToAddUponRespawn = timeToAdd;

    }
}
