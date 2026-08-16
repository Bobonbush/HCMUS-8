using UnityEngine;

namespace FreeStyle
{
    /// <summary>A hinged door: rotates about an axis. The default door type.</summary>
    [AddComponentMenu("FreeStyle/Door/Hinge Door Motion")]
    public class HingeDoorMotion : DoorMotion
    {
        [Tooltip("The object that rotates (the hinge). Leave empty to use this object.")]
        [SerializeField] Transform pivot;

        [Tooltip("Rotation axis in the pivot's local space.")]
        [SerializeField] Vector3 localAxis = Vector3.up;

        [Tooltip("Maximum opening angle in degrees. Use a negative value to swing the other way.")]
        [SerializeField] float openAngle = 95f;

        [Tooltip("Automatically pick the direction that swings away from the player. Turn off for a door that always opens one fixed way.")]
        [SerializeField] bool swingAwayFromInteractor = true;

        Quaternion _closedLocalRotation;
        float _sign = 1f;

        Transform Pivot => pivot != null ? pivot : transform;

        protected override void OnCaptureClosedState()
        {
            _closedLocalRotation = Pivot.localRotation;
        }

        public override void OnOpenRequested(Vector3 interactorPosition)
        {
            if (!swingAwayFromInteractor) { _sign = 1f; return; }

            // A positive rotation about +Y pushes the panel towards the hinge's local -Z.
            // So: player standing on the +Z side -> positive angle; on the -Z side -> negative.
            Vector3 toInteractor = interactorPosition - Pivot.position;
            _sign = Vector3.Dot(Pivot.forward, toInteractor) > 0f ? 1f : -1f;
        }

        protected override void OnApply(float t)
        {
            Vector3 axis = localAxis.sqrMagnitude < 0.0001f ? Vector3.up : localAxis.normalized;
            Pivot.localRotation = _closedLocalRotation * Quaternion.AngleAxis(openAngle * _sign * t, axis);
        }
    }
}
