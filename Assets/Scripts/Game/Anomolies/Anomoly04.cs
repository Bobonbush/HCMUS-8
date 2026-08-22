using System.Collections.Generic;
using UnityEngine;

// Room number plates above the classroom doors turn into gibberish.
public class Anomoly04 : MonoBehaviour, Anomoly
{

    [Tooltip("The room number labels on the door signs, wired by the setup tooling.")]
    public List<TMPro.TextMeshPro> labels = new List<TMPro.TextMeshPro>();

    Anomoly.EvaluateType type = Anomoly.EvaluateType.Single;

    private readonly List<string> originals = new List<string>();
    private const string Garbage = "QXZKVWJHRTLGY";

    public void Evaluate()
    {
        originals.Clear();
        foreach (TMPro.TextMeshPro label in labels)
        {
            if (label == null) { originals.Add(null); continue; }
            originals.Add(label.text);

            char[] scrambled = label.text.ToCharArray();
            for (int i = 0; i < scrambled.Length; i++)
            {
                // Keep separators so it still reads like a sign, just a wrong one.
                if (scrambled[i] == '.' || scrambled[i] == ' ') continue;
                scrambled[i] = Garbage[Random.Range(0, Garbage.Length)];
            }
            label.text = new string(scrambled);
        }
    }

    public void Restore()
    {
        for (int i = 0; i < labels.Count && i < originals.Count; i++)
        {
            if (labels[i] != null && originals[i] != null) labels[i].text = originals[i];
        }
        originals.Clear();
    }

    public Anomoly.EvaluateType getType()
    {
        return type;
    }
}
