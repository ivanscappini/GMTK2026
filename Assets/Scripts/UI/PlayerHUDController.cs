using UnityEngine;
using TMPro;

public class PlayerHUDController : MonoBehaviour
{
    [SerializeField] private Timer CountdownTimer;
    [SerializeField] private TMP_Text timerText;

    private void Update()
    {
        timerText.text = CountdownTimer.TheTime.ToString("00.00");
    }
}
