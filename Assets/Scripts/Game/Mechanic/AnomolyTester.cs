using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Debug tool: step through every anomoly in order while playing.
//
//   N - restore everything, then evaluate the NEXT anomoly on the player's floor
//   B - same, but backwards
//   R - restore everything (back to a clean floor)
//
// The current anomoly and a hint about where to look are drawn on screen.
// Anomoly03 (AddUp) gets two steps: watcher above, then watcher below.
// Delete this object from the scene (or untick Enabled) for real builds.
public class AnomolyTester : MonoBehaviour
{

    [Tooltip("Master switch so the tool can stay in the scene but do nothing.")]
    public bool hotkeysEnabled = true;

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
        { "Anomoly01", "Den nhap nhay - nhin cac den hanh lang (cho ~3s)" },
        { "Anomoly02", "Toan bo den se tat - cho 5-10s" },
        { "Anomoly03+1", "Nguoi tang TREN nhin xuong - ra gieng troi, nhin LEN" },
        { "Anomoly03-1", "Nguoi tang DUOI - ra gieng troi, nhin XUONG" },
        { "Anomoly04", "So phong thanh gibberish - nhin bang xanh tren cua lop" },
        { "Anomoly05", "Doppelganger - lai gan cua kinh lon roi quay dau lai" },
        { "Anomoly14", "Ke nup - di het hanh lang ve phia cau thang cuoi" },
        { "Anomoly16", "Tran nha thap hon 35cm - de y den tran va mep tuong" },
        { "Anomoly23", "Dong ho chay nguoc - tuong giua P.201 va P.202" },
        { "Anomoly27", "Cua P.203 lech va nghieng, ho khe o mep" },
        { "Anomoly29", "Buoc vao bat ky thang may nao - se bi doi sang thang ben kia" },
        { "Anomoly07", "Banner lop hoc: APCS -> TPCS (2 lop chi tiet, tren bang)" },
        { "Anomoly06", "Guong dung trong phong toilet phia trong - co nguoi trong guong" },
    };

    private void Update()
    {
        if (!hotkeysEnabled) return;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.nKey.wasPressedThisFrame) Next(+1);
        else if (kb.bKey.wasPressedThisFrame) Next(-1);
        else if (kb.rKey.wasPressedThisFrame) RestoreAll();
    }

    public void Next(int direction)
    {
        List<Step> steps = BuildSteps();
        if (steps.Count == 0) { status = "khong tim thay AnomolyManager"; return; }

        step = ((step + direction) % (steps.Count + 1) + steps.Count + 1) % (steps.Count + 1);
        // step == steps.Count means the "clean floor" slot in the cycle
        if (step == steps.Count) { RestoreAllInternal(); status = "(sach - khong co anomoly)"; return; }

        Apply(steps[step]);
    }

    public void RestoreAll()
    {
        RestoreAllInternal();
        step = -1;
        status = "(da restore het)";
    }

    private void Apply(Step s)
    {
        RestoreAllInternal();

        AnomolyManager mgr = FindPlayerFloorManager();
        if (mgr == null) { status = "khong tim thay tang cua player"; return; }

        if (s.addUpDirection != 0)
            GameManager.Instance.ForceAnomoly(s.addUpDirection, s.index);
        else
            mgr.ForceAnomoly(s.index);

        string key = s.addUpDirection == 0 ? s.name : s.name + (s.addUpDirection > 0 ? "+1" : "-1");
        string hint;
        Hints.TryGetValue(key, out hint);
        status = s.name + (s.addUpDirection > 0 ? " (tang tren)" : s.addUpDirection < 0 ? " (tang duoi)" : "")
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
        if (!hotkeysEnabled) return;
        List<Step> steps = BuildSteps();
        string header = step >= 0 && step < steps.Count
            ? "[" + (step + 1) + "/" + steps.Count + "] "
            : "";
        string text = "ANOMOLY TESTER   N: tiep theo | B: lui | R: restore\n" + header + status;

        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = false };
        style.normal.textColor = Color.white;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(8, 8, 640, 70), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(16, 12, 630, 64), text, style);
    }
}
