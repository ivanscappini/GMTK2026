using UnityEngine;

/// <summary>
/// Identifies who is speaking a given bark line. Used to pick the right font and,
/// for Monster, to trigger the screen-shake effect on that line.
/// </summary>
public enum Character
{
    Vector,
    HQ,
    Monster
}

/// <summary>
/// A single line of bark dialogue, with metadata about who says it and which
/// "exchange" it belongs to.
/// </summary>
[System.Serializable]
public class DialogueClass
{
    [Tooltip("Who is speaking this line.")]
    public Character character;

    [Tooltip("The line of dialogue itself.")]
    public string dialogue;

    [Tooltip("The index of the dialogue is used in the dialogue where someone responds to another character." +
             " So for example in the barks when the player is standing still the index isnt used so it can be left as 0")]
    public int dialogueIndex;

    public DialogueClass(Character character, string dialogue, int dialogueIndex)
    {
        this.character = character;
        this.dialogue = dialogue;
        this.dialogueIndex = dialogueIndex;
    }
}