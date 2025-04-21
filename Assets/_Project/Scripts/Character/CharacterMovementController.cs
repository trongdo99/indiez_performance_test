using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMovementController : MonoBehaviour
{
    [SerializeField] private float _maxMoveSpeed = 5f;
    [SerializeField] private float _movementSharpness = 15f;
    [SerializeField] private float _rotationSharpness = 15f;
    [SerializeField] private float _gravity = 30f;
    [SerializeField] private Transform _modelRoot;
    
    private CharacterController _characterController;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private Vector3 _cameraRelativeInput;
    private Vector3 _currentVelocity;
    private Quaternion _currentRotation;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Vector3 targetVelocity = _cameraRelativeInput.normalized * _maxMoveSpeed;
        _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity,
            1f - Mathf.Exp(-_movementSharpness * Time.deltaTime));
        
        // Apply gravity.
        if (!_characterController.isGrounded)
        {
            _currentVelocity -= _characterController.transform.up * _gravity;
        }
        
        _characterController.Move(_currentVelocity * Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (_lookInput.magnitude > 0.01f)
        {
            Vector3 targetDirection = new Vector3(_lookInput.x, 0, _lookInput.y).normalized;
            Vector3 smoothedRotateDirection = Vector3.Slerp(_modelRoot.forward, targetDirection, 1f - Mathf.Exp(-_rotationSharpness * Time.deltaTime)).normalized;
            _currentRotation = Quaternion.LookRotation(smoothedRotateDirection);
        }
        
        _modelRoot.rotation = _currentRotation;
    }

    public void SetMoveInput(Vector2 moveInput)
    {
        _moveInput = moveInput;
        
        Vector3 forward = Camera.main.transform.TransformDirection(Vector3.forward);
        forward.y = 0f;
        forward.Normalize();

        var right = new Vector3(forward.z, 0f, -forward.x);
        Vector3 cameraRelativeInput = _moveInput.x * right + moveInput.y * forward;
        if (cameraRelativeInput.magnitude > 1f)
        {
            cameraRelativeInput.Normalize();
        }
        
        _cameraRelativeInput = cameraRelativeInput;
    }

    public void SetLookInput(Vector2 lookInput)
    {
        _lookInput = lookInput;
    }

    private void RotateTowardMovementDirection()
    {
        var movementVector = new Vector3(_currentVelocity.x, 0f, _currentVelocity.z);
        if (movementVector.magnitude > 0.01f)
        {
            Vector3 smoothedRotateDirection = Vector3.Slerp(_modelRoot.forward, movementVector, 1f - Mathf.Exp(-_rotationSharpness * Time.deltaTime)).normalized;
            _currentRotation = Quaternion.LookRotation(smoothedRotateDirection);
        }
        
        _modelRoot.rotation = _currentRotation;
    }
}
