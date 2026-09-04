using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// The brain of the dialog: handles the branching logic (pick A go this way, pick B go that way).
// All display is delegated to DialogView, so every style shares this same logic.
public class CutSceneDialogManager : MonoBehaviour
{
    public static CutSceneDialogManager Instance { get; private set; }



    [Header("The two display styles")]
    public DialogView boxView;            // rectangular box at the bottom
    public DialogView bubbleView;         // speech bubble above a character

    // Fired when the conversation ends -> e.g. the Boss listens to this to start the fight.
    public event Action DialogEnded;

    private DialogView active;            // the view used by the current conversation

    public bool IsActive => active != null;   // true while a conversation is on screen
    private DialogNode currentNode;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
        if (boxView != null) boxView.Close();
        if (bubbleView != null) bubbleView.Close();

    }

    bool IsAnyGamepadButtonPressed()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) return false;

        // Loop through all controls on the gamepad (buttons, triggers, bumpers, stick presses)
        foreach (var control in gamepad.allControls)
        {
            if (control is ButtonControl button && button.wasPressedThisFrame)
            {
                return true;
            }
        }
        return false;
    }

    private bool JustStarted = false;
    private void Update()
    {
        if (active == null)
            return;

        if (JustStarted || !(Keyboard.current.anyKey.wasPressedThisFrame || (Gamepad.current != null && IsAnyGamepadButtonPressed())))
        {
            JustStarted = false;
            return;
        }
        // First press -> reveal instantly
        if (!active.AnimationDone)
        {
            active.SkipTypewriter();
            return;
        }


        if (currentNode == null)
            return;
        
        if (currentNode.IsEnd)
        {
            EndDialog();
            return;
        }

        if (currentNode.choices.Count == 1)
        {
            
            OnChoiceSelected(currentNode.choices[0].nextNode);
        }
    }

    public void StartDialog(DialogNode startNode, DialogStyle style, Transform target = null)
    {

        active = (style == DialogStyle.Bubble) ? bubbleView : boxView;
        if (active == null)
        {
            Debug.LogError($"DialogManager: chưa gán {style} View trong Inspector!", this);
            return;
        }
        JustStarted = true;
        if (style == DialogStyle.Bubble) active.SetTarget(target);

        active.Open();
        ShowNode(startNode);
    }

    private void ShowNode(DialogNode node)
    {
        currentNode = node;
        active.SetText(node.speakerName, node.text);
        active.ClearChoices();

        if (node.IsEnd)
        {
            active.SpawnChoice("Close", EndDialog);
            return;
        }

        // ===== BRANCHING (the "if-else") =====
        // Each choice jumps to its own nextNode. Pick A (choices[0]) -> node C,
        // pick B (choices[1]) -> node D.
        foreach (DialogChoice choice in node.choices)
        {
            DialogNode nextNode = choice.nextNode;  // copy to a local so the lambda captures the right value
            //active.SpawnChoice(choice.choiceText, () => OnChoiceSelected(nextNode));
        }
    }

    private void OnChoiceSelected(DialogNode nextNode)
    {
        if (nextNode == null) { EndDialog(); return; }  // points nowhere => end
        ShowNode(nextNode);                              // jump to the matching branch
    }

    public bool AnimationDone()
    {
        
        if (active == null) return true;

        return active.AnimationDone;
    }


    public void EndDialog()
    {
        if (active != null) active.Close();
        active = null;
        DialogEnded?.Invoke();
    }
}
