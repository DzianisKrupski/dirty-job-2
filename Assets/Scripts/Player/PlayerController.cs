#nullable enable
using UnityEngine;
using System;

namespace Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputController input = default!;
        [SerializeField] private PlayerMovement movement = default!;
        [SerializeField] private PlayerInteractor interactor = default!;

        public event Action<MotorState>? OnStateChanged;

        void Awake()
        {
            if (movement != null)
                movement.OnStateChanged += s => OnStateChanged?.Invoke(s);
        }

        void Update()
        {
            // Роутинг ввода в движение
            if (input == null || movement == null) return;
            movement.SetMoveInput(input.Move);
            movement.SetJump(input.JumpPressed);
            movement.SetCrouch(input.CrouchHeld);

            // Роутинг ввода во взаимодействие
            if (interactor != null)
            {
                if (input.InteractPressed) interactor.TryInteract();
                if (input.ThrowPressed) interactor.TryThrow();
            }
        }
    }
}
