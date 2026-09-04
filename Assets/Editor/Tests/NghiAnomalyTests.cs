using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class NghiAnomalyTests
{
    private const string FloorPath = "Assets/Prefabs/Floor.prefab";

    [Test]
    public void CameraAnomaly_TracksOnlyWhenActiveAndLeavesElevatorFixed()
    {
        NghiCameraTrackingSetup.CheckTracking();
    }

    [Test]
    public void SwitchingAwayFromFollow_RestoresPatrolAndClearsFollowState()
    {
        GameObject floor = PrefabUtility.LoadPrefabContents(FloorPath);
        try
        {
            var manager = floor.GetComponent<AnomolyManager>();
            var follow = floor.GetComponentInChildren<Anomoly15>(true);
            var patrol = follow.agentObject.GetComponent<NPCTrajectory>();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            int index = manager.anomolies.IndexOf(follow);
            manager.ForceAnomoly(index);
            Assert.That((bool)typeof(Anomoly15).GetField("active", flags).GetValue(follow), Is.True);
            // Reproduce the state after a close encounter, without needing a baked NavMesh.
            typeof(Anomoly15).GetField("following", flags).SetValue(follow, true);
            patrol.enabled = false;
            var replacement = floor.AddComponent<NghiSelectionProbe>();
            manager.anomolies.Add(replacement);
            manager.ForceAnomoly(manager.anomolies.Count - 1);
            Assert.That((bool)typeof(Anomoly15).GetField("active", flags).GetValue(follow), Is.False);
            Assert.That((bool)typeof(Anomoly15).GetField("following", flags).GetValue(follow), Is.False);
            Assert.That(patrol.enabled, Is.True);
            Assert.That(follow.agentObject.GetComponent<NPCChasing>().enabled, Is.False);
            Assert.That(manager.currentType, Is.EqualTo(Anomoly.EvaluateType.Single));
            manager.ForceRestore();
            Assert.That(replacement.restored, Is.True);
        }
        finally { PrefabUtility.UnloadPrefabContents(floor); }
    }

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
                a.extraCameras.All(c => c != null) &&
                a.cameraHeads.Count == a.extraCameras.Count && a.cameraHeads.All(h => h != null) &&
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
            Assert.That(target.activeSelf, Is.True);
            target.SetActive(false);

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

public class NghiSelectionProbe : MonoBehaviour, Anomoly
{
    public bool restored;
    public void Evaluate() { restored = false; }
    public void Restore() { restored = true; }
    public Anomoly.EvaluateType getType() => Anomoly.EvaluateType.Single;
}
