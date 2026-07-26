using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class HintDialogue : MonoBehaviour
{
    public static HintDialogue Instance { get; private set; }

    [SerializeField] TMP_Text hintText;

    [SerializeField] DialogueClass hint;

    public event Action onHintAppeared;

    private void Awake()
    {
        Instance = this;

        hintText.color = hintText.color = new Color(hintText.color.r, hintText.color.g, hintText.color.b, 0);
        hintText.text = hint.dialogue;
    }

    private void Start()
    {
        if (DisappearingText.Instance != null)
        {
            DisappearingText.Instance.onVanishingDialoguesFinished += StartAppearing;
        }
        else
        {
            FadeImage.Instance.OnFadeComplete += StartAppearing;
        }
    }

    private void StartAppearing()
    {
        StartCoroutine(AppearText());
    }

    IEnumerator AppearText()
    {
        while (hintText.color.a < 1)
        {
            hintText.color = new Color(hintText.color.r,
                                       hintText.color.g,
                                       hintText.color.b,
                                       hintText.color.a + Time.unscaledDeltaTime);
            yield return null;
        }

        onHintAppeared?.Invoke();
    }
}
