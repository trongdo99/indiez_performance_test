using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private InputReader _input;
    [SerializeField] private CharacterMovementController _characterMovementController;

    private void Start()
    {
        _input.EnablePlayerActions();
        
        // Find a way to get the ref to CharacterMovementController in proper way later.
        if (_characterMovementController == null)
        {
            _characterMovementController = GameObject.FindFirstObjectByType<CharacterMovementController>();
        }
    }

    private void Update()
    {
        if (_characterMovementController != null)
        {
            _characterMovementController.SetMoveInput(_input.MoveInput);
            _characterMovementController.SetLookInput(_input.LookInput);
        }
    }
}
