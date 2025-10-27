using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private SimpleCharacterMotor characterController;
    [SerializeField] private float  moveSpeed = 3f;
        
    private InputAction _moveAction;
    private InputAction _lookAction;
    private Vector3 _moveVelocity;
    private Vector3 _lookVelocity;

    public void OnEnable()
    {
        var playerMap = InputSystem.actions.FindActionMap("Player", true);
        playerMap.Enable();
        _moveAction = playerMap.FindAction("Move", true);
        _lookAction = playerMap.FindAction("Look", true);
    }

    public void OnDisable()
    {
        var playerMap = InputSystem.actions.FindActionMap("Player", true);
        playerMap.Disable();
    }

    private void Update()
    {
        _moveVelocity = _moveAction.ReadValue<Vector2>();
        characterController.MoveInput = _moveVelocity;
    }
}