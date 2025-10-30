#nullable enable
using UnityEngine;
using System;
using UnityEngine.InputSystem;

namespace Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputController playerInput = default!;
        [SerializeField] private PlayerMovement movement = default!;
        [SerializeField] private PlayerInteractor interactor = default!;
        
        public event Action<MotorState>? OnStateChanged;

        private void Awake()
        {
            if (movement != null)
                movement.OnStateChanged += s => OnStateChanged?.Invoke(s);
        }

        private void FixedUpdate()
        {
            PlayerInputData inputData = playerInput.GetInputs();
            
            movement.SetMoveInput(inputData.move);
            movement.SetCrouch(inputData.crouch);
            
            movement.SetJump(inputData.jump);

            if (interactor != null)
            {
                interactor.TryInteract();
            }
            
            playerInput.ResetInputs();
        }
    }
}