using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMovementController : MonoBehaviour
{
    [SerializeField] private float _maxMoveSpeed = 5f;
    [SerializeField] private float _movementSharpness = 15f;
    [SerializeField] private float _rotationSharpness = 15f;
    [SerializeField] private float _gravity = 30f;
    
    private CharacterController _characterController;
    private Camera _camera;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private Vector3 _cameraRelativeInput;
    public Vector3 CurrentVelocity => _characterController.velocity;
    private Vector3 _currentVelocity;
    private Quaternion _currentRotation;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        // Cache the ref to camera
        _camera = Camera.main;
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
        RotateTowardMovementDirection();
    }

    public void SetMoveInput(Vector2 moveInput)
    {
        _moveInput = moveInput;
        
        Vector3 forward = _camera.transform.TransformDirection(Vector3.forward);
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
            Vector3 smoothedRotateDirection = Vector3.Slerp(transform.forward, movementVector, 1f - Mathf.Exp(-_rotationSharpness * Time.deltaTime)).normalized;
            _currentRotation = Quaternion.LookRotation(smoothedRotateDirection);
        }
        
        transform.rotation = _currentRotation;
    }

    private void RotateTowardTargetDirection()
    {
        Vector3 targetDirection = new Vector3(_lookInput.x, 0, _lookInput.y).normalized;
        Vector3 smoothedRotateDirection = Vector3.Slerp(transform.forward, targetDirection, 1f - Mathf.Exp(-_rotationSharpness * Time.deltaTime)).normalized;
        _currentRotation = Quaternion.LookRotation(smoothedRotateDirection);
        transform.rotation = _currentRotation;
    }
}
