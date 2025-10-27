using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private void Awake()
    {
        InputSystem.actions.actionMaps[0].Enable();
    }
}