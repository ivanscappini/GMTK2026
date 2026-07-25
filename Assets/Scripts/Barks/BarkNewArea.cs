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
    [SerializeField] private TMP_Text barkTextCharFirst;
    [SerializeField] private TMP_Text barkTextDialogueFirst;
    [SerializeField] private TMP_Text barkTextCharSecond;
    [SerializeField] private TMP_Text barkTextDialogueSecond;

    [Header("Barks settings")]
    [SerializeField] private float delayBetweenCharacters = 0.1f;
    [SerializeField] private float delayBetweenDialogues = 1f;
    [SerializeField] private float delayBeforeCharactersVanish = 1.2f;

    private bool canBark = true;

    private void Start()
    {
        barkTextCharFirst.text = "";
        barkTextDialogueFirst.text = "";
        barkTextCharSecond.text = "";
        barkTextDialogueSecond.text = "";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (!canBark) return;

        canBark = false;

        //Debug.Log("Player entered the new area trigger. Barking dialogue will play.");

        //BarksManager.Instance.Barking = true;

        PlayDialogue(barksArea, Random.Range(0, barksArea.Length));
    }

    private void PlayDialogue(DialogueClass[] barksToPlay, int index)
    {
        barkTextCharFirst.text = "";
        barkTextDialogueFirst.text = "";
        barkTextCharSecond.text = "";
        barkTextDialogueSecond.text = "";

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
                }
                else
                {
                    characterNameSecond = bark.character.ToString();
                    dialogueSecond = bark.dialogue;
                }
            }
        }

        barkTextCharFirst.text = characterNameFirst + ":";
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

        barkTextCharSecond.text = secondCharacter;

        index = 0;

        while (index < secondDialogue.Length)
        {
            barkTextDialogueSecond.text += secondDialogue[index];
            yield return new WaitForSeconds(delayBetweenCharacters);
            index++;
        }

        yield return new WaitForSeconds(delayBetweenDialogues);

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
