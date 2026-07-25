using System.Collections;
using TMPro;
using UnityEngine;

public class BarkGameOver : MonoBehaviour
{
    [Header("Dialogue Game Over")]
    [SerializeField] private DialogueClass[] dialoguesGameOver;


    [Header("Dialogues UI")]
    [SerializeField] private TMP_Text barkTextCharFirst;
    [SerializeField] private TMP_Text barkTextDialogueFirst;
    [SerializeField] private TMP_Text barkTextCharSecond;
    [SerializeField] private TMP_Text barkTextDialogueSecond;

    [Header("Dialogues settings")]
    [SerializeField] private float delayBetweenCharacters = 0.1f;
    [SerializeField] private float delayBetweenDialogues = 1f;
    [SerializeField] private float delayBeforeCharactersVanish = 1.2f;

    private void Start()
    {
        barkTextCharFirst.text = "";
        barkTextDialogueFirst.text = "";
        barkTextCharSecond.text = "";
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
                    characterNameFirst = bark.character.ToString() + ":";
                    dialogueFirst = bark.dialogue;
                }
                else
                {
                    characterNameSecond = bark.character.ToString();
                    dialogueSecond = bark.dialogue;
                }
            }
        }

        barkTextCharFirst.text = characterNameFirst;
        StartCoroutine(PlayCharacter(dialogueFirst, characterNameSecond + ":", dialogueSecond));
    }

    IEnumerator PlayCharacter(string firstDialogue, string secondCharacter, string secondDialogue)
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
            barkTextCharSecond.text = secondCharacter;

            index = 0;

            while (index < secondDialogue.Length)
            {
                barkTextDialogueSecond.text += secondDialogue[index];
                yield return new WaitForSeconds(delayBetweenCharacters);
                index++;
            }

            yield return new WaitForSeconds(delayBetweenDialogues);
        }

        while (barkTextCharFirst.color.a > 0)
        {
            barkTextCharFirst.color = new Color(barkTextCharFirst.color.r, barkTextCharFirst.color.g, barkTextCharFirst.color.b, barkTextCharFirst.color.a - Time.deltaTime);
            barkTextDialogueFirst.color = new Color(barkTextDialogueFirst.color.r, barkTextDialogueFirst.color.g, barkTextDialogueFirst.color.b, barkTextDialogueFirst.color.a - Time.deltaTime);

            barkTextCharSecond.color = new Color(barkTextCharSecond.color.r, barkTextCharSecond.color.g, barkTextCharSecond.color.b, barkTextCharSecond.color.a - Time.deltaTime);
            barkTextDialogueSecond.color = new Color(barkTextDialogueSecond.color.r, barkTextDialogueSecond.color.g, barkTextDialogueSecond.color.b, barkTextDialogueSecond.color.a - Time.deltaTime);
            yield return null;
        }
    }
}
