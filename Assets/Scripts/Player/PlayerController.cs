#nullable enable
using UnityEngine;
using System;

namespace Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputController playerInput = default!;
        [SerializeField] private PlayerMovement movement = default!;
        [SerializeField] private PlayerInteractor interactor = default!;
        [SerializeField] private CameraController cameraController = default!;

        private readonly LookPhysicsState _lookPhysicsState = new LookPhysicsState();
        
        public event Action<MotorState>? OnStateChanged;

        private void Awake()
        {
            if (movement != null)
            {
                movement.Initialize(0f, 0f, _lookPhysicsState);
                movement.OnStateChanged += s => OnStateChanged?.Invoke(s);
            }

            if (cameraController != null)
            {
                cameraController.Initialize(_lookPhysicsState);
            }
        }

        private void FixedUpdate()
        {
            if (movement == null) return;
            
            PlayerInputData inputData = playerInput.GetInputs();
            movement.SetMoveInput(inputData.moveAccum);
            movement.SetCrouch(inputData.crouch);
            movement.SetJump(inputData.jump);
            movement.SetLookInput(inputData.lookAccum);

            if (interactor != null)
            {
                if(inputData.interact) interactor.TryInteract();
            }
            
            playerInput.ResetInputs();
        }
        
        void Update()
        {
            if (cameraController == null || movement == null) return;
            
            bool crouched = movement.CurrentState == MotorState.Crawl || movement.CurrentState == MotorState.Slide;
            cameraController.SetCharacterPosition(movement.transform.position, crouched);
        }
    }
}