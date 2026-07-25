using UnityEngine;

public enum Character
{
    Vector,
    HQ,
    Monster
}

[System.Serializable]
public class DialogueClass
{
    public Character character;
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
