using UnityEngine;

namespace FreeStyle
{
    /// <summary>
    /// How a door physically moves. Door only ever pushes a value t (0 = closed,
    /// 1 = open); turning that into a swing, a slide or a lift is this class's job.
    ///
    /// To add a new kind of door (hatch that lifts into the ceiling, trapdoor,
    /// portcullis...), subclass DoorMotion and override OnApply(t). Door.cs never
    /// needs to change.
    /// </summary>
    public abstract class DoorMotion : MonoBehaviour
    {
        protected bool Captured { get; private set; }

        /// <summary>Records the current pose as "closed". Door calls this on Awake.</summary>
        public void CaptureClosedState()
        {
            if (Captured) return;
            OnCaptureClosedState();
            Captured = true;
        }

        protected abstract void OnCaptureClosedState();

        /// <summary>t: 0 = fully closed, 1 = fully open.</summary>
        public void Apply(float t)
        {
            CaptureClosedState();
            OnApply(Mathf.Clamp01(t));
        }

        protected abstract void OnApply(float t);

        /// <summary>
        /// Called right before opening so the door knows which side the player is on
        /// (used by hinged doors that swing away from whoever opened them).
        /// </summary>
        public virtual void OnOpenRequested(Vector3 interactorPosition) { }
    }
}
