#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [Serializable]
    public struct PlayerInputData
    {
        public Vector2 move;
        public Vector2 moveAccum;
        public Vector2 look;
        public Vector2 lookAccum;
        public bool jump;
        public bool crouch;
        public bool interact;
    }
    
    public class PlayerInputController : MonoBehaviour
    {
        [SerializeField] private PlayerInput playerInput = default!;
        
        private InputAction? _move;
        private InputAction? _look;
        private InputAction? _jump;
        private InputAction? _crouch;
        private InputAction? _interact;
        private PlayerInputData _input;

        private bool _jumpTrigger;
        private bool _interactTrigger;
        private Vector2 _moveAccum;
        private Vector2 _lookAccum;

        private float _inputDelay;
        
        public PlayerInputData GetInputs() => _input;

        public void ResetInputs()
        {
            _jumpTrigger = false;
            _interactTrigger = false;
            _moveAccum = Vector2.zero;
            _lookAccum = Vector2.zero;
        }

        private void OnEnable()
        {
            if (playerInput != null)
            {
                var actions = playerInput.actions;
                
                _move = actions.FindAction("Move",true);
                _look = actions.FindAction("Look",true);
                _jump = actions.FindAction("Jump",true);
                _crouch = actions.FindAction("Crouch",true);
                _interact = actions.FindAction("Interact",true);
            }

            if (_jump != null) _jump.started += OnJumpStarted;
            if (_interact != null) _interact.started += OnInteractStarted;
        }

        private void OnDisable()
        {
            if (_jump != null) _jump.started -= OnJumpStarted;
            if (_interact != null) _interact.started -= OnInteractStarted;
        }
        
        private void OnJumpStarted(InputAction.CallbackContext ctx) => _jumpTrigger = true;
        private void OnInteractStarted(InputAction.CallbackContext ctx) => _interactTrigger = true;

        private void Update()
        {
            if (_inputDelay < 1f)
            {
                _inputDelay += Time.deltaTime;
                return;
            }

            Vector2 move = _move?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector2 look = _look?.ReadValue<Vector2>() ?? Vector2.zero;
            _moveAccum += move;
            _lookAccum += look;
            
            _input = new PlayerInputData
            {
                move = move,
                moveAccum = _moveAccum,
                look = look,
                lookAccum = _lookAccum,
                jump = _jumpTrigger,
                crouch = _crouch != null && _crouch.IsPressed(),
                interact = _interactTrigger
            };
        }
    }
}