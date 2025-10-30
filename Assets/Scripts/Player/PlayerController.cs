#nullable enable
using UnityEngine;
using System;
using FishNet.Object;

namespace Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerController : NetworkBehaviour
    {
        [SerializeField] private PlayerInputController playerInput = default!;
        [SerializeField] private PlayerMovement movement = default!;
        [SerializeField] private PlayerInteractor interactor = default!;
        [SerializeField] private CameraController cameraController = default!;

        private readonly LookPhysicsState _lookPhysicsState = new LookPhysicsState();
        
        public event Action<MotorState>? OnStateChanged;

        public override void OnStartNetwork()
        {
            if (!Owner.IsLocalClient)
            {
                cameraController.gameObject.SetActive(false);
                movement.enabled = false;
                interactor.enabled = false;
                playerInput.enabled = false;
                enabled = false;
            }
            
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
                if(inputData.throwObject) interactor.TryThrow();
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