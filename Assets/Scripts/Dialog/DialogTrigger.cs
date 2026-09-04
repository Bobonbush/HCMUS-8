using UnityEngine;

// Attach this to a Boss (or NPC). It is the "trigger" that starts a conversation.
public class DialogTrigger : MonoBehaviour
{
    public DialogNode startNode;                 // First node of the conversation
    public DialogStyle style = DialogStyle.Box;  // Box or bubble
    public Transform bubbleTarget;               // Who the bubble follows (empty = this object)
    public bool triggerOnce = true;              // Run only once?

    private bool used = false;

    public void TriggerDialog()
    {
        if (triggerOnce && used) return;
        if (startNode == null || DialogManager.Instance == null) return;

        used = true;
        Transform target = bubbleTarget != null ? bubbleTarget : transform;
        DialogManager.Instance.StartDialog(startNode, style, target);
    }

    // Option 1: auto-run when the Player enters the trigger zone (needs a Collider2D with "Is Trigger").
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            
            TriggerDialog();
        }
    }



    // Option 2: call TriggerDialog() from other code whenever you want (e.g. right before a boss fight).
}
