#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private bool analogMove = true;

        private Vector2 _move;
        private bool _jump;
        private bool _crouch;
        private bool _interact;
        private bool _throw;

        public Vector2 Move => analogMove ? Vector2.ClampMagnitude(_move, 1f) : _move.normalized;
        public bool JumpPressed => _jump;
        public bool CrouchHeld => _crouch;
        public bool InteractPressed => Consume(ref _interact);
        public bool ThrowPressed => Consume(ref _throw);

        public void OnMove(InputValue v) => _move = v.Get<Vector2>();
        public void OnJump(InputValue v) => _jump = v.isPressed;
        public void OnCrouch(InputValue v) => _crouch = v.isPressed;
        public void OnInteract(InputValue v) => _interact = v.isPressed;
        public void OnThrow(InputValue v) => _throw = v.isPressed;

        private static bool Consume(ref bool b) { bool v = b; b = false; return v; }
    }
}