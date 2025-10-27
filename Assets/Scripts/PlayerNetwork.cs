using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float  moveSpeed = 3f;
        
    private InputAction _moveAction;
    private InputAction _lookAction;
    private Vector3 _moveVelocity;
    private Vector3 _lookVelocity;

    public override void OnNetworkSpawn()
    {
        if(!IsOwner) return;

        var playerMap = InputSystem.actions.FindActionMap("Player", true);
        playerMap.Enable();
        _moveAction = playerMap.FindAction("Move", true);
        _lookAction = playerMap.FindAction("Look", true);
    }

    public override void OnNetworkDespawn()
    {
        if(!IsOwner) return;
        
        var playerMap = InputSystem.actions.FindActionMap("Player", true);
        playerMap.Disable();
    }

    private void Update()
    {
        if(!IsOwner) return;
        
        _moveVelocity = _moveAction.ReadValue<Vector2>();
        characterController.Move(_moveVelocity * (moveSpeed * Time.deltaTime));
    }
}