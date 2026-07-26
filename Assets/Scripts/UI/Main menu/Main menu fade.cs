using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Mainmenufade : MonoBehaviour
{
    public static Mainmenufade Instance { get; private set; }

    [SerializeField] GameObject verticalContainer;

    [SerializeField] GameObject imageContainer;
    [SerializeField] Image image;

    [SerializeField] float maxScale = 25f;
    [SerializeField] float scaleVelocity = 5f;
    [SerializeField] Color colorToFade = Color.black;
    [SerializeField] float WaitTimeBeforeLerpColor = 10f;
    [SerializeField] float fadeMultiplier = 0.2f;
    [SerializeField] float WaitTimeBeforeSendEvent = 15;

    public event Action onFinished;

    private void Awake()
    {
        Instance = this;

        StartCoroutine(MakeItBig());
        Invoke("LerpColor", WaitTimeBeforeLerpColor);
        Invoke("SendEvent", WaitTimeBeforeSendEvent);
    }

    IEnumerator MakeItBig()
    {
        while (imageContainer.transform.localScale.x < maxScale)
        {
            imageContainer.transform.localScale = new Vector3(imageContainer.transform.localScale.x + (Time.deltaTime * scaleVelocity),
                                                              imageContainer.transform.localScale.y + (Time.deltaTime * scaleVelocity),
                                                              imageContainer.transform.localScale.y + (Time.deltaTime * scaleVelocity));
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    private void LerpColor()
    {
        verticalContainer.SetActive(false);

        StartCoroutine(LerpCol());
    }

    IEnumerator LerpCol()
    {
        float timer = 0;
        while (image.color != colorToFade)
        {
            timer += Time.deltaTime * fadeMultiplier;
            image.color = Color.Lerp(image.color, colorToFade, timer);
            yield return null;
        }
    }

    private void SendEvent()
    {
        onFinished?.Invoke();
    }
}
