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
    [SerializeField] GameObject player;

    //This variable is to stop and resume enemies activities based on the safe zone state
    [SerializeField] GameObject enemy;

    private bool stopTimer = false;
    private bool startTimer = false;

    private void Start()
    {
        ResetTimer();
    }

    private void Update()
    {
        if (!startTimer)
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
        if (other.gameObject.Equals(player))
        {
            stopTimer = false;
            //StartTimer();
            startTimer = true;
            //Missing a call function that stops the gameTimer from ticking down until one of the two condictions are met (1- Timer is over, 2-Player leaves the safe zone)
            enemy.SetActive(false); //Placeholder, the enemy should have a variable like isActive that once it is false it makes it not execute any script while still being visibile
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.Equals(player))
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
        safeZonetimerOnScreen.text = "";
        //Missing a call function that start the timer once again the gameTimer from ticking down until one of the two condictions are met (1- Timer is over, 2-Player leaves the safe zone)
        enemy.SetActive(true); //Placeholder, the enemy should have a variable like isActive that once it is true it makes it behave normally
    }


}
