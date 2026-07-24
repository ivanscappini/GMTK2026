using Hermes;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class BarksManager : MonoBehaviour
{
    [Header("Barks dialogues")]
    //[SerializeField] private string[] barksGameOver;
    [SerializeField] private string[] barksNewArea;
    [SerializeField] private string[] barksCountdownAlmostOver;
    [SerializeField] private string[] barksStandingStill;
    [SerializeField] private string[] barksPausingGame;

    [SerializeField] private float delayBetweenCharacters = 0.1f;
    [SerializeField] private float delayBeforeCharactersVanish = 1.2f;
    [SerializeField] private float delayBeforeCanBarkAgain = 5f;

    [SerializeField] private TMP_Text barkTextGameplay;
    [SerializeField] private TMP_Text barkTextPause;

    private bool barking = false;
    private bool startTimer = false;

    private float timer = 0f;

    private void Start()
    {
        // Here we should subscribe to the events for every occasion
        FPMovement.Instance.OnStationary += () => PlayBarkGameplay(barksStandingStill);
    }

    private void Update()
    {
        if (startTimer)
        {
            timer += Time.deltaTime;

            if (timer >= delayBeforeCanBarkAgain)
            {
                barking = false;
                startTimer = false;
                timer = 0f;
            }
        }
    }

    private void PlayBarkPause(string[] barks)
    {
        if (barks == null || barks.Length == 0) return;

        //if (barking) return;

        barking = true;

        StopCoroutine(PlayCharacter(new char[0])); // Stop any ongoing bark coroutine

        barkTextGameplay.text = string.Empty;
        barkTextGameplay.color = new Color(barkTextGameplay.color.r, barkTextGameplay.color.g, barkTextGameplay.color.b, 1f);

        barkTextPause.text = string.Empty;
        barkTextPause.color = new Color(barkTextPause.color.r, barkTextPause.color.g, barkTextPause.color.b, 1f);

        string bark = barks[UnityEngine.Random.Range(0, barks.Length)];

        char[] barkCharacter = new char[bark.Length];

        for (int i = 0; i < bark.Length; i++)
        {
            barkCharacter[i] = bark[i];
        }

        StartCoroutine(PlayCharacter(barkCharacter));
    }

    private void PlayBarkGameplay(string[] barks)
    {
        if (barks == null || barks.Length == 0) return;

        if (barking) return;

        barking = true;

        barkTextGameplay.text = string.Empty;
        barkTextGameplay.color = new Color(barkTextGameplay.color.r, barkTextGameplay.color.g, barkTextGameplay.color.b, 1f);

        string bark = barks[UnityEngine.Random.Range(0, barks.Length)];

        char[] barkCharacter = new char[bark.Length];

        for (int i = 0; i < bark.Length; i++)
        {
            barkCharacter[i] = bark[i];
        }

        StartCoroutine(PlayCharacter(barkCharacter));
    }

    IEnumerator PlayCharacter(char[] characters)
    {
        int index = 0;

        while (index < characters.Length)
        {
            // Display the character at the current index
            barkTextGameplay.text += characters[index];
            // Wait for the specified delay before displaying the next character
            yield return new WaitForSeconds(delayBetweenCharacters);
            index++;
        }

        yield return new WaitForSeconds(delayBeforeCharactersVanish);

        while (barkTextGameplay.color.a >= 0f)
        {
            barkTextGameplay.color = new Color(barkTextGameplay.color.r, barkTextGameplay.color.g, barkTextGameplay.color.b, barkTextGameplay.color.a - Time.deltaTime / 2);
            yield return null;
        }

        startTimer = true;

        yield return null;
    }
}