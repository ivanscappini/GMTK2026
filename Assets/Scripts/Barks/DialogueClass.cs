using TMPro;
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
///
/// Also supports optional per-line overrides for font, color and size, so an individual line
/// can deviate from the defaults its playing script (BarksManager, BarkGameOver, BarkMainMenu,
/// BarkNewArea, DisappearingText) would otherwise apply based on "character". All overrides
/// default to off/false, so existing dialogue data configured in the Inspector is completely
/// unaffected until you explicitly opt a line into one.
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

    [Header("Overrides (optional)")]
    [Tooltip("If enabled, use fontOverride for this line instead of the font normally picked from 'character'.")]
    public bool overrideFont = false;
    [Tooltip("Font used when overrideFont is enabled.")]
    public TMP_FontAsset fontOverride;

    [Tooltip("If enabled, tint this line with colorOverride instead of the text component's default color.")]
    public bool overrideColor = false;
    [Tooltip("Color used when overrideColor is enabled (alpha is ignored - the typewriter/fade effects manage alpha themselves).")]
    public Color colorOverride = Color.white;

    [Tooltip("If enabled, use fontSizeOverride for this line instead of the text component's current size.")]
    public bool overrideFontSize = false;
    [Tooltip("Font size used when overrideFontSize is enabled.")]
    public float fontSizeOverride = 36f;

    public DialogueClass(Character character, string dialogue, int dialogueIndex)
    {
        this.character = character;
        this.dialogue = dialogue;
        this.dialogueIndex = dialogueIndex;
    }
}