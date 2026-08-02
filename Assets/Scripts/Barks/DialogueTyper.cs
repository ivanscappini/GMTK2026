using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Shared helpers for the "bark"/dialogue typewriter effect used by BarksManager, BarkGameOver,
/// BarkMainMenu, BarkNewArea and DisappearingText. These five scripts had near-identical copies
/// of this logic (font lookup, character-by-character typing, fade out, screen shake, and -
/// for BarkGameOver/BarkNewArea - picking a random "exchange" by DialogueClass.dialogueIndex),
/// with small per-script differences in timing (real vs scaled time, fade speed) that were easy
/// to lose track of across five separate copies. Centralising it means those differences are now
/// explicit parameters instead of accidental copy-paste drift, and now that DialogueClass
/// supports per-line overrides, every script that types dialogue picks them up automatically
/// instead of needing its own support added by hand.
/// </summary>
public static class DialogueTyper
{
    /// <summary>
    /// Font for a line: a per-line override (DialogueClass.overrideFont) takes priority,
    /// otherwise falls back to the usual character-based lookup. hqFont may be left null, in
    /// which case Character.HQ falls back to vectorFont (matches BarksManager's original
    /// behaviour, which never had a dedicated HQ font configured).
    /// </summary>
    public static TMP_FontAsset GetFontForLine(DialogueClass line, TMP_FontAsset monsterFont, TMP_FontAsset vectorFont, TMP_FontAsset hqFont = null)
    {
        if (line.overrideFont && line.fontOverride != null)
            return line.fontOverride;

        switch (line.character)
        {
            case Character.Monster: return monsterFont;
            case Character.Vector: return vectorFont;
            default: return hqFont != null ? hqFont : vectorFont;
        }
    }

    /// <summary>
    /// Applies a line's optional color/size overrides to a text component. Font is handled
    /// separately by GetFontForLine, since callers assign .font themselves alongside their own
    /// setup - call this after assigning the font (and after any other default styling) so a
    /// per-line override always wins last.
    /// </summary>
    public static void ApplyLineOverrides(TMP_Text text, DialogueClass line)
    {
        if (line.overrideColor)
            text.color = new Color(line.colorOverride.r, line.colorOverride.g, line.colorOverride.b, text.color.a);

        if (line.overrideFontSize)
            text.fontSize = line.fontSizeOverride;
    }

    /// <summary>Types "dialogue" into text.text one character at a time.</summary>
    public static IEnumerator TypeText(TMP_Text text, string dialogue, float delayBetweenCharacters, bool useUnscaledTime)
    {
        for (int i = 0; i < dialogue.Length; i++)
        {
            text.text += dialogue[i];
            yield return useUnscaledTime ? new WaitForSecondsRealtime(delayBetweenCharacters) : new WaitForSeconds(delayBetweenCharacters);
        }
    }

    /// <summary>Fades text's alpha down to 0. fadeSpeed is alpha-per-second (1 = fades out over roughly 1 second).</summary>
    public static IEnumerator FadeOut(TMP_Text text, float fadeSpeed, bool useUnscaledTime)
    {
        while (text.color.a > 0f)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Color c = text.color;
            text.color = new Color(c.r, c.g, c.b, c.a - dt * fadeSpeed);
            yield return null;
        }
    }

    /// <summary>Fades two text boxes out together, in lockstep - used by the two-speaker exchanges in BarkGameOver/BarkNewArea.</summary>
    public static IEnumerator FadeOutTogether(TMP_Text a, TMP_Text b, float fadeSpeed, bool useUnscaledTime)
    {
        while (a.color.a > 0f)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            a.color = new Color(a.color.r, a.color.g, a.color.b, a.color.a - dt * fadeSpeed);
            b.color = new Color(b.color.r, b.color.g, b.color.b, b.color.a - dt * fadeSpeed);
            yield return null;
        }
    }

    /// <summary>Randomly jitters text's anchored position for shakeDuration seconds, then snaps back to where it started.</summary>
    public static IEnumerator Shake(TMP_Text text, Vector2 initialAnchoredPosition, float shakeDuration, float delayBetweenShakes, float deltaX, float deltaY, bool useUnscaledTime)
    {
        float timerDuration = 0f;
        float timerBetweenShakes = 0f;

        while (timerDuration < shakeDuration)
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (timerBetweenShakes >= delayBetweenShakes)
            {
                text.rectTransform.anchoredPosition = new Vector2(
                    initialAnchoredPosition.x + Random.Range(-deltaX, deltaX + 1),
                    initialAnchoredPosition.y + Random.Range(-deltaY, deltaY + 1));
                timerBetweenShakes = 0f;
            }

            timerDuration += dt;
            timerBetweenShakes += dt;

            // Without this yield the whole shake duration would be consumed synchronously
            // within a single frame instead of animating over time (a bug already found and
            // fixed in the individual bark scripts before this refactor).
            yield return null;
        }

        text.rectTransform.anchoredPosition = initialAnchoredPosition;
    }

    /// <summary>
    /// Highest DialogueClass.dialogueIndex present in "dialogues" - the valid [0, max] range to
    /// randomize an "exchange" from. Using dialogues.Length instead would be wrong whenever an
    /// exchange has more than one line, since Length counts individual lines, not exchanges.
    /// </summary>
    public static int GetMaxDialogueIndex(DialogueClass[] dialogues)
    {
        int max = 0;
        foreach (var d in dialogues)
            if (d.dialogueIndex > max) max = d.dialogueIndex;
        return max;
    }

    /// <summary>
    /// Finds the first two lines in "dialogues" whose dialogueIndex equals "index" (first
    /// speaker, then reply). Only the first two matches are used - a third line sharing the
    /// same index is silently ignored, since callers only have two text boxes to show it in.
    /// Either or both outputs can be null if fewer than two lines share that index.
    /// </summary>
    public static void FindExchange(DialogueClass[] dialogues, int index, out DialogueClass first, out DialogueClass second)
    {
        first = null;
        second = null;

        foreach (var line in dialogues)
        {
            if (line.dialogueIndex != index) continue;

            if (first == null) first = line;
            else if (second == null) second = line;
        }
    }
}