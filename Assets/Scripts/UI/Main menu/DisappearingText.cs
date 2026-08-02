using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Plays a sequence of intro dialogues one after another (type out, hold, fade out, next line),
/// then raises onVanishingDialoguesFinished. Part of the main-menu boot sequence, chained after
/// Mainmenufade via that event.
///
/// The typing/fading logic now lives in the shared DialogueTyper utility (also used by
/// BarksManager, BarkGameOver, BarkMainMenu and BarkNewArea) instead of being duplicated here.
/// Unlike the bark scripts, this one doesn't do character-based font switching, but it does now
/// pick up a line's optional color/size override if one is set (font override is not applied
/// here, since the original never assigned a font at all - it relies on whatever font is set on
/// the TMP_Text component in the Inspector).
/// </summary>
public class DisappearingText : MonoBehaviour
{
    public static DisappearingText Instance { get; private set; }

    [SerializeField] DialogueClass[] dialogues;
    private int index = 0;

    [SerializeField] TMP_Text dialoguesText;

    [SerializeField] AudioSource textAudio;

    [Header("Dialogues settings")]
    [SerializeField] private float delayBetweenCharacters = 0.1f;
    [SerializeField] private float delayBetweenDialogues = 1f;
    [SerializeField] private float delayBeforeCharactersVanish = 1.2f;

    [SerializeField] float WaitTimeBeforeSendEvent = 15;

    public event Action onVanishingDialoguesFinished;

    private void Awake()
    {
        Instance = this;
        Invoke("SendEvent", WaitTimeBeforeSendEvent);
    }

    private void Start()
    {
        dialoguesText.text = string.Empty;

        Mainmenufade.Instance.onFinished += StartDialogues;
    }

    public void StartDialogues()
    {
        StartCoroutine(PlayDialogues(dialogues));
    }

    IEnumerator PlayDialogues(DialogueClass[] dialogues)
    {
        while (index < dialogues.Length)
        {
            DialogueClass line = dialogues[index];

            textAudio.Play();

            DialogueTyper.ApplyLineOverrides(dialoguesText, line);

            yield return DialogueTyper.TypeText(dialoguesText, line.dialogue, delayBetweenCharacters, useUnscaledTime: false);

            yield return new WaitForSeconds(delayBeforeCharactersVanish);

            yield return DialogueTyper.FadeOut(dialoguesText, fadeSpeed: 1f, useUnscaledTime: true);

            dialoguesText.text = string.Empty;
            dialoguesText.color = Color.white;

            index++;
            yield return new WaitForSeconds(delayBetweenDialogues);
        }
    }

    private void SendEvent()
    {
        onVanishingDialoguesFinished?.Invoke();
    }
}