using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Plays a short two-character "bark" exchange on the game-over screen.
/// Picks a random exchange (a group of lines sharing the same dialogueIndex),
/// types it out one character at a time, then fades both lines out.
/// If "Monster" is one of the speakers, its line also shakes on screen while typing.
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
        int maxIndex = GetMaxDialogueIndex(dialoguesGameOver);
        PlayDialogue(dialoguesGameOver, Random.Range(0, maxIndex + 1)); // +1: Random.Range's upper bound is exclusive
    }

    /// <summary>
    /// Highest dialogueIndex used across the given lines - defines the valid
    /// [0, max] range to randomize an exchange from.
    /// </summary>
    private int GetMaxDialogueIndex(DialogueClass[] dialogues)
    {
        int max = 0;
        foreach (var d in dialogues)
        {
            if (d.dialogueIndex > max) max = d.dialogueIndex;
        }
        return max;
    }

    /// <summary>
    /// Font lookup for a speaking character, centralised so it's only written once.
    /// </summary>
    private TMP_FontAsset GetFontForCharacter(Character character)
    {
        switch (character)
        {
            case Character.Monster: return barkFontMonster;
            case Character.Vector: return barkFontVector;
            default: return barkFontHQ;
        }
    }

    /// <summary>
    /// Finds the lines belonging to "index" (first match = first speaker,
    /// second match = reply) and starts typing + shaking.
    /// Note: only the first two matching lines are used - a third line sharing
    /// the same index would be silently ignored, since there are only two text boxes.
    /// </summary>
    private void PlayDialogue(DialogueClass[] barksToPlay, int index)
    {
        Debug.Log("Playing dialogue with index: " + index);

        string dialogueFirst = "";
        string dialogueSecond = "";
        Character firstCharacter = default;
        Character secondCharacter = default;
        bool foundFirst = false;

        foreach (var bark in barksToPlay)
        {
            if (bark.dialogueIndex != index) continue;

            if (!foundFirst)
            {
                dialogueFirst = bark.dialogue;
                firstCharacter = bark.character;
                barkTextDialogueFirst.font = GetFontForCharacter(bark.character);
                foundFirst = true;
            }
            else
            {
                dialogueSecond = bark.dialogue;
                secondCharacter = bark.character;
                barkTextDialogueSecond.font = GetFontForCharacter(bark.character);
            }
        }

        StartCoroutine(PlayCharacter(dialogueFirst, dialogueSecond));

        // Only the Monster's line gets the shake effect.
        if (firstCharacter == Character.Monster)
        {
            StartCoroutine(ShakeText(barkTextDialogueFirst.rectTransform.anchoredPosition,
                                     (dialogueFirst.Length * delayBetweenCharacters) + delayBeforeCharactersVanish,
                                     barkTextDialogueFirst));
        }
        else if (secondCharacter == Character.Monster)
        {
            StartCoroutine(ShakeText(barkTextDialogueSecond.rectTransform.anchoredPosition,
                                     (dialogueFirst.Length * delayBetweenCharacters) + (dialogueSecond.Length * delayBetweenCharacters) + delayBeforeCharactersVanish * 2,
                                     barkTextDialogueSecond));
        }
    }

    /// <summary>
    /// Types the first line, then (if there is one) the reply, then fades both
    /// text boxes out together.
    /// </summary>
    IEnumerator PlayCharacter(string firstDialogue, string secondDialogue)
    {
        int index = 0;
        while (index < firstDialogue.Length)
        {
            barkTextDialogueFirst.text += firstDialogue[index];
            yield return new WaitForSeconds(delayBetweenCharacters);
            index++;
        }

        yield return new WaitForSeconds(delayBetweenDialogues);

        if (secondDialogue.Length > 0)
        {
            index = 0;
            while (index < secondDialogue.Length)
            {
                barkTextDialogueSecond.text += secondDialogue[index];
                yield return new WaitForSeconds(delayBetweenCharacters);
                index++;
            }

            yield return new WaitForSeconds(delayBetweenDialogues);
        }

        while (barkTextDialogueFirst.color.a > 0)
        {
            barkTextDialogueFirst.color = new Color(barkTextDialogueFirst.color.r, barkTextDialogueFirst.color.g, barkTextDialogueFirst.color.b, barkTextDialogueFirst.color.a - Time.deltaTime);
            barkTextDialogueSecond.color = new Color(barkTextDialogueSecond.color.r, barkTextDialogueSecond.color.g, barkTextDialogueSecond.color.b, barkTextDialogueSecond.color.a - Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>
    /// Randomly jitters the text's position for "shakeDuration" seconds, then
    /// snaps it back to where it started.
    /// </summary>
    IEnumerator ShakeText(Vector3 initialAnchoredPosition, float shakeDuration, TMP_Text textComponent)
    {
        float timerDuration = 0f;
        float timerBetweenShakes = 0f;

        while (timerDuration < shakeDuration)
        {
            if (timerBetweenShakes >= delayBetweenShakes)
            {
                textComponent.rectTransform.anchoredPosition = new Vector3(initialAnchoredPosition.x + UnityEngine.Random.Range(-deltaX, deltaX + 1),
                                                                   initialAnchoredPosition.y + UnityEngine.Random.Range(-deltaY, deltaY + 1),
                                                                   initialAnchoredPosition.z);
                timerBetweenShakes = 0f;
            }

            timerDuration += Time.deltaTime;
            timerBetweenShakes += Time.deltaTime;

            // IMPORTANT FIX: this yield was missing in the original. Without it, the
            // while loop never hands control back to Unity between iterations, so the
            // whole "shakeDuration" worth of jitter happens synchronously within a
            // single frame - the shake never actually renders, it just freezes for
            // one tick and immediately snaps back to initialPosition afterward.
            yield return null;
        }

        textComponent.rectTransform.anchoredPosition = initialAnchoredPosition;
    }
}