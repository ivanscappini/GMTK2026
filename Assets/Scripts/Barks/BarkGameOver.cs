using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Plays a short two-character "bark" exchange on the game-over screen.
/// Picks a random exchange (a group of lines sharing the same dialogueIndex),
/// types it out one character at a time, then fades both lines out.
/// If "Monster" is one of the speakers, its line also shakes on screen while typing.
///
/// The typing/fading/shaking/font-lookup/exchange-picking logic now lives in the shared
/// DialogueTyper utility (also used by BarksManager, BarkMainMenu, BarkNewArea and
/// DisappearingText) instead of being duplicated here.
/// </summary>
public class BarkGameOver : MonoBehaviour
{
    [Header("Dialogue Game Over")]
    [Tooltip("All possible game-over bark lines. Lines sharing the same dialogueIndex form one exchange (first speaker, then reply).")]
    [SerializeField] private DialogueClass[] dialoguesGameOver;

    [Header("Dialogues UI")]
    [SerializeField] private TMP_Text barkTextDialogueFirst;
    [SerializeField] private TMP_Text barkTextDialogueSecond;

    [Header("Dialogues settings")]
    [SerializeField] private float delayBetweenCharacters = 0.1f;
    [SerializeField] private float delayBetweenDialogues = 1f;
    [SerializeField] private float delayBeforeCharactersVanish = 1.2f;
    [SerializeField] private float delayBetweenShakes = 0.08f;
    [SerializeField] private float deltaX = 0.7f;
    [SerializeField] private float deltaY = 0.7f;

    [Header("Barks fonts")]
    [SerializeField] private TMP_FontAsset barkFontMonster;
    [SerializeField] private TMP_FontAsset barkFontVector;
    [SerializeField] private TMP_FontAsset barkFontHQ;

    private void Start()
    {
        barkTextDialogueFirst.text = "";
        barkTextDialogueSecond.text = "";

        if (dialoguesGameOver == null || dialoguesGameOver.Length == 0) return;

        // Pick a random exchange by looking at the highest dialogueIndex actually
        // present, instead of assuming it happens to sit in the last array slot.
        int maxIndex = DialogueTyper.GetMaxDialogueIndex(dialoguesGameOver);
        PlayDialogue(dialoguesGameOver, Random.Range(0, maxIndex + 1)); // +1: Random.Range's upper bound is exclusive
    }

    /// <summary>
    /// Finds the exchange at "index" (first speaker, then reply) and starts typing + shaking.
    /// Note: only the first two matching lines are used - a third line sharing the same index
    /// would be silently ignored, since there are only two text boxes.
    /// </summary>
    private void PlayDialogue(DialogueClass[] barksToPlay, int index)
    {
        Debug.Log("Playing dialogue with index: " + index);

        DialogueTyper.FindExchange(barksToPlay, index, out var first, out var second);

        if (first != null)
        {
            barkTextDialogueFirst.font = DialogueTyper.GetFontForLine(first, barkFontMonster, barkFontVector, barkFontHQ);
            DialogueTyper.ApplyLineOverrides(barkTextDialogueFirst, first);
        }
        if (second != null)
        {
            barkTextDialogueSecond.font = DialogueTyper.GetFontForLine(second, barkFontMonster, barkFontVector, barkFontHQ);
            DialogueTyper.ApplyLineOverrides(barkTextDialogueSecond, second);
        }

        StartCoroutine(PlayCharacter(first?.dialogue ?? "", second?.dialogue ?? ""));

        // Only the Monster's line gets the shake effect.
        if (first != null && first.character == Character.Monster)
        {
            StartCoroutine(DialogueTyper.Shake(barkTextDialogueFirst, barkTextDialogueFirst.rectTransform.anchoredPosition,
                                               (first.dialogue.Length * delayBetweenCharacters) + delayBeforeCharactersVanish,
                                               delayBetweenShakes, deltaX, deltaY, useUnscaledTime: false));
        }
        else if (second != null && second.character == Character.Monster)
        {
            float firstLength = first != null ? first.dialogue.Length : 0;
            StartCoroutine(DialogueTyper.Shake(barkTextDialogueSecond, barkTextDialogueSecond.rectTransform.anchoredPosition,
                                               (firstLength * delayBetweenCharacters) + (second.dialogue.Length * delayBetweenCharacters) + delayBeforeCharactersVanish * 2,
                                               delayBetweenShakes, deltaX, deltaY, useUnscaledTime: false));
        }
    }

    /// <summary>
    /// Types the first line, then (if there is one) the reply, then fades both
    /// text boxes out together.
    /// </summary>
    IEnumerator PlayCharacter(string firstDialogue, string secondDialogue)
    {
        yield return DialogueTyper.TypeText(barkTextDialogueFirst, firstDialogue, delayBetweenCharacters, useUnscaledTime: false);

        yield return new WaitForSeconds(delayBetweenDialogues);

        if (secondDialogue.Length > 0)
        {
            yield return DialogueTyper.TypeText(barkTextDialogueSecond, secondDialogue, delayBetweenCharacters, useUnscaledTime: false);

            yield return new WaitForSeconds(delayBetweenDialogues);
        }

        yield return DialogueTyper.FadeOutTogether(barkTextDialogueFirst, barkTextDialogueSecond, fadeSpeed: 1f, useUnscaledTime: false);
    }
}