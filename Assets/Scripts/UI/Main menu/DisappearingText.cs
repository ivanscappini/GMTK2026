using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DisappearingText : MonoBehaviour
{
    public static DisappearingText Instance { get; private set; }

    [SerializeField] DialogueClass[] dialogues;
    private int index = 0;

    [SerializeField] TMP_Text dialoguesText;

    [Header("Dialogues settings")]
    [SerializeField] private float delayBetweenCharacters = 0.1f;
    [SerializeField] private float delayBetweenDialogues = 1f;
    [SerializeField] private float delayBeforeCharactersVanish = 1.2f;

    [SerializeField] float WaitTimeBeforeSendEvent = 15;

    public event Action onVanishingDialoguesFinished;

    private void Awake()
    {
        Instance = this;
        Invoke("SendEvent", WaitTimeBeforeSendEvent);
    }

    private void Start()
    {
        dialoguesText.text = string.Empty;

        Mainmenufade.Instance.onFinished += StartDialogues;
    }

    public void StartDialogues()
    {
        StartCoroutine(PlayDialogues(dialogues));
    }

    IEnumerator PlayDialogues(DialogueClass[] dialogues)
    {
        int indexChar = 0;

        while (index < dialogues.Length)
        {
            while (indexChar < dialogues[index].dialogue.Length)
            {
                dialoguesText.text += dialogues[index].dialogue[indexChar];
                indexChar++;
                yield return new WaitForSeconds(delayBetweenCharacters);
            }
            yield return new WaitForSeconds(delayBeforeCharactersVanish);

            while (dialoguesText.color.a > 0)
            {
                dialoguesText.color = new Color(dialoguesText.color.r,
                                                dialoguesText.color.g,
                                                dialoguesText.color.b,
                                                dialoguesText.color.a - Time.deltaTime);
                yield return null;
            }

            dialoguesText.text = string.Empty;
            dialoguesText.color = Color.white;

            index++;
            indexChar = 0;
            yield return new WaitForSeconds(delayBetweenDialogues);
        }
    }

    private void SendEvent()
    {
        onVanishingDialoguesFinished?.Invoke();
    }
}
