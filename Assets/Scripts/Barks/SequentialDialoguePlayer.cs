using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The main-menu intro bark: on each "interact" press, types out the next line from
/// "dialogues" in sequence (not a random exchange, unlike BarkGameOver/BarkNewArea); once all
/// lines have played, the next interact press loads the "Blockout" scene instead.
///
/// The typing/fading/shaking/font-lookup logic now lives in the shared DialogueTyper utility
/// (also used by BarksManager, BarkGameOver, BarkNewArea and DisappearingText) instead of being
/// duplicated here.
/// </summary>
public class SequentialDialoguePlayer : MonoBehaviour
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
    [SerializeField] private AudioSource audio;

    [Header("Barks fonts")]
    [SerializeField] private TMP_FontAsset barkFontMonster;
    [SerializeField] private TMP_FontAsset barkFontVector;
    [SerializeField] private TMP_FontAsset barkFontHQ;

    [Header("Scene to load after dialogues")]
    [SerializeField] private string sceneToLoadAfterDialogues;

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
    }

    // BUGFIX: interactAction used to be Enable()'d and subscribed once in Start() and was
    // never Disable()'d or unsubscribed - so this component kept listening for the "interact"
    // input for its whole lifetime, even after the last dialogue line had already played
    // (pressing interact would still fire and reload "Blockout" every time), and it would keep
    // listening even if the object survived past this scene. Moving Enable/subscribe into
    // OnEnable and adding the matching OnDisable ties the listening state to whether this
    // component is actually active.
    private void OnEnable()
    {
        interactAction.Enable();
        interactAction.started += OnInteract;
    }

    private void OnDisable()
    {
        interactAction.started -= OnInteract;
        interactAction.Disable();
    }

    private void OnDestroy()
    {
        if (HintDialogue.Instance != null)
            HintDialogue.Instance.onHintAppeared -= SetIsBarking;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (isBarking) return;

        if (dialogueIndex < dialogues.Length)
        {
            StartCoroutine(PlayDialogue(dialogues));
        }
        else
        {
            SceneManager.LoadScene(sceneToLoadAfterDialogues);
        }
    }

    IEnumerator PlayDialogue(DialogueClass[] dialogues)
    {
        isBarking = true;

        DialogueClass line = dialogues[dialogueIndex];

        if (dialogueIndex != 0)
        {
            yield return DialogueTyper.FadeOut(barkTextDialogue, fadeSpeed: 1f, useUnscaledTime: true);

            barkTextDialogue.text = string.Empty;
            barkTextDialogue.color = new Color(barkTextDialogue.color.r, barkTextDialogue.color.g, barkTextDialogue.color.b, 1);
        }

        barkTextDialogue.font = DialogueTyper.GetFontForLine(line, barkFontMonster, barkFontVector, barkFontHQ);

        if (dialogueIndex == dialogues.Length - 1)
        {
            delayBetweenCharacters = delayBetweenCharactersLastDialogue;
        }

        // Applied after the "last dialogue" special case above, so a per-line override (if the
        // author sets one) always has the final say - including opting a line out of the
        // big-font treatment on the very last line.
        DialogueTyper.ApplyLineOverrides(barkTextDialogue, line);

        if (line.character == Character.Monster)
        {
            StartCoroutine(DialogueTyper.Shake(barkTextDialogue, barkTextDialogue.rectTransform.anchoredPosition,
                                    line.dialogue.Length * delayBetweenCharacters, delayBetweenShakes, deltaX, deltaY, useUnscaledTime: true));
        }

        audio.Play();

        yield return DialogueTyper.TypeText(barkTextDialogue, line.dialogue, delayBetweenCharacters, useUnscaledTime: true);

        dialogueIndex++;
        isBarking = false;
    }

    private void SetIsBarking()
    {
        isBarking = false;
    }
}