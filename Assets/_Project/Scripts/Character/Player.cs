using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : MonoBehaviour
{
    [SerializeField] private InputReader _input;
    [FormerlySerializedAs("_characterMovementController")] [SerializeField] private PlayerCharacterController _playerCharacterController;
    
    public Transform PlayerCharacterTransform => _playerCharacterTransform;
    private Transform _playerCharacterTransform;

    private void Start()
    {
        _input.EnablePlayerActions();
        
        // Find a way to get the ref to CharacterMovementController in proper way later.
        if (_playerCharacterController == null)
        {
            _playerCharacterController = GameObject.FindFirstObjectByType<PlayerCharacterController>();
        }
        
        _playerCharacterTransform = _playerCharacterController.transform;
    }

    private void Update()
    {
        if (_playerCharacterController != null)
        {
            _playerCharacterController.SetMoveInput(_input.MoveInput);
            _playerCharacterController.SetLookInput(_input.LookInput);
        }
    }
}
