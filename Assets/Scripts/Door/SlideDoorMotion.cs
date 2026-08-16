using System;
using UnityEngine;

namespace FreeStyle
{
    /// <summary>
    /// A sliding door. Declare as many panels as you like (single sliding door,
    /// double gate, lift doors...), each with its own slide direction.
    /// </summary>
    [AddComponentMenu("FreeStyle/Door/Slide Door Motion")]
    public class SlideDoorMotion : DoorMotion
    {
        [Serializable]
        public class Panel
        {
            public Transform target;
            [Tooltip("How far this panel travels when fully open, in the panel's local space.")]
            public Vector3 localOffset = new Vector3(0.95f, 0f, 0f);

            [NonSerialized] public Vector3 ClosedLocalPosition;
        }

        [SerializeField] Panel[] panels = new Panel[0];

        protected override void OnCaptureClosedState()
        {
            if (panels.Length == 0)
                panels = new[] { new Panel { target = transform } };

            foreach (var p in panels)
                if (p.target != null) p.ClosedLocalPosition = p.target.localPosition;
        }

        protected override void OnApply(float t)
        {
            foreach (var p in panels)
                if (p.target != null)
                    p.target.localPosition = p.ClosedLocalPosition + p.localOffset * t;
        }
    }
}
