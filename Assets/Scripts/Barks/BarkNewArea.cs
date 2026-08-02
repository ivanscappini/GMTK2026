using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Triggers a one-time bark exchange the first time the player enters this area
/// (e.g. a trigger volume placed at the entrance of a new zone).
///
/// The typing/fading/shaking/font-lookup/exchange-picking logic now lives in the shared
/// DialogueTyper utility (also used by BarksManager, BarkGameOver, BarkMainMenu and
/// DisappearingText) instead of being duplicated here.
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

        int maxIndex = DialogueTyper.GetMaxDialogueIndex(barksArea);
        PlayDialogue(barksArea, Random.Range(0, maxIndex + 1)); // +1: Random.Range's upper bound is exclusive
    }

    private void PlayDialogue(DialogueClass[] barksToPlay, int index)
    {
        barkTextDialogueFirst.text = "";
        barkTextDialogueSecond.text = "";

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

    IEnumerator PlayCharacter(string firstDialogue, string secondDialogue)
    {
        yield return DialogueTyper.TypeText(barkTextDialogueFirst, firstDialogue, delayBetweenCharacters, useUnscaledTime: false);

        yield return new WaitForSeconds(delayBetweenDialogues);

        // Only type out (and wait for) a reply if this exchange actually has one - a
        // single-line exchange shouldn't pay an extra second of dead air waiting on an
        // empty string.
        if (secondDialogue.Length > 0)
        {
            yield return DialogueTyper.TypeText(barkTextDialogueSecond, secondDialogue, delayBetweenCharacters, useUnscaledTime: false);

            yield return new WaitForSeconds(delayBetweenDialogues);
        }

        yield return DialogueTyper.FadeOutTogether(barkTextDialogueFirst, barkTextDialogueSecond, fadeSpeed: 1f, useUnscaledTime: false);
    }
}