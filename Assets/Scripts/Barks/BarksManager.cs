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

    [Header("Barks UI in play")]
    [SerializeField] private TMP_Text barkTextCharGameplay;
    [SerializeField] private TMP_Text barkTextDialogueGameplay;

    [Header("Barks UI in pause")]
    [SerializeField] private TMP_Text barkTextCharPause;
    [SerializeField] private TMP_Text barkTextDialoguePause;

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

        barkTextCharGameplay.text = string.Empty;
        barkTextDialogueGameplay.text = string.Empty;
        barkTextCharPause.text = string.Empty;
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

        //StopAllCoroutines();

        barkTextDialogueGameplay.text = string.Empty;
        barkTextDialogueGameplay.color = new Color(barkTextDialogueGameplay.color.r, barkTextDialogueGameplay.color.g, barkTextDialogueGameplay.color.b, 1f);
        barkTextCharGameplay.text = string.Empty;
        barkTextCharGameplay.color = new Color(barkTextCharGameplay.color.r, barkTextCharGameplay.color.g, barkTextCharGameplay.color.b, 1f);

        barkTextCharPause.text = string.Empty;
        barkTextCharPause.color = new Color(barkTextCharPause.color.r, barkTextCharPause.color.g, barkTextCharPause.color.b, 1f);
        barkTextDialoguePause.text = string.Empty;
        barkTextDialoguePause.color = new Color(barkTextDialoguePause.color.r, barkTextDialoguePause.color.g, barkTextDialoguePause.color.b, 1f);

        int randomIndex = UnityEngine.Random.Range(0, dialogues.Length);

        string dialogue = dialogues[randomIndex].dialogue;

        char[] dialogueCharacter = new char[dialogue.Length];

        for (int i = 0; i < dialogue.Length; i++)
        {
            dialogueCharacter[i] = dialogue[i];
        }

        barkTextCharPause.text = dialogues[randomIndex].character.ToString() + ":";

        StartCoroutine(PlayCharacterPause(dialogueCharacter));
    }

    private void PlayBarkGameplay(DialogueClass[] dialogues, bool timeDialogue)
    {
        if (dialogues == null || dialogues.Length == 0) return;

        if (Barking && !timeDialogue) return;

        Barking = true;

        barkTextCharGameplay.text = string.Empty;
        barkTextCharGameplay.color = new Color(barkTextCharGameplay.color.r, barkTextCharGameplay.color.g, barkTextCharGameplay.color.b, 1f);
        barkTextDialogueGameplay.text = string.Empty;
        barkTextDialogueGameplay.color = new Color(barkTextDialogueGameplay.color.r, barkTextDialogueGameplay.color.g, barkTextDialogueGameplay.color.b, 1f);

        int randomIndex = UnityEngine.Random.Range(0, dialogues.Length);

        string dialogue = dialogues[randomIndex].dialogue;

        char[] dialogueCharacter = new char[dialogue.Length];

        for (int i = 0; i < dialogue.Length; i++)
        {
            dialogueCharacter[i] = dialogue[i];
        }

        barkTextCharGameplay.text = dialogues[randomIndex].character.ToString() + ":";

        StartCoroutine(PlayCharacterGameplay(dialogueCharacter));
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
            barkTextCharGameplay.color = new Color(barkTextCharGameplay.color.r, barkTextCharGameplay.color.g, barkTextCharGameplay.color.b, barkTextCharGameplay.color.a - Time.deltaTime / 2);
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
            barkTextCharPause.color = new Color(barkTextCharPause.color.r, barkTextCharPause.color.g, barkTextCharPause.color.b, barkTextCharPause.color.a - Time.deltaTime / 2);
            yield return null;
        }

        Barking = false;

        yield return null;
    }
}