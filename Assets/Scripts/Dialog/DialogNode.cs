using System.Collections.Generic;
using UnityEngine;

// A single line in the dialog tree.
// Create an asset via right-click in the Project window: Create > Dialog > Dialog Node
[CreateAssetMenu(fileName = "DialogNode", menuName = "Dialog/Dialog Node")]
public class DialogNode : ScriptableObject
{
    public string speakerName;          // Who is talking (Boss, Player...)

    [TextArea(3, 10)]
    public string text;                 // The line's content

    public List<DialogChoice> choices = new List<DialogChoice>();

    // No choices => this is the final line of the conversation
    public bool IsEnd => choices == null || choices.Count == 0;
}

[System.Serializable]
public class DialogChoice
{
    [TextArea(1, 3)]
    public string choiceText;           // Label shown on the button: "A", "B"...

    public DialogNode nextNode;         // Clicking jumps to this node (C, D...). Leave empty = end.
}
