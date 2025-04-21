using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "Input/InputReader")]
public class InputReader : ScriptableObject, PlayerInputSystem_Actions.IPlayerActions
{
    public event Action<Vector2> MoveEvent;
    
    public Vector2 MoveInput => _inputActions.Player.Move.ReadValue<Vector2>();
    public Vector2 LookInput => _inputActions.Player.Look.ReadValue<Vector2>();
    
    private PlayerInputSystem_Actions _inputActions;
    
    public void EnablePlayerActions()
    {
        if (_inputActions == null)
        {
            _inputActions = new PlayerInputSystem_Actions();
            _inputActions.Player.SetCallbacks(this);
        }
        _inputActions.Enable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // noop
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        // noop
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        // noop
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
        // noop
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        // noop
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        // noop
    }
}
