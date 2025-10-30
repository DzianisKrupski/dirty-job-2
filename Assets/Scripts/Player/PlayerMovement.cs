#nullable enable
using UnityEngine;
using System;
using FishNet.Object;

namespace Player
{
    public enum MotorState
    {
        Grounded,
        Air,
        Slide,
        Crawl
    }

    [DisallowMultipleComponent]
    public sealed class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private MovementConfig config = default!;
        [SerializeField] private Rigidbody rb = default!;
        [SerializeField] private CapsuleCollider capsule = default!;
        [SerializeField] private LayerMask groundMask;

        public event Action<MotorState>? OnStateChanged;
        public MotorState CurrentState => _state;

        private MotorState _state;
        private Vector2 _moveInput;
        private bool _crouchHeld;
        private float _springLockUntil;
        private float _lastGrounded;
        private float _lastJumpPressed;
        private Vector3 _groundNormal = Vector3.up;
        private bool _isGrounded;
        private readonly RaycastHit[] _hitBuf = new RaycastHit[2];
        private LookPhysicsState? _lookPhysicsState;
        
        private float _deltaYaw;
        private float _yaw;
        private float _deltaPitch;
        private float _pitch;

        private void OnEnable()
        {
            if (config != null) config.OnChanged += ApplyImmediateConfigChanges;
        }
        private void OnDisable()
        {
            if (config != null) config.OnChanged -= ApplyImmediateConfigChanges;
        }
        
        public void Initialize(float yaw, float pitch, LookPhysicsState lookPhysicsState)
        {
            SetHeight(config.StandHeight, true);
            SwitchState(MotorState.Air);

            _lookPhysicsState = lookPhysicsState;
            _yaw = yaw;
            _pitch = pitch;
        }

        private void ApplyImmediateConfigChanges()
        {
            // Мгновенно подгоняем геометрию и подвес под новые параметры
            bool crouched = (_state == MotorState.Crawl || _state == MotorState.Slide);
            float targetH = crouched ? config.CrawlHeight : config.StandHeight;
            SetHeight(targetH, true);
        }

        public void SetMoveInput(Vector2 move) => _moveInput = Vector2.ClampMagnitude(move, 1f);
        
        public void SetJump(bool pressed)
        {
            if (pressed) _lastJumpPressed = Time.time;
        }
        public void SetCrouch(bool held) => _crouchHeld = held;
        
        public void SetLookInput(Vector2 look)
        {
            _deltaYaw += look.x * config.LookSensitivity;
            _deltaPitch -= look.y * config.LookSensitivity;
        }

        public void FixedUpdate()
        {
            UpdateGround();
            ApplyHoverSpring();

            _yaw += _deltaYaw;
            _deltaYaw = 0f;
            _deltaPitch = Mathf.Clamp(_deltaPitch, config.PitchMin, config.PitchMax);
            _pitch = Mathf.Clamp(_pitch + _deltaPitch, -89f, 89f);
            _deltaPitch = 0f;
            rb.MoveRotation(Quaternion.Euler(0f, _yaw, 0f));
            _lookPhysicsState?.PushFixed(_yaw, _pitch);

            switch (_state)
            {
                case MotorState.Grounded:
                    GroundMove();
                    TryJump();
                    if (_crouchHeld && rb.linearVelocity.magnitude > config.SlideMinSpeed) SwitchState(MotorState.Slide);
                    else if (_crouchHeld) SwitchState(MotorState.Crawl);
                    if (!_isGrounded) SwitchState(MotorState.Air);
                    break;

                case MotorState.Air:
                    AirMove();
                    if (_isGrounded) SwitchState(MotorState.Grounded);
                    break;

                case MotorState.Slide:
                    SlideMove();
                    if (!_crouchHeld) SwitchState(_isGrounded ? MotorState.Grounded : MotorState.Air);
                    break;

                case MotorState.Crawl:
                    CrawlMove();
                    if (!_crouchHeld && CanStandUp()) SwitchState(_isGrounded ? MotorState.Grounded : MotorState.Air);
                    if (!_isGrounded) SwitchState(MotorState.Air);
                    break;
            }
            
            UpdateHeight();
        }

        private void UpdateGround()
        {
            var origin = rb.worldCenterOfMass;
            float maxDist = config.RideHeight + 0.6f;

            int hits = Physics.SphereCastNonAlloc(origin, config.GroundCheckRadius, Vector3.down, _hitBuf, maxDist, groundMask, QueryTriggerInteraction.Ignore);
            _isGrounded = false;
            _groundNormal = Vector3.up;

            for (int i = 0; i < hits; i++)
            {
                var h = _hitBuf[i];
                float angle = Vector3.Angle(h.normal, Vector3.up);
                if (angle <= config.MaxSlopeAngle)
                {
                    _isGrounded = true;
                    _groundNormal = h.normal;
                    _lastGrounded = Time.time;
                    break;
                }
            }
        }
        
        private static Vector3 GetGroundVel(in RaycastHit hit)
        {
            return (hit.rigidbody != null) ? hit.rigidbody.GetPointVelocity(hit.point) : Vector3.zero;
        }

