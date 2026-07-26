using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Hermes;

/// <summary>
/// Central manager for the recurring gameplay barks (countdown warning, standing
/// still too long) and the bark shown while the game is paused. A singleton so other
/// systems (Timer, FPMovement, PauseController) can trigger a bark by raising an event.
/// </summary>
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
    // When a gameplay bark is interrupted by pause, this coroutine fades the
    // partially-typed gameplay text using unscaled time so it still animates
    // while the game is paused.
    private Coroutine gameplayInterruptCoroutine;

    // Tracks the currently-running gameplay typing/fade coroutine so a new bark can
    // actually cancel the previous one. StopCoroutine needs a reference to the exact
    // running instance - passing it a brand-new IEnumerator (as the original code did)
    // stops nothing, since that IEnumerator was never the one handed to StartCoroutine.
    private Coroutine barkGameplayCoroutine;

    [Header("Barks fonts")]
    [SerializeField] private TMP_FontAsset barkFontMonster;
    [SerializeField] private TMP_FontAsset barkFontVector;

    public static BarksManager Instance { get; private set; }

    public bool Barking = false;

    private bool startTimer = false;
    private float timer = 0f;

    private void Start()
    {
        // FIX: enforce the singleton *before* subscribing to any events. The original
        // subscribed first and checked for a duplicate afterward, so a duplicate
        // instance would briefly register handlers on Timer/FPMovement/PauseController
        // before being destroyed - Destroy() doesn't happen until end of frame, so
        // those handlers could still fire (and would then throw once this object is
        // actually gone).
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        Timer.Instance.OnTimerAlmostOver += HandleTimerAlmostOver;
        FPMovement.Instance.OnStationary += HandleStationary;
        PauseController.Instance.OnPauseStarted += HandlePauseStarted;

        barkTextDialogueGameplay.text = string.Empty;
        barkTextDialoguePause.text = string.Empty;
    }

    private void OnDestroy()
    {
        // FIX: the original subscribed with inline lambdas, which can never be
        // unsubscribed (each lambda is a new delegate instance). Using named handler
        // methods instead lets us clean up here, so a destroyed BarksManager doesn't
        // stay registered on these (likely longer-lived) singletons.
        if (Timer.Instance != null) Timer.Instance.OnTimerAlmostOver -= HandleTimerAlmostOver;
        if (FPMovement.Instance != null) FPMovement.Instance.OnStationary -= HandleStationary;
        if (PauseController.Instance != null) PauseController.Instance.OnPauseStarted -= HandlePauseStarted;
    }

    private void HandleTimerAlmostOver() => PlayBarkGameplay(barksCountdownAlmostOver, true);
    private void HandleStationary() => PlayBarkGameplay(barksStandingStill, false);
    private void HandlePauseStarted() => PlayBarkPause(barksPausingGame);

    private void Update()
    {
        // Cooldown after a gameplay bark finishes, before another (non-time-critical) one can play.
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

    /// <summary>
    /// Font lookup for a speaking character. Note this manager only has two fonts
    /// configured (Monster and Vector) - unlike BarkGameOver/BarkNewArea, there's no
    /// separate HQ font here, so an HQ line falls back to the Vector font, same as the original.
    /// </summary>
    private TMP_FontAsset GetFontForCharacter(Character character)
    {
        return character == Character.Monster ? barkFontMonster : barkFontVector;
    }

    private void PlayBarkPause(DialogueClass[] dialogues)
    {
        if (dialogues == null || dialogues.Length == 0) return;
        // If a gameplay bark is currently typing, interrupt it so the line is
        // cut off (e.g. "YOU CANT RUN FO—") and then fade away while paused.
        if (barkGameplayCoroutine != null)
        {
            // Stop the typing coroutine and clear the reference. Keep the
            // currently-displayed partial text so the pause bark can reply to it.
            StopCoroutine(barkGameplayCoroutine);
            barkGameplayCoroutine = null;

            // Ensure any previous interrupt fade is stopped before starting a new one.
            if (gameplayInterruptCoroutine != null)
            {
                StopCoroutine(gameplayInterruptCoroutine);
            }
            gameplayInterruptCoroutine = StartCoroutine(InterruptGameplayAndFade(barkTextDialogueGameplay, 0.25f, 0.6f));
        }

        if (barkPauseCoroutine != null)
        {
            StopCoroutine(barkPauseCoroutine);
        }

        barkTextDialoguePause.text = string.Empty;
        barkTextDialoguePause.color = new Color(barkTextDialoguePause.color.r, barkTextDialoguePause.color.g, barkTextDialoguePause.color.b, 1f);

        int randomIndex = UnityEngine.Random.Range(0, dialogues.Length);
        DialogueClass chosen = dialogues[randomIndex];

        barkTextDialoguePause.font = GetFontForCharacter(chosen.character);

        char[] dialogueCharacter = chosen.dialogue.ToCharArray();

        barkPauseCoroutine = StartCoroutine(PlayCharacterPause(dialogueCharacter));

        if (chosen.character == Character.Monster)
        {
            StartCoroutine(ShakeText(barkTextDialoguePause.rectTransform.anchoredPosition,
                                    (dialogueCharacter.Length * delayBetweenCharacters) + delayBeforeCharactersVanish,
                                     barkTextDialoguePause,
                                     true));
        }
    }

    private void PlayBarkGameplay(DialogueClass[] dialogues, bool timeDialogue)
    {
        if (dialogues == null || dialogues.Length == 0) return;
        if (Barking && !timeDialogue) return;

        // FIX: was `StopCoroutine(PlayCharacterGameplay(new char[0]))`, which stops
        // nothing (see the field comment above). Now stops the actual running instance.
        if (barkGameplayCoroutine != null)
        {
            StopCoroutine(barkGameplayCoroutine);
        }

        Barking = true;

        barkTextDialogueGameplay.text = string.Empty;
        barkTextDialogueGameplay.color = new Color(barkTextDialogueGameplay.color.r, barkTextDialogueGameplay.color.g, barkTextDialogueGameplay.color.b, 1f);

        int randomIndex = UnityEngine.Random.Range(0, dialogues.Length);
        DialogueClass chosen = dialogues[randomIndex];

        barkTextDialogueGameplay.font = GetFontForCharacter(chosen.character);

        char[] dialogueCharacter = chosen.dialogue.ToCharArray();

        barkGameplayCoroutine = StartCoroutine(PlayCharacterGameplay(dialogueCharacter));

        if (chosen.character == Character.Monster)
        {
            StartCoroutine(ShakeText(barkTextDialogueGameplay.rectTransform.anchoredPosition,
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
            barkTextDialogueGameplay.text += characters[index];
            yield return new WaitForSeconds(delayBetweenCharacters);
            index++;
        }

        yield return new WaitForSeconds(delayBeforeCharactersVanish);

        while (barkTextDialogueGameplay.color.a > 0f)
        {
            barkTextDialogueGameplay.color = new Color(barkTextDialogueGameplay.color.r, barkTextDialogueGameplay.color.g, barkTextDialogueGameplay.color.b, barkTextDialogueGameplay.color.a - Time.deltaTime / 2);
            yield return null;
        }

        startTimer = true;
    }

    IEnumerator PlayCharacterPause(char[] characters)
    {
        int index = 0;
        while (index < characters.Length)
        {
            barkTextDialoguePause.text += characters[index];
            yield return new WaitForSecondsRealtime(delayBetweenCharacters);
            index++;
        }

        yield return new WaitForSecondsRealtime(delayBeforeCharactersVanish);

        while (barkTextDialoguePause.color.a > 0f)
        {
            barkTextDialoguePause.color = new Color(barkTextDialoguePause.color.r, barkTextDialoguePause.color.g, barkTextDialoguePause.color.b, barkTextDialoguePause.color.a - Time.deltaTime / 2);
            yield return null;
        }
    }

    /// <summary>
    /// Fade out a partially-typed gameplay bark while the game is paused.
    /// Uses unscaled time so it progresses with Time.timeScale == 0.
    /// </summary>
    IEnumerator InterruptGameplayAndFade(TMP_Text textComponent, float visibleDuration, float fadeDuration)
    {
        // Keep the current text visible for a short unscaled duration
        float t = 0f;
        while (t < visibleDuration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Then fade its alpha out over fadeDuration using unscaled time
        float startAlpha = textComponent.color.a;
        float elapsed = 0f;
        while (elapsed < fadeDuration && textComponent.color.a > 0f)
        {
            elapsed += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / fadeDuration));
            textComponent.color = new Color(textComponent.color.r, textComponent.color.g, textComponent.color.b, a);
            yield return null;
        }

        textComponent.color = new Color(textComponent.color.r, textComponent.color.g, textComponent.color.b, 0f);
        gameplayInterruptCoroutine = null;
    }

    IEnumerator ShakeText(Vector2 initialAnchoredPosition, float shakeDuration, TMP_Text textComponent, bool isThisPauseBark)
    {
        float timerDuration = 0f;
        float timerBetweenShakes = 0f;

        while (timerDuration < shakeDuration)
        {
            if (timerBetweenShakes >= delayBetweenShakes)
            {
                textComponent.rectTransform.anchoredPosition = new Vector2(initialAnchoredPosition.x + UnityEngine.Random.Range(-deltaX, deltaX + 1),
                                                                           initialAnchoredPosition.y + UnityEngine.Random.Range(-deltaY, deltaY + 1));
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

            // IMPORTANT FIX: this yield was missing in the original. Without it, the
            // whole shake duration is consumed synchronously within a single frame -
            // the shake never actually renders, it just freezes for one tick and
            // immediately snaps back to initial position.
            yield return null;
        }

        textComponent.rectTransform.anchoredPosition = initialAnchoredPosition;
    }
}