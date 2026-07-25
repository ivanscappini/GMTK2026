using System.Collections;
using System.ComponentModel.Design;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class BarkNewArea : MonoBehaviour
{
    [Header("Bark dialogues for new area")]
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

        canBark = false;

        PlayDialogue(barksArea, Random.Range(0, barksArea.Length));
    }

    private void PlayDialogue(DialogueClass[] barksToPlay, int index)
    {
        barkTextDialogueFirst.text = "";
        barkTextDialogueSecond.text = "";

        string dialogueFirst = "";
        string dialogueFirstChar = "";
        string dialogueSecond = "";
        string dialogueSecondChar = "";

        foreach (var bark in barksToPlay)
        {
            if (bark.dialogueIndex == index)
            {
                if (dialogueFirst == "")
                {
                    dialogueFirst = bark.dialogue;
                    dialogueFirstChar = bark.character.ToString();

                    if (bark.character.ToString() == "Monster")
                    {
                        barkTextDialogueFirst.font = barkFontMonster;
                    }
                    else if (bark.character.ToString() == "Vector")
                    {
                        barkTextDialogueFirst.font = barkFontVector;
                    }
                    else
                    {
                        barkTextDialogueFirst.font = barkFontHQ;
                    }
                }
                else
                {
                    dialogueSecond = bark.dialogue;
                    dialogueSecondChar = bark.character.ToString();

                    if (bark.character.ToString() == "Monster")
                    {
                        barkTextDialogueSecond.font = barkFontMonster;
                    }
                    else if (bark.character.ToString() == "Vector")
                    {
                        barkTextDialogueSecond.font = barkFontVector;
                    }
                    else
                    {
                        barkTextDialogueSecond.font = barkFontHQ;
                    }
                }
            }
        }

        StartCoroutine(PlayCharacter(dialogueFirst, dialogueSecond));

        if (dialogueFirstChar == "Monster")
        {

            StartCoroutine(ShakeText(barkTextDialogueFirst.rectTransform.position,
                                     (dialogueFirst.Length * delayBetweenCharacters) + delayBeforeCharactersVanish,
                                     barkTextDialogueFirst));
        }
        else if (dialogueSecondChar == "Monster")
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

        index = 0;

        while (index < secondDialogue.Length)
        {
            barkTextDialogueSecond.text += secondDialogue[index];
            yield return new WaitForSeconds(delayBetweenCharacters);
            index++;
        }

        yield return new WaitForSeconds(delayBetweenDialogues);

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

        }

        textComponent.rectTransform.position = initialPosition;

        yield return null;
    }
}
