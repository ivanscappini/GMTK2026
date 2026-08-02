using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Hermes;

/// <summary>
/// Central manager for the recurring gameplay barks (countdown warning, standing
/// still too long) and the bark shown while the game is paused. A singleton so other
/// systems (Timer, FPMovement, PauseController) can trigger a bark by raising an event.
///
/// The actual typing/fading/shaking/font-lookup logic now lives in the shared DialogueTyper
/// utility (also used by BarkGameOver, BarkMainMenu, BarkNewArea and DisappearingText) instead
/// of being duplicated here.
/// </summary>
public class BarksGameplayManager : MonoBehaviour
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
    // When a gameplay bark is interrupted (e.g. by tutorial/dialog), this
    // coroutine fades the partially-typed gameplay text using unscaled time
    // so it still animates while the game is paused.
    private Coroutine gameplayInterruptCoroutine;

    // Tracks the currently-running gameplay typing/fade coroutine so a new bark can
    // actually cancel the previous one. StopCoroutine needs a reference to the exact
    // running instance - passing it a brand-new IEnumerator (as the original code did)
    // stops nothing, since that IEnumerator was never the one handed to StartCoroutine.
    private Coroutine barkGameplayCoroutine;

    [Header("Barks fonts")]
    [SerializeField] private TMP_FontAsset barkFontMonster;
    [SerializeField] private TMP_FontAsset barkFontVector;

    public static BarksGameplayManager Instance { get; private set; }

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

    private void PlayBarkPause(DialogueClass[] dialogues)
    {
        if (dialogues == null || dialogues.Length == 0) return;

        if (barkPauseCoroutine != null)
        {
            StopCoroutine(barkPauseCoroutine);
        }

        barkTextDialoguePause.text = string.Empty;
        barkTextDialoguePause.color = new Color(barkTextDialoguePause.color.r, barkTextDialoguePause.color.g, barkTextDialoguePause.color.b, 1f);

        int randomIndex = UnityEngine.Random.Range(0, dialogues.Length);
        DialogueClass chosen = dialogues[randomIndex];

        barkTextDialoguePause.font = DialogueTyper.GetFontForLine(chosen, barkFontMonster, barkFontVector);
        DialogueTyper.ApplyLineOverrides(barkTextDialoguePause, chosen);

        barkPauseCoroutine = StartCoroutine(PlayCharacterPause(chosen));

        if (chosen.character == Character.Monster)
        {
            StartCoroutine(DialogueTyper.Shake(barkTextDialoguePause, barkTextDialoguePause.rectTransform.anchoredPosition,
                                    (chosen.dialogue.Length * delayBetweenCharacters) + delayBeforeCharactersVanish,
                                     delayBetweenShakes, deltaX, deltaY, useUnscaledTime: true));
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

        barkTextDialogueGameplay.font = DialogueTyper.GetFontForLine(chosen, barkFontMonster, barkFontVector);
        DialogueTyper.ApplyLineOverrides(barkTextDialogueGameplay, chosen);

        barkGameplayCoroutine = StartCoroutine(PlayCharacterGameplay(chosen));

        if (chosen.character == Character.Monster)
        {
            StartCoroutine(DialogueTyper.Shake(barkTextDialogueGameplay, barkTextDialogueGameplay.rectTransform.anchoredPosition,
                                    (chosen.dialogue.Length * delayBetweenCharacters) + delayBeforeCharactersVanish,
                                     delayBetweenShakes, deltaX, deltaY, useUnscaledTime: false));
        }
    }

    IEnumerator PlayCharacterGameplay(DialogueClass line)
    {
        yield return DialogueTyper.TypeText(barkTextDialogueGameplay, line.dialogue, delayBetweenCharacters, useUnscaledTime: false);

        yield return new WaitForSeconds(delayBeforeCharactersVanish);

        yield return DialogueTyper.FadeOut(barkTextDialogueGameplay, fadeSpeed: 0.5f, useUnscaledTime: false);

        startTimer = true;
    }

    IEnumerator PlayCharacterPause(DialogueClass line)
    {
        yield return DialogueTyper.TypeText(barkTextDialoguePause, line.dialogue, delayBetweenCharacters, useUnscaledTime: true);

        yield return new WaitForSecondsRealtime(delayBeforeCharactersVanish);

        // BUGFIX: this fade used to run on Time.deltaTime (scaled), which is 0 while the game
        // is actually paused (Time.timeScale = 0) - so the pause bark would type out correctly
        // but then just sit there at full opacity for as long as the game stayed paused, only
        // actually fading once the player unpaused. Now uses unscaled time, like the rest of
        // this coroutine (and like its own Shake call above already did), so it fades out while
        // still paused, as intended.
        yield return DialogueTyper.FadeOut(barkTextDialoguePause, fadeSpeed: 0.5f, useUnscaledTime: true);
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

    /// <summary>
    /// Public helper to immediately interrupt any active gameplay bark and
    /// start the unscaled fade. Safe to call from other systems (tutorial)
    /// to ensure a partial line fades out when the game is paused.
    /// </summary>
    public void FadeActiveGameplayBark(float visibleDuration = 0.25f, float fadeDuration = 0.6f)
    {
        if (barkGameplayCoroutine != null)
        {
            StopCoroutine(barkGameplayCoroutine);
            barkGameplayCoroutine = null;

            if (gameplayInterruptCoroutine != null)
                StopCoroutine(gameplayInterruptCoroutine);

            gameplayInterruptCoroutine = StartCoroutine(InterruptGameplayAndFade(barkTextDialogueGameplay, visibleDuration, fadeDuration));
        }
    }
}