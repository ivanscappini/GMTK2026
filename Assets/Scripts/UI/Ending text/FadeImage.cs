using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeImage : MonoBehaviour
{
    public static FadeImage Instance { get; private set; }

    [SerializeField] Image image;
    [SerializeField] float fadeMultiplier = 0.2f;
    [SerializeField] AudioSource audioToFade;

    public event Action OnFadeComplete;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(FadeInImage());
    }

    IEnumerator FadeInImage()
    {
        while (image.color.a < 1)
        {
            image.color = new Color(image.color.r,
                                    image.color.g,
                                    image.color.b,
                                    image.color.a + Time.unscaledDeltaTime * fadeMultiplier);

            audioToFade.volume -= Time.unscaledDeltaTime * fadeMultiplier;
            yield return null;
        }
        audioToFade.volume = 0;
        OnFadeComplete?.Invoke();
    }
}
