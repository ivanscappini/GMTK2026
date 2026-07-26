using UnityEngine;
using TMPro;

public class PlayerHUDController : MonoBehaviour
{
    [SerializeField] private Timer CountdownTimer;
    [SerializeField] private TMP_Text timerText;

    private void Update()
    {
        if (CountdownTimer == null || timerText == null) return;

        float t = Mathf.Max(0f, CountdownTimer.TheTime);

        int totalSeconds = Mathf.FloorToInt(t);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        int centis = Mathf.FloorToInt((t - totalSeconds) * 100f);

        // Always show mm:ss:cc for clarity (00:00:00)
        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, centis);
    }
}
