using System.Collections;
using TMPro;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BarkMainMenu : MonoBehaviour
{
    [Header("Dialogue Game Over")]
    [Tooltip("All possible game-over bark lines. Lines sharing the same dialogueIndex form one exchange (first speaker, then reply).")]
    [SerializeField] private DialogueClass[] dialogues;

    [Header("Dialogues UI")]
    [SerializeField] private TMP_Text barkTextDialogue;

    [Header("Dialogues settings")]
    [SerializeField] private float delayBetweenCharacters = 0.1f;
    [SerializeField] private float delayBetweenCharactersLastDialogue = 0.8f;
    [SerializeField] private float delayBetweenShakes = 0.08f;
    [SerializeField] private float deltaX = 0.7f;
    [SerializeField] private float deltaY = 0.7f;

    [Header("Barks fonts")]
    [SerializeField] private TMP_FontAsset barkFontMonster;
    [SerializeField] private TMP_FontAsset barkFontVector;
    [SerializeField] private TMP_FontAsset barkFontHQ;

    [SerializeField] private float lastDialogueFontSize = 400f;

    [SerializeField] private UnityEditor.SceneAsset gameplaySene;

    private bool isBarking = true;
    private int dialogueIndex = 0;

    [SerializeField] public InputAction interactAction;

    private void Awake()
    {
        barkTextDialogue.text = string.Empty;
    }

    private void Start()
    {
        HintDialogue.Instance.onHintAppeared += SetIsBarking;

        interactAction.Enable();
        interactAction.started += OnInteract;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if ((isBarking)) return;

        if (dialogueIndex < dialogues.Length)
        {
            StartCoroutine(PlayDialogue(dialogues));
        }
        else
        {
            SceneManager.LoadScene(gameplaySene.name);
        }
    }

    IEnumerator PlayDialogue(DialogueClass[] dialogues)
    {
        isBarking = true;

        if (dialogueIndex != 0)
        {
            while (barkTextDialogue.color.a > 0)
            {
                barkTextDialogue.color = new Color(barkTextDialogue.color.r,
                                                   barkTextDialogue.color.g,
                                                   barkTextDialogue.color.b,
                                                   barkTextDialogue.color.a - Time.unscaledDeltaTime);
                yield return null;
            }

            barkTextDialogue.text = string.Empty;
            barkTextDialogue.color = new Color(barkTextDialogue.color.r,
                                                       barkTextDialogue.color.g,
                                                       barkTextDialogue.color.b,
                                                       1);
        }

        barkTextDialogue.font = GetFontForCharacter(dialogues[dialogueIndex].character);

        int index = 0;

        if (dialogueIndex == dialogues.Length - 1)
        {
            barkTextDialogue.fontSize = lastDialogueFontSize;
            delayBetweenCharacters = delayBetweenCharactersLastDialogue;
        }

        if (dialogues[dialogueIndex].character.ToString() == "Monster")
        {
            StartCoroutine(ShakeText(barkTextDialogue.rectTransform.position, dialogues[dialogueIndex].dialogue.Length * delayBetweenCharacters, barkTextDialogue));
        }

        while (index < dialogues[dialogueIndex].dialogue.Length)
        {
            barkTextDialogue.text += dialogues[dialogueIndex].dialogue[index];
            index++;
            yield return new WaitForSecondsRealtime(delayBetweenCharacters);
        }

        dialogueIndex++;
        isBarking = false;
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

    IEnumerator ShakeText(Vector3 initialPosition, float shakeDuration, TMP_Text textComponent)
    {
        float timerDuration = 0f;
        float timerBetweenShakes = 0f;

        while (timerDuration < shakeDuration)
        {
            if (timerBetweenShakes >= delayBetweenShakes)
            {
                textComponent.rectTransform.position = new Vector3(initialPosition.x + UnityEngine.Random.Range(-deltaX, deltaX + 1),
                                                                   initialPosition.y + UnityEngine.Random.Range(-deltaY, deltaY + 1),
                                                                   initialPosition.z);
                timerBetweenShakes = 0f;
            }

            timerDuration += Time.unscaledDeltaTime;
            timerBetweenShakes += Time.unscaledDeltaTime;

            // IMPORTANT FIX: this yield was missing in the original. Without it, the
            // while loop never hands control back to Unity between iterations, so the
            // whole "shakeDuration" worth of jitter happens synchronously within a
            // single frame - the shake never actually renders, it just freezes for
            // one tick and immediately snaps back to initialPosition afterward.
            yield return null;
        }

        textComponent.rectTransform.position = initialPosition;
    }

    private void SetIsBarking()
    {
        isBarking = false;
    }
}
