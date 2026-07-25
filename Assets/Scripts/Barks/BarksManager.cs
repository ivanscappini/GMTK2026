using Hermes;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class BarksManager : MonoBehaviour
{
    [Header("Barks dialogues")]
    [SerializeField] private DialogueClass[] barksCountdownAlmostOver;
    [SerializeField] private DialogueClass[] barksStandingStill;
    [SerializeField] private DialogueClass[] barksPausingGame;

    [Header("Barks settings")]
    [SerializeField] private float delayBetweenCharacters = 0.1f;
    [SerializeField] private float delayBeforeCharactersVanish = 1.2f;
    [SerializeField] private float delayBeforeCanBarkAgain = 5f;
    [SerializeField] private float delayBetweenShakes = 0.08f;
    [SerializeField] private float deltaX = 10f;
    [SerializeField] private float deltaY = 10f;

    [Header("Barks UI in play")]
    [SerializeField] private TMP_Text barkTextDialogueGameplay;

    [Header("Barks UI in pause")]
    [SerializeField] private TMP_Text barkTextDialoguePause;
    private Coroutine barkPauseCoroutine;

    [Header("Barks fonts")]
    [SerializeField] private TMP_FontAsset barkFontMonster;
    [SerializeField] private TMP_FontAsset barkFontVector;

    public static BarksManager Instance { get; private set; }

    public bool Barking = false;

    private bool startTimer = false;

    private float timer = 0f;

    private void Start()
    {
        Timer.Instance.OnTimerAlmostOver += () => PlayBarkGameplay(barksCountdownAlmostOver, true);
        FPMovement.Instance.OnStationary += () => PlayBarkGameplay(barksStandingStill, false);
        PauseController.Instance.OnPauseStarted += () => PlayBarkPause(barksPausingGame);

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        barkTextDialogueGameplay.text = string.Empty;
        barkTextDialoguePause.text = string.Empty;
    }

    private void Update()
    {
        if (startTimer)
        {
            timer += Time.deltaTime;

            if (timer >= delayBeforeCanBarkAgain)
            {
                Barking = false;
                startTimer = false;
                timer = 0f;
            }
        }
    }

    private void PlayBarkPause(DialogueClass[] dialogues)
    {
        if (dialogues == null || dialogues.Length == 0) return;

        if (barkPauseCoroutine != null)
        {
            StopCoroutine(barkPauseCoroutine);
        }

        //HO PROVATO A COMMENTARE PER PROVA SO IDK
        //barkTextDialogueGameplay.text = string.Empty;
        //barkTextDialogueGameplay.color = new Color(barkTextDialogueGameplay.color.r, barkTextDialogueGameplay.color.g, barkTextDialogueGameplay.color.b, 1f);

        barkTextDialoguePause.text = string.Empty;
        barkTextDialoguePause.color = new Color(barkTextDialoguePause.color.r, barkTextDialoguePause.color.g, barkTextDialoguePause.color.b, 1f);

        int randomIndex = UnityEngine.Random.Range(0, dialogues.Length);

        if (dialogues[randomIndex].character.ToString() == "Monster")
        {
            barkTextDialoguePause.font = barkFontMonster;
        }
        else
        {
            barkTextDialoguePause.font = barkFontVector;
        }

        string dialogue = dialogues[randomIndex].dialogue;

        char[] dialogueCharacter = new char[dialogue.Length];

        for (int i = 0; i < dialogue.Length; i++)
        {
            dialogueCharacter[i] = dialogue[i];
        }

        barkPauseCoroutine = StartCoroutine(PlayCharacterPause(dialogueCharacter));

        if (dialogues[randomIndex].character.ToString() == "Monster")
        {
            StartCoroutine(ShakeText(barkTextDialoguePause.rectTransform.position,
                                    (dialogueCharacter.Length * delayBetweenCharacters) + delayBeforeCharactersVanish,
                                     barkTextDialoguePause,
                                     true));
        }
    }

    private void PlayBarkGameplay(DialogueClass[] dialogues, bool timeDialogue)
    {
        if (dialogues == null || dialogues.Length == 0) return;

        if (Barking && !timeDialogue) return;

        StopCoroutine(PlayCharacterGameplay(new char[0]));

        Barking = true;

        barkTextDialogueGameplay.text = string.Empty;
        barkTextDialogueGameplay.color = new Color(barkTextDialogueGameplay.color.r, barkTextDialogueGameplay.color.g, barkTextDialogueGameplay.color.b, 1f);

        int randomIndex = UnityEngine.Random.Range(0, dialogues.Length);

        if (dialogues[randomIndex].character.ToString() == "Monster")
        {
            barkTextDialogueGameplay.font = barkFontMonster;
        }
        else
        {
            barkTextDialogueGameplay.font = barkFontVector;
        }

        string dialogue = dialogues[randomIndex].dialogue;

        char[] dialogueCharacter = new char[dialogue.Length];

        for (int i = 0; i < dialogue.Length; i++)
        {
            dialogueCharacter[i] = dialogue[i];
        }

        StartCoroutine(PlayCharacterGameplay(dialogueCharacter));

        if (dialogues[randomIndex].character.ToString() == "Monster")
        {
            StartCoroutine(ShakeText(barkTextDialogueGameplay.rectTransform.position,
                                    (dialogueCharacter.Length * delayBetweenCharacters) + delayBeforeCharactersVanish,
                                     barkTextDialogueGameplay,
                                     false));
        }
    }

    IEnumerator PlayCharacterGameplay(char[] characters)
    {
        int index = 0;

        while (index < characters.Length)
        {
            // Display the character at the current index
            barkTextDialogueGameplay.text += characters[index];
            // Wait for the specified delay before displaying the next character
            yield return new WaitForSeconds(delayBetweenCharacters);
            index++;
        }

        yield return new WaitForSeconds(delayBeforeCharactersVanish);

        while (barkTextDialogueGameplay.color.a >= 0f)
        {
            barkTextDialogueGameplay.color = new Color(barkTextDialogueGameplay.color.r, barkTextDialogueGameplay.color.g, barkTextDialogueGameplay.color.b, barkTextDialogueGameplay.color.a - Time.deltaTime / 2);
            yield return null;
        }

        startTimer = true;

        yield return null;
    }

    IEnumerator PlayCharacterPause(char[] characters)
    {
        int index = 0;

        while (index < characters.Length)
        {
            // Display the character at the current index
            barkTextDialoguePause.text += characters[index];
            // Wait for the specified delay before displaying the next character
            yield return new WaitForSecondsRealtime(delayBetweenCharacters);
            index++;
        }

        yield return new WaitForSecondsRealtime(delayBeforeCharactersVanish);

        while (barkTextDialoguePause.color.a >= 0f)
        {
            barkTextDialoguePause.color = new Color(barkTextDialoguePause.color.r, barkTextDialoguePause.color.g, barkTextDialoguePause.color.b, barkTextDialoguePause.color.a - Time.deltaTime / 2);
            yield return null;
        }

        yield return null;
    }

    IEnumerator ShakeText(Vector2 initialPosition, float shakeDuration, TMP_Text textComponent, bool isThisPauseBark)
    {
        float timerDuration = 0f;
        float timerBetweenShakes = 0f;

        while (timerDuration < shakeDuration)
        {
            if (timerBetweenShakes >= delayBetweenShakes)
            {
                textComponent.rectTransform.position = new Vector2(initialPosition.x + UnityEngine.Random.Range(-deltaX, deltaX + 1),
                                                                   initialPosition.y + UnityEngine.Random.Range(-deltaY, deltaY + 1));
                timerBetweenShakes = 0f;
            }

            if (isThisPauseBark)
            {
                timerDuration += Time.unscaledDeltaTime;
                timerBetweenShakes += Time.unscaledDeltaTime;
            }
            else
            {
                timerDuration += Time.deltaTime;
                timerBetweenShakes += Time.deltaTime;
            }

        }

        textComponent.rectTransform.position = initialPosition;

        yield return null;
    }
}