using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Triggers a one-time bark exchange the first time the player enters this area
/// (e.g. a trigger volume placed at the entrance of a new zone).
/// </summary>
public class BarkNewArea : MonoBehaviour
{
    [Header("Bark dialogues for new area")]
    [Tooltip("Lines for this area. Lines sharing the same dialogueIndex form one exchange.")]
    [SerializeField] private DialogueClass[] barksArea;

    [Header("Barks UI")]
    [SerializeField] private TMP_Text barkTextDialogueFirst;
    [SerializeField] private TMP_Text barkTextDialogueSecond;

    [Header("Barks settings")]
    [SerializeField] private float delayBetweenCharacters = 0.1f;
    [SerializeField] private float delayBetweenDialogues = 1f;
    [SerializeField] private float delayBeforeCharactersVanish = 1.2f;
    [SerializeField] private float delayBetweenShakes = 0.08f;
    [SerializeField] private float deltaX = 10f;
    [SerializeField] private float deltaY = 10f;

    [Header("Barks fonts")]
    [SerializeField] private TMP_FontAsset barkFontMonster;
    [SerializeField] private TMP_FontAsset barkFontVector;
    [SerializeField] private TMP_FontAsset barkFontHQ;

    // Once the bark has played, this stays false forever - by design, this is a
    // one-shot "welcome to the area" line, not a repeatable trigger.
    private bool canBark = true;

    private void Start()
    {
        barkTextDialogueFirst.text = "";
        barkTextDialogueSecond.text = "";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!canBark) return;
        if (barksArea == null || barksArea.Length == 0) return;

        canBark = false;

        int maxIndex = GetMaxDialogueIndex(barksArea);
        PlayDialogue(barksArea, Random.Range(0, maxIndex + 1)); // +1: Random.Range's upper bound is exclusive
    }

    /// <summary>
    /// Highest dialogueIndex present in the array - the valid range to randomize over.
    /// (barksArea.Length would be wrong here: that's the total number of individual
    /// lines, not the number of exchanges. If an exchange has two lines, using
    /// Length as the upper bound lets Random.Range pick numbers that don't correspond
    /// to any exchange at all, silently producing an empty bark.)
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

    private TMP_FontAsset GetFontForCharacter(Character character)
    {
        switch (character)
        {
            case Character.Monster: return barkFontMonster;
            case Character.Vector: return barkFontVector;
            default: return barkFontHQ;
        }
    }

    private void PlayDialogue(DialogueClass[] barksToPlay, int index)
    {
        barkTextDialogueFirst.text = "";
        barkTextDialogueSecond.text = "";

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

        if (firstCharacter == Character.Monster)
        {
            StartCoroutine(ShakeText(barkTextDialogueFirst.rectTransform.position,
                                     (dialogueFirst.Length * delayBetweenCharacters) + delayBeforeCharactersVanish,
                                     barkTextDialogueFirst));
        }
        else if (secondCharacter == Character.Monster)
        {
            StartCoroutine(ShakeText(barkTextDialogueSecond.rectTransform.position,
                                     (dialogueFirst.Length * delayBetweenCharacters) + (dialogueSecond.Length * delayBetweenCharacters) + delayBeforeCharactersVanish * 2,
                                     barkTextDialogueSecond));
        }
    }

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

        // FIX: only type out (and wait for) a reply if this exchange actually has one.
        // The original always ran this block, so a single-line exchange still paid
        // an extra second of dead air waiting on an empty string.
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

    IEnumerator ShakeText(Vector2 initialPosition, float shakeDuration, TMP_Text textComponent)
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

            timerDuration += Time.deltaTime;
            timerBetweenShakes += Time.deltaTime;

            // Same missing-yield bug as the other bark scripts - without this the
            // shake completed in a single frame instead of animating over time.
            yield return null;
        }

        textComponent.rectTransform.position = initialPosition;
    }
}