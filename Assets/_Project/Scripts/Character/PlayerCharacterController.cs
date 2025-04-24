using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(Health))]
public class PlayerCharacterController : MonoBehaviour
{
    public event Action OnDeath;
    public event Action OnDeathAnimationComplete;
    
    public enum PlayerState
    {
        Idle,
        Moving,
        Dead
    }
    
    [SerializeField] private float _maxMoveSpeed = 5f;
    [SerializeField] private float _movementSharpness = 15f;
    [SerializeField] private float _rotationSharpness = 15f;
    [SerializeField] private float _gravity = 30f;
    
    private CharacterController _characterController;
    private PlayerBoundaryConstraint _boundaryConstraint;
    private PlayerAnimationEventProxy _animationEventProxy;
    private Animator _animator;
    private Health _health;
    private Camera _camera;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private Vector3 _cameraRelativeInput;
    public Vector3 CurrentVelocity => _characterController.velocity;
    private Vector3 _currentVelocity;
    private Quaternion _currentRotation;
    
    private PlayerState _currentState = PlayerState.Idle;
    
    public bool IsAlive => !_health.IsDead;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _boundaryConstraint = GetComponent<PlayerBoundaryConstraint>();
        _animationEventProxy = GetComponentInChildren<PlayerAnimationEventProxy>();
        _animator = GetComponentInChildren<Animator>();
        _health = GetComponent<Health>();
    }

    private void Start()
    {
        // Cache the ref to camera
        _camera = Camera.main;

        _health.OnHealthReachedZero += HandlePlayerHealthReachedZero;
        _animationEventProxy.AnimationDieCompletedEvent += HandleAnimationDieCompleted;
    }

    private void Update()
    {
        if (GameplayManager.Instance.IsGamePaused) return;
        
        if (_health.IsDead)
        {
            SetState(PlayerState.Dead);
        }

        switch (_currentState)
        {
            case PlayerState.Idle:
                HandleIdle();
                break;
            case PlayerState.Moving:
                HandleMoving();
                break;
            case PlayerState.Dead:
                // No logic
                break;
        }
    }

    private void LateUpdate()
    {
        if (_currentState != PlayerState.Dead)
        {
            RotateTowardTargetDirection();
        }
    }

    public void SetMoveInput(Vector2 moveInput)
    {
        if (_currentState == PlayerState.Dead) return;
        
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
        
        // Update state based on movement input
        if (_cameraRelativeInput.magnitude > 0.01f)
        {
            SetState(PlayerState.Moving);
        }
        else
        {
            SetState(PlayerState.Idle);
        }
    }

    public void SetLookInput(Vector2 lookInput)
    {
        if (_currentState == PlayerState.Dead) return;
        
        _lookInput = lookInput;
    }
    
    private void SetState(PlayerState newState, bool force = false)
    {
        if (!force && _currentState == newState) return;
        
        Debug.Log($"Player state changed from {_currentState} to {newState}");
        _currentState = newState;

        switch (_currentState)
        {
            case PlayerState.Idle:
                _animator.SetFloat(AnimatorParameters.VelocityX, 0f);
                _animator.SetFloat(AnimatorParameters.VelocityZ, 0f);
                break;
                
            case PlayerState.Moving:
                // No logic
                break;
                
            case PlayerState.Dead:
                _animator.SetTrigger(AnimatorParameters.Die);
                OnDeath?.Invoke();
                break;
        }
    }

    private void HandleIdle()
    {
        ApplyGravity();
        
        _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero,
            1f - Mathf.Exp(-_movementSharpness * Time.deltaTime));
        
        _characterController.Move(_currentVelocity * Time.deltaTime);
    }

    private void HandleMoving()
    {
        Vector3 targetVelocity = _cameraRelativeInput.normalized * _maxMoveSpeed;
        
        if (_boundaryConstraint != null)
        {
            targetVelocity = _boundaryConstraint.ConstrainVelocityToBounds(targetVelocity);
        }
        
        _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity,
            1f - Mathf.Exp(-_movementSharpness * Time.deltaTime));
        
        ApplyGravity();
        
        _characterController.Move(_currentVelocity * Time.deltaTime);
        
        _animator.SetFloat(AnimatorParameters.VelocityX, _currentVelocity.x);
        _animator.SetFloat(AnimatorParameters.VelocityZ, _currentVelocity.z);
    }
    
    private void ApplyGravity()
    {
        if (!_characterController.isGrounded)
        {
            _currentVelocity -= _characterController.transform.up * (_gravity * Time.deltaTime);
        }
    }

    private void RotateTowardTargetDirection()
    {
        Vector3 targetDirection = new Vector3(_lookInput.x, 0, _lookInput.y).normalized;
        
        if (targetDirection.magnitude > 0.01f)
        {
            Vector3 smoothedRotateDirection = Vector3.Slerp(transform.forward, targetDirection, 
                1f - Mathf.Exp(-_rotationSharpness * Time.deltaTime)).normalized;
            _currentRotation = Quaternion.LookRotation(smoothedRotateDirection);
            transform.rotation = _currentRotation;
        }
    }
    
    private void HandlePlayerHealthReachedZero()
    {
        SetState(PlayerState.Dead);
    }

    private void HandleAnimationDieCompleted()
    {
        OnDeathAnimationComplete?.Invoke();
    }
}
