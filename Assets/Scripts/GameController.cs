using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
    private InputAction _backAction;
    
    private void OnEnable()
    {
        var uiMap = InputSystem.actions.FindActionMap("UI", true);
        uiMap.Enable();
        _backAction = uiMap.FindAction("Cursor", true);
        _backAction.performed += OnCursorPerformed;
    }

    private void OnDisable()
    {
        _backAction.performed -= OnCursorPerformed;
    }

    private void OnCursorPerformed(InputAction.CallbackContext obj)
    {
        bool locked = Cursor.lockState == CursorLockMode.Locked;
        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = !locked;
    }
}