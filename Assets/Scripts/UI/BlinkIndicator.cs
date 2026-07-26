using UnityEngine;

public class BlinkIndicator : MonoBehaviour
{
    [SerializeField] private float blinkInterval = 0.5f; // seconds per on/off toggle
    [SerializeField] private bool startVisible = true;

    private SpriteRenderer spriteRenderer;
    private float timer;
    private bool isVisible;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        isVisible = startVisible;
        spriteRenderer.enabled = isVisible;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= blinkInterval)
        {
            timer -= blinkInterval;
            isVisible = !isVisible;
            spriteRenderer.enabled = isVisible;
        }
    }
}