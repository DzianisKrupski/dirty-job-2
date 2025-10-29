#nullable enable
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Rigidbody rb = default!;
        [SerializeField] private Transform cam = default!; // точка взгляда
        [SerializeField] private LayerMask interactMask;
        [SerializeField] private float useDistance = 3.0f;
        [SerializeField] private float holdDistance = 1.8f;
        [SerializeField] private float holdSpring = 90f;
        [SerializeField] private float holdDamper = 12f;
        [SerializeField] private float throwImpulse = 12f;

        private Rigidbody? _held;
        private ConfigurableJoint? _joint;

        public void TryInteract()
        {
            if (_held != null) { DropHeld(); return; }

            if (Physics.Raycast(cam.position, cam.forward, out var hit, useDistance, interactMask, QueryTriggerInteraction.Ignore))
            {
                var rbHit = hit.rigidbody;
                if (rbHit != null && rbHit.mass <= 25f) // // ASSUMPTION: лимит массы переносимых предметов
                {
                    Grab(rbHit);
                }
            }
        }

        public void TryThrow()
        {
            if (_held == null) return;
            var dir = cam.forward;
            DropHeld();
            _held?.AddForce(dir * throwImpulse, ForceMode.VelocityChange);
        }

        private void Grab(Rigidbody target)
        {
            _held = target;
            _held.interpolation = RigidbodyInterpolation.Interpolate;
            _held.useGravity = true;

            _joint = gameObject.AddComponent<ConfigurableJoint>();
            _joint.connectedBody = target;
            _joint.autoConfigureConnectedAnchor = false;
            _joint.anchor = Vector3.zero;
            _joint.connectedAnchor = Vector3.zero;
            _joint.xMotion = ConfigurableJointMotion.Limited;
            _joint.yMotion = ConfigurableJointMotion.Limited;
            _joint.zMotion = ConfigurableJointMotion.Limited;
            _joint.angularXMotion = ConfigurableJointMotion.Limited;
            _joint.angularYMotion = ConfigurableJointMotion.Limited;
            _joint.angularZMotion = ConfigurableJointMotion.Limited;

            Soft(_joint, holdSpring, holdDamper);
        }

        private void DropHeld()
        {
            if (_joint != null) Destroy(_joint);
            _joint = null;
            _held = null;
        }

        void FixedUpdate()
        {
            if (_joint == null || _held == null || cam == null) return;

            // подтягиваем предмет к цели перед камерой
            Vector3 targetPos = cam.position + cam.forward * holdDistance;
            Vector3 toTarget = targetPos - _held.worldCenterOfMass;
            rb.AddForceAtPosition(toTarget * holdSpring, targetPos, ForceMode.Acceleration);

            // демпфирование относительной скорости
            Vector3 relVel = _held.linearVelocity - rb.linearVelocity;
            rb.AddForce(-relVel * holdDamper, ForceMode.Acceleration);
        }

        private static void Soft(ConfigurableJoint j, float s, float d)
        {
            var ld = new SoftJointLimit { limit = 0.01f };
            j.linearLimit = ld;
            var sD = new SoftJointLimitSpring { spring = s, damper = d };
            j.linearLimitSpring = sD;

            var ang = new SoftJointLimit { limit = 10f };
            j.lowAngularXLimit = j.highAngularXLimit = ang;
            j.angularYLimit = j.angularZLimit = ang;
            var aS = new SoftJointLimitSpring { spring = s * 0.5f, damper = d };
            j.angularXLimitSpring = j.angularYZLimitSpring = aS;
        }
    }
}
