using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class NghiAnomalyTests
{
    private const string FloorPath = "Assets/Prefabs/Floor.prefab";

    [Test]
    public void FloorPrefab_HasEveryNghiAnomalyRegisteredAndWired()
    {
        // Test the saved prefab as-is; tests must never rebuild the user's placements.
        GameObject floor = PrefabUtility.LoadPrefabContents(FloorPath);
        try
        {
            AnomolyManager manager = floor.GetComponent<AnomolyManager>();
            Assert.That(manager, Is.Not.Null);
            AssertRegistered<Anomoly11>(floor, manager, a => a.normalTrash != null && a.replacementTrash != null);
            AssertRegistered<Anomoly15>(floor, manager, a => a.agentObject != null);
            AssertRegistered<Anomoly19>(floor, manager, a => a.corpses.Count > 0);
            AssertRegistered<Anomoly21>(floor, manager, a => a.waterSurfaces.Count > 0);
            AssertRegistered<Anomoly28>(floor, manager, a => a.windowFaces.Count > 0);
            AssertRegistered<Anomoly34>(floor, manager, a =>
                a.extraCameras.Count >= 12 &&
                a.extraCameras.Any(c => c != null && c.name.Contains("Bin_Peeking")) &&
                a.extraCameras.Count(c => c != null && c.name.Contains("StairStep")) >= 3 &&
                a.extraCameras.Count(c => c != null && c.name.Contains("RailingBird")) >= 2);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(floor);
        }
    }

    [Test]
    public void VisibilityAnomalies_EvaluateAndRestoreAreExactInverses()
    {
        GameObject host = new GameObject("test host");
        GameObject target = new GameObject("target");
        try
        {
            target.SetActive(false);
            Anomoly34 cameras = host.AddComponent<Anomoly34>();
            cameras.extraCameras.Add(target);
            cameras.Evaluate();
            Assert.That(target.activeSelf, Is.True);
            cameras.Restore();
            Assert.That(target.activeSelf, Is.False);

            Anomoly19 corpse = host.AddComponent<Anomoly19>();
            corpse.corpses.Add(target);
            corpse.Evaluate();
            Assert.That(target.activeSelf, Is.True);
            corpse.Restore();
            Assert.That(target.activeSelf, Is.False);

            Anomoly28 face = host.AddComponent<Anomoly28>();
            face.windowFaces.Add(target);
            face.Evaluate();
            Assert.That(target.activeSelf, Is.True);
            face.Restore();
            Assert.That(target.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void TrashSwap_EvaluateAndRestoreSelectExactlyOneModel()
    {
        GameObject host = new GameObject("test host");
        GameObject normal = new GameObject("normal");
        GameObject replacement = new GameObject("replacement");
        try
        {
            normal.SetActive(true);
            replacement.SetActive(false);
            Anomoly11 swap = host.AddComponent<Anomoly11>();
            swap.normalTrash = normal;
            swap.replacementTrash = replacement;
            swap.Evaluate();
            Assert.That(normal.activeSelf, Is.False);
            Assert.That(replacement.activeSelf, Is.True);
            swap.Restore();
            Assert.That(normal.activeSelf, Is.True);
            Assert.That(replacement.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(normal);
            Object.DestroyImmediate(replacement);
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void AssignedTypes_MatchManagerRouting()
    {
        GameObject host = new GameObject("type test");
        try
        {
            Assert.That(host.AddComponent<Anomoly11>().getType(), Is.EqualTo(Anomoly.EvaluateType.Single));
            Assert.That(host.AddComponent<Anomoly15>().getType(), Is.EqualTo(Anomoly.EvaluateType.NPCInvolve));
            Assert.That(host.AddComponent<Anomoly19>().getType(), Is.EqualTo(Anomoly.EvaluateType.Single));
            Assert.That(host.AddComponent<Anomoly21>().getType(), Is.EqualTo(Anomoly.EvaluateType.Single));
            Assert.That(host.AddComponent<Anomoly28>().getType(), Is.EqualTo(Anomoly.EvaluateType.Single));
            Assert.That(host.AddComponent<Anomoly34>().getType(), Is.EqualTo(Anomoly.EvaluateType.Single));
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private static void AssertRegistered<T>(GameObject floor, AnomolyManager manager,
                                            System.Func<T, bool> referencesValid) where T : MonoBehaviour, Anomoly
    {
        T component = floor.GetComponentInChildren<T>(true);
        Assert.That(component, Is.Not.Null, typeof(T).Name + " is missing from Floor.prefab");
        Assert.That(manager.anomolies.Contains(component), Is.True, typeof(T).Name + " is not registered");
        Assert.That(referencesValid(component), Is.True, typeof(T).Name + " has missing setup references");
    }
}
