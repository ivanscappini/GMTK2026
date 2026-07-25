using System.Collections;
using TMPro;
using UnityEngine;

public class BarkGameOver : MonoBehaviour
{
    [Header("Dialogue Game Over")]
    [SerializeField] private DialogueClass[] dialoguesGameOver;


    [Header("Dialogues UI")]
    [SerializeField] private TMP_Text barkTextDialogueFirst;
    [SerializeField] private TMP_Text barkTextDialogueSecond;

    [Header("Dialogues settings")]
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

    private void Start()
    {
        barkTextDialogueFirst.text = "";
        barkTextDialogueSecond.text = "";

        PlayDialogue(dialoguesGameOver, Random.Range(0, dialoguesGameOver[dialoguesGameOver.Length - 1].dialogueIndex));
    }

    private void PlayDialogue(DialogueClass[] barksToPlay, int index)
    {
        Debug.Log("Playing dialogue with index: " + index);

        string characterNameFirst = "";
        string dialogueFirst = "";
        string characterNameSecond = "";
        string dialogueSecond = "";

        foreach (var bark in barksToPlay)
        {
            if (bark.dialogueIndex == index)
            {
                if (characterNameFirst == "")
                {
                    characterNameFirst = bark.character.ToString();
                    dialogueFirst = bark.dialogue;

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
                    characterNameSecond = bark.character.ToString();
                    dialogueSecond = bark.dialogue;

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

        if (characterNameFirst == "Monster")
        {

            StartCoroutine(ShakeText(barkTextDialogueFirst.rectTransform.position,
                                     (dialogueFirst.Length * delayBetweenCharacters) + delayBeforeCharactersVanish,
                                     barkTextDialogueFirst));
        }
        else if (characterNameSecond == "Monster")
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

        }

        textComponent.rectTransform.position = initialPosition;

        yield return null;
    }
}
