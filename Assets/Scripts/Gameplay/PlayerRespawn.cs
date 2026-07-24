using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] GameObject Player;
    CharacterController cc;

    private Vector3 respawnLocation;
    private Quaternion respawnRotation;
    
    private void Start()
    {
        Timer.Instance.countdownEnd.AddListener(ShowGameOver);
        
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
    }

    public void setRespawnLocation(GameObject target)
    {
        respawnLocation = target.transform.position; //- new Vector3(0,(target.GetComponent<BoxCollider>().bounds.min.y + 3),0);
        respawnRotation = target.transform.rotation;

    }
    
    private void ShowGameOver()
    {
        gameOverScreen.SetActive(true);
    }
}
