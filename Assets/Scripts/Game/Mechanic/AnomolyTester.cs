using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Debug tool: step through every anomoly in order while playing.
//
//   F1 - toggle the tester on/off (off = no hotkeys, no overlay)
//   N  - restore everything, then evaluate the NEXT anomoly on the player's floor
//   B  - same, but backwards
//   R  - restore everything (back to a clean floor)
//
// The current anomoly and a hint about where to look are drawn on screen.
// Anomoly03 (AddUp) gets two steps: watcher above, then watcher below.
// Delete this object from the scene (or untick Start Enabled) for real builds.
public class AnomolyTester : MonoBehaviour
{

    [Tooltip("Whether the tester starts active. F1 toggles it at runtime either way.")]
    public bool startEnabled = true;

    private bool active;
    private int step = -1;          // -1 = clean floor, nothing evaluated
    private string status = "";

    private struct Step
    {
        public int index;           // index in AnomolyManager.anomolies
        public int addUpDirection;  // 0 = normal, +1/-1 = AddUp neighbour floor
        public string name;
    }

    private static readonly Dictionary<string, string> Hints = new Dictionary<string, string>
    {
        { "Anomoly01", "A corridor light flickers - watch the ceiling lights (takes ~3s)" },
        { "Anomoly02", "All lights go out - wait 5-10s" },
        { "Anomoly03+1", "Watcher on the floor ABOVE - go to the atrium railing and look UP" },
        { "Anomoly03-1", "Watcher on the floor BELOW - look DOWN the atrium" },
        { "Anomoly04", "Room numbers turn to gibberish - check the green signs above classroom doors" },
        { "Anomoly05", "Go near the big classroom windows - something approaches the glass from the other side" },
        { "Anomoly14", "Someone at the bottom of the east stairwell, facing the corner" },
        { "Anomoly16", "The ceiling presses down to just above your head" },
        { "Anomoly23", "The clock runs backwards - wall between P.201 and P.202, watch the red hand" },
        { "Anomoly27", "Door P.203 sits crooked in its frame, with a gap at the edge" },
        { "Anomoly29", "Step into either lift - you will be swapped to the other one" },
        { "Anomoly07", "Classroom banner reads TPCS instead of APCS - above the chalkboard" },
        { "Anomoly06", "Someone in the wall mirror - toilet, above the sinks" },
        
        { "Anomoly20", "Someone hangs from the ceiling of the lit classroom" },
        { "Anomoly08", "Toilet keep blushing" },
        { "Anomoly10", "Count the corridor ceiling lights - there are more of them than usual" },
        { "Anomoly12", "The desks are stacked in a heap in the middle of the classroom" },
        { "Anomoly13", "A classroom that should be dark lights up on its own after ~5s" },
        { "Anomoly18", "Corridor props (bin, extinguisher, room sign) swell slowly over ~45s" },
        { "Anomoly22", "The conference poster on the green board by the toilets - check its logo" },
        { "Anomoly24", "The crest by the lifts is UEH, not HCMUS" },
        { "Anomoly26", "One ceiling light fixture hangs at the wrong angle - the lighting is unchanged" },
        { "Anomoly31", "Someone Fall from above" },
    };

    private void Awake()
    {
        active = startEnabled;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f1Key.wasPressedThisFrame)
        {
            active = !active;
            if (!active) RestoreAll();
            return;
        }
        if (!active) return;

        if (kb.nKey.wasPressedThisFrame) Next(+1);
        else if (kb.bKey.wasPressedThisFrame) Next(-1);
        else if (kb.rKey.wasPressedThisFrame) RestoreAll();
    }

    public void Next(int direction)
    {
        List<Step> steps = BuildSteps();
        if (steps.Count == 0) { status = "no AnomolyManager found"; return; }

        step = ((step + direction) % (steps.Count + 1) + steps.Count + 1) % (steps.Count + 1);
        // step == steps.Count means the "clean floor" slot in the cycle
        if (step == steps.Count) { RestoreAllInternal(); status = "(clean floor - no anomoly, for comparison)"; return; }

        Apply(steps[step]);
    }

    public void RestoreAll()
    {
        RestoreAllInternal();
        step = -1;
        status = "(everything restored)";
    }

    private void Apply(Step s)
    {
        RestoreAllInternal();

        AnomolyManager mgr = FindPlayerFloorManager();
        if (mgr == null) { status = "player floor not found"; return; }

        if (s.addUpDirection != 0)
            GameManager.Instance.ForceAnomoly(s.addUpDirection, s.index);
        else
            mgr.ForceAnomoly(s.index);

        string key = s.addUpDirection == 0 ? s.name : s.name + (s.addUpDirection > 0 ? "+1" : "-1");
        string hint;
        Hints.TryGetValue(key, out hint);
        status = s.name + (s.addUpDirection > 0 ? " (floor above)" : s.addUpDirection < 0 ? " (floor below)" : "")
               + "\n" + (hint ?? "");
    }

    private List<Step> BuildSteps()
    {
        var result = new List<Step>();
        AnomolyManager mgr = FindPlayerFloorManager();
        if (mgr == null || mgr.anomolies == null) return result;

        for (int i = 0; i < mgr.anomolies.Count; i++)
        {
            MonoBehaviour mb = mgr.anomolies[i];
            if (!(mb is Anomoly)) continue;
            string name = mb.GetType().Name;
            if (((Anomoly)mb).getType() == Anomoly.EvaluateType.AddUp)
            {
                result.Add(new Step { index = i, addUpDirection = +1, name = name });
                result.Add(new Step { index = i, addUpDirection = -1, name = name });
            }
            else
            {
                result.Add(new Step { index = i, addUpDirection = 0, name = name });
            }
        }
        return result;
    }

    private void RestoreAllInternal()
    {
        if (GameManager.Instance != null) GameManager.Instance.ForceRestoreAll();
    }

    private AnomolyManager FindPlayerFloorManager()
    {
        if (GameManager.Instance == null) return null;
        Transform player = GameManager.Instance.GetPlayerTransform();
        if (player == null) return null;

        AnomolyManager best = null;
        float bestDistance = float.MaxValue;
        foreach (AnomolyManager m in FindObjectsByType<AnomolyManager>(FindObjectsSortMode.None))
        {
            float d = Mathf.Abs(m.transform.position.y - player.position.y);
            if (d < bestDistance) { bestDistance = d; best = m; }
        }
        return best;
    }

    private void OnGUI()
    {
        if (!active) return;
        List<Step> steps = BuildSteps();
        string header = step >= 0 && step < steps.Count
            ? "[" + (step + 1) + "/" + steps.Count + "] "
            : "";
        string text = "ANOMOLY TESTER   N: next | B: back | R: restore | F1: hide\n" + header + status;

        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = false };
        style.normal.textColor = Color.white;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(8, 8, 660, 70), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(16, 12, 650, 64), text, style);
    }
}
