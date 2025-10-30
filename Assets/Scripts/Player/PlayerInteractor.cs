#nullable enable
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : NetworkBehaviour
    {
        [SerializeField] private MovementConfig config = default!;
        [SerializeField] private Rigidbody rb = default!;
        [SerializeField] private Transform cam = default!; // точка взгляда
        [SerializeField] private LayerMask interactMask;
       

        private Rigidbody? _held;
        private ConfigurableJoint? _joint;
        
        [ServerRpc] 
        private void GiveOwnership(NetworkObject box, NetworkConnection toWho)
        {
            box.GiveOwnership(toWho); // короткая аренда: 0.2–0.5 c после последнего толчка
        }
    
        [ServerRpc]
        private void ReturnToServerAuth(NetworkObject no)
        {
            if (no != null)
                no.RemoveOwnership(); // сервер снова единственный авторитет
        }

        public void TryInteract()
        {
            if (_held != null)
            {
                if(!_held.TryGetComponent<NetworkObject>(out var networkObject) || networkObject.Owner.ClientId != Owner.ClientId)
                    return;
                
                DropHeld(); 
                ReturnToServerAuth(networkObject);
                return;
            }

            if (Physics.Raycast(cam.position, cam.forward, out var hit, config.UseDistance, interactMask, QueryTriggerInteraction.Ignore))
            {
                var rbHit = hit.rigidbody;
                if (rbHit != null && rbHit.mass <= 25f) // ASSUMPTION: лимит массы переносимых предметов
                {
                    if (!rbHit.TryGetComponent<NetworkObject>(out var networkObject) || networkObject.Owner.ClientId != -1) 
                    {
                        return;
                    }
                    // [FIX] Нам нужна информация о 'hit', чтобы знать, куда мы попали
                    GiveOwnership(networkObject, Owner);
                    Grab(rbHit, hit);
                }
            }
        }

        public void TryThrow()
        {
            if (_held == null) return;
            
            var dir = cam.forward;
            
            // [FIX] Сначала запоминаем тело, *потом* отпускаем, *потом* применяем силу
            var heldBody = _held; 
            DropHeld();
            heldBody.AddForce(dir * config.ThrowImpulse, ForceMode.VelocityChange);
            
            if (heldBody.TryGetComponent<NetworkObject>(out var networkObject) && networkObject.Owner.ClientId == Owner.ClientId)
            {
                ReturnToServerAuth(networkObject);
            }
        }

        // [FIX] Принимаем 'hit', чтобы знать точку контакта
        private void Grab(Rigidbody target, RaycastHit hit)
        {
            _held = target;
            _held.interpolation = RigidbodyInterpolation.Interpolate;
            _held.useGravity = true; // Гравитация будет давать "ощущение веса"

            _joint = gameObject.AddComponent<ConfigurableJoint>();
            _joint.connectedBody = target;
            _joint.autoConfigureConnectedAnchor = false;
            
            // [FIX] Устанавливаем якорь на игроке в *начальную* целевую позицию (в локальных координатах)
            Vector3 targetPos = cam.position + cam.forward * config.HoldDistance;
            _joint.anchor = transform.InverseTransformPoint(targetPos);
            
            // [FIX] Устанавливаем якорь на объекте в *точку попадания* (в локальных координатах объекта)
            _joint.connectedAnchor = target.transform.InverseTransformPoint(hit.point); 
            
            _joint.xMotion = ConfigurableJointMotion.Limited;
            _joint.yMotion = ConfigurableJointMotion.Limited;
            _joint.zMotion = ConfigurableJointMotion.Limited;
            _joint.angularXMotion = ConfigurableJointMotion.Limited;
            _joint.angularYMotion = ConfigurableJointMotion.Limited;
            _joint.angularZMotion = ConfigurableJointMotion.Limited;

            Soft(_joint, config.HoldSpring, config.HoldDamper);
        }

        private void DropHeld()
        {
            if (_joint != null) Destroy(_joint);
            _joint = null;
            
            if (_held != null)
            {
                // [FIX] Сбрасываем интерполяцию
                _held.interpolation = RigidbodyInterpolation.None; 
            }
            _held = null;
        }

        void FixedUpdate()
        {
            if (_joint == null || _held == null || cam == null) return;
            
            Vector3 targetPos = cam.position + cam.forward * config.HoldDistance;
            _joint.anchor = transform.InverseTransformPoint(targetPos);
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