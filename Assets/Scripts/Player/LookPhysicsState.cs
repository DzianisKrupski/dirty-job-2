#nullable enable
using UnityEngine;

namespace Player
{
    public sealed class LookPhysicsState
    {
        public struct Snapshot
        {
            public float Yaw;
            public float Pitch;
        }

        public Snapshot Prev { get; private set; }
        public Snapshot Curr { get; private set; }
        public float LastFixedTime { get; private set; }

        public void PushFixed(float yawDeg, float pitchDeg)
        {
            Prev = Curr;
            Curr = new Snapshot { Yaw = yawDeg, Pitch = pitchDeg };
            LastFixedTime = Time.time; // время наступившего фикс-тика
        }

        public float GetAlpha()
        {
            // Насколько продвинулись от последнего фикс-тика.
            float t = (Time.time - LastFixedTime) / Time.fixedDeltaTime;
            return Mathf.Clamp01(t);
        }
    }
}