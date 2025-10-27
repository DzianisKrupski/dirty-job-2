using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private SimpleCharacterMotor characterController;
    [SerializeField] private Camera camera;
        
    private InputAction _moveAction;
    private InputAction _lookAction;
    private Vector3 _moveVelocity;
    private Vector3 _lookVelocity;

    public override void OnNetworkSpawn()
    {
        camera.gameObject.SetActive(IsOwner);
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
        characterController.MoveInput = _moveVelocity;
        
        _lookVelocity = _lookAction.ReadValue<Vector2>();
        characterController.LookInput = _lookVelocity;
    }
    
}