        private void ApplyHoverSpring()
        {
            if (Time.time < _springLockUntil) return; // блок пружины сразу после прыжка

            var origin = rb.worldCenterOfMass;
            if (!Physics.Raycast(origin, Vector3.down, out var hit, config.RideHeight + 1f, groundMask, QueryTriggerInteraction.Ignore))
                return;

            float error = config.RideHeight - hit.distance;           // >0 — ниже целевой высоты
            if (error <= config.HoverTolerance) return;                // мёртвая зона и запрещаем тянуть вниз

            Vector3 groundVel = GetGroundVel(hit);
            float relVelN = Vector3.Dot(rb.linearVelocity - groundVel, hit.normal);
            relVelN = Mathf.Min(relVelN, 0f);                          // демпфим только движение В СТОРОНУ поверхности

            float k = config.SpringK;
            float c = 2f * Mathf.Sqrt(Mathf.Max(1f, k));               // крит. демпфирование
            float accel = (error * k) - (relVelN * c);
            accel = Mathf.Clamp(accel, 0f, config.MaxSpringAcceleration); // только вверх

            rb.AddForce(hit.normal * accel, ForceMode.Acceleration);

            if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                hit.rigidbody.AddForceAtPosition(-hit.normal * accel, hit.point, ForceMode.Acceleration);
        }

        private void GroundMove()
        {
            Vector3 wish = GetWishOnPlane(_groundNormal) * config.MoveSpeed;
            AccelerateToward(wish, config.Accel, config.MaxGroundSpeed);
            ApplyPlanarFriction(5f);
        }

        private void AirMove()
        {
            Vector3 wish = GetWishOnPlane(Vector3.up) * config.MaxAirSpeed;
            AccelerateToward(wish, config.AirAccel, config.MaxAirSpeed);
        }

        private void SlideMove()
        {
            SetHeight(Mathf.Lerp(config.StandHeight, config.CrawlHeight, 0.7f), false);
            Vector3 planeVel = Vector3.ProjectOnPlane(rb.linearVelocity, _groundNormal);
            rb.AddForce(-planeVel * config.SlideFriction, ForceMode.Acceleration);
            Vector3 gravityAlong = Vector3.ProjectOnPlane(Physics.gravity, _groundNormal);
            rb.AddForce(gravityAlong, ForceMode.Acceleration);

            if (rb.linearVelocity.magnitude < config.CrawlSpeed * 1.2f) SwitchState(MotorState.Crawl);
        }

        private void CrawlMove()
        {
            SetHeight(config.CrawlHeight, false);
            Vector3 wish = GetWishOnPlane(_groundNormal) * config.CrawlSpeed;
            AccelerateToward(wish, config.Accel * 0.6f, config.CrawlSpeed);
            ApplyPlanarFriction(6f);
        }

        private void TryJump()
        {
            bool buffered = (Time.time - _lastJumpPressed) <= config.JumpBuffer;
            bool coyote = (Time.time - _lastGrounded) <= config.CoyoteTime;
            if (!(buffered && coyote)) return;

            _lastJumpPressed = -999f;
            Vector3 v = rb.linearVelocity;
            if (v.y < 0f) v.y = 0f;
            rb.linearVelocity = v;

            float jumpVel = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * config.JumpHeight);
            rb.AddForce(Vector3.up * jumpVel, ForceMode.VelocityChange);
            _springLockUntil = Time.time + config.SpringDisableAfterJump;

            SwitchState(MotorState.Air);
        }

        private void AccelerateToward(Vector3 targetVel, float accel, float maxSpeed)
        {
            Vector3 curPlanar = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
            Vector3 tgtPlanar = Vector3.ClampMagnitude(new Vector3(targetVel.x, 0f, targetVel.z), maxSpeed);

            Vector3 delta = tgtPlanar - curPlanar;
            Vector3 accelVec = Vector3.ClampMagnitude(delta, accel * Time.fixedDeltaTime) / Time.fixedDeltaTime;
            rb.AddForce(accelVec, ForceMode.Acceleration);
        }

        private void ApplyPlanarFriction(float strength)
        {
            Vector3 planar = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
            rb.AddForce(-planar * strength, ForceMode.Acceleration);
        }

        private Vector3 GetWishOnPlane(Vector3 planeNormal)
        {
            Vector3 f = transform.forward; // // ASSUMPTION: transform синхронизирован с камерой-поворотом
            Vector3 r = transform.right;
            Vector3 wish = (f * _moveInput.y + r * _moveInput.x);
            return Vector3.ProjectOnPlane(wish, planeNormal).normalized;
        }

        private void SwitchState(MotorState s)
        {
            if (_state == s) return;
            _state = s;
            OnStateChanged?.Invoke(_state);
        }

        private void UpdateHeight()
        {
            float target = (_state == MotorState.Crawl || _state == MotorState.Slide) ? config.CrawlHeight : config.StandHeight;
            float lerp = 1f - Mathf.Exp(-config.HeightLerpSpeed * Time.fixedDeltaTime);
            SetHeight(Mathf.Lerp(GetCurrentHeight(), target, lerp), false);
        }

        private float GetCurrentHeight() => capsule.height;

        private void SetHeight(float height, bool force)
        {
            if (!force && Mathf.Approximately(capsule.height, height)) return;
            float prev = capsule.height;
            capsule.height = height;
            capsule.center = new Vector3(0f, height * 0.5f, 0f);

            float diff = (height - prev) * 0.5f;
            rb.position += Vector3.up * diff; // сохраняем подвес относительно COM
        }

        private bool CanStandUp()
        {
            float stand = config.StandHeight;
            Vector3 p = transform.position + Vector3.up * (config.CrawlHeight * 0.5f);
            float cast = (stand - config.CrawlHeight) + 0.05f;
            return !Physics.CapsuleCast(p, p, capsule.radius * 0.95f, Vector3.up, cast, groundMask, QueryTriggerInteraction.Ignore);
        }
    }
}