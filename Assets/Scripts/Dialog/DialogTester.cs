using UnityEngine;
using UnityEngine.InputSystem;

// TEMPORARY script to test the dialog with a key. Attach to the Player, press F to open.
// Safe to delete once you're done testing.
public class DialogTester : MonoBehaviour
{
    public DialogNode startNode;                 // First node
    public DialogStyle style = DialogStyle.Box;  // Box or Bubble
    public Transform bubbleTarget;               // Who the bubble follows (empty = this object)

    bool played = false;

    float time = 0.0f;


    private void Update()
    {
        
        if (time <= 1.0f)
        {
            time += Time.deltaTime;
            return;
        }
        if(!played)
        {
            Transform target = bubbleTarget != null ? bubbleTarget : transform;
            DialogManager.Instance.StartDialog(startNode, style, target);
            played = true;
        }

        if (!DialogManager.Instance.AnimationDone())
        {
            return;
        }
        time += Time.deltaTime;
        if(time <= 2.0f)
        {
            return;
        }

        Debug.Log("Ended");
        DialogManager.Instance.EndDialog();

        Destroy(this.gameObject);
    }

}
