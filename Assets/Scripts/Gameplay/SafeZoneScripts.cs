using TMPro;
using UnityEngine;

public class SafeZoneScripts : MonoBehaviour
{
    //A game object that has a TextMeshPro-TextUI that is connected to the player and is used to display on screen the timer left before the safe zone is no longer safe
    [SerializeField] TMP_Text safeZonetimerOnScreen;

    //How long the safe zone is considered safe
    [SerializeField] float safeZonetimer = 20.0f;

    private float localSafeZoneTimer;

    //A game object that has a TextMeshPro-TextUI that is connected to the player and is used to display on screen the timer left before the enemy catches you
    [SerializeField] TMP_Text gameTimerText; //To be changed for sure, but i need to see how the game timer was implemented

    //This variable is used to determine if the collision happened with the player or not, might need to be changed if the player is spawned and is not present from the beginning
    private GameObject playerRef;

    //This variable is to stop and resume enemies activities based on the safe zone state
    private GameObject enemyRef;


    private bool stopTimer = false;
    private bool startTimer = false;
    private bool firstTime = true;

    private void Start()
    {
        ResetTimer();
        firstTime = true;
        playerRef = GameManager.Instance.player;
        enemyRef = GameManager.Instance.enemy;
    }

    private void Update()
    {
        if (!startTimer || !firstTime)
            return;
        if (stopTimer == true || localSafeZoneTimer < 0)
        {
            ContinueGame();
        }
        localSafeZoneTimer -= Time.deltaTime;
        safeZonetimerOnScreen.text = Mathf.CeilToInt(localSafeZoneTimer).ToString();

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.Equals(playerRef))
        {
            stopTimer = false;
            //StartTimer();
            startTimer = true;
            //Missing a call function that stops the gameTimer from ticking down until one of the two condictions are met (1- Timer is over, 2-Player leaves the safe zone)
            playerRef.GetComponent<PlayerRespawn>().setRespawnLocation(this.gameObject);
            enemyRef.SetActive(false); //Placeholder, the enemy should have a variable like isActive that once it is false it makes it not execute any script while still being visibile
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.Equals(playerRef))
        {
            ResetTimer();
            ContinueGame();
        }
    }

    private void ResetTimer()
    {
        localSafeZoneTimer = safeZonetimer;
    }

    private void ContinueGame()
    {
        stopTimer = true;
        startTimer = false;
        firstTime = false;
        safeZonetimerOnScreen.text = "";
        //Missing a call function that start the timer once again the gameTimer from ticking down until one of the two condictions are met (1- Timer is over, 2-Player leaves the safe zone)
        enemyRef.SetActive(true); //Placeholder, the enemy should have a variable like isActive that once it is true it makes it behave normally
    }


}
