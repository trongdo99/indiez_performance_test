using System;
using UnityEngine;

public enum PlayerStateType
{
    Idle,
    Moving,
    Dead
}

[RequireComponent(typeof(CharacterController), typeof(Health), typeof(TargetFinder))]
public class PlayerCharacterController : StateMachine<PlayerStateType>
{
    public event Action OnDeath;
    public event Action OnDeathAnimationComplete;
    
    [Header("Movement Settings")]
    [SerializeField] private float _maxMoveSpeed = 5f;
    [SerializeField] private float _movementSharpness = 15f;
    [SerializeField] private float _rotationSharpness = 15f;
    [SerializeField] private float _gravity = 30f;
    
    [Header("Input")]
    [SerializeField] private InputReader _input;
    
    private CharacterController _characterController;
    private PlayerBoundaryConstraint _boundaryConstraint;
    private PlayerAnimationEventProxy _animationEventProxy;
    private ThrowWeaponController _throwWeaponController;
    private TargetFinder _targetFinder;
    private WeaponController _weaponController;
    private Animator _animator;
    private Health _health;
    private Camera _camera;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private Vector3 _cameraRelativeInput;
    
    public Vector3 CurrentVelocity => _characterController.velocity;
    public float MaxHealth => _health.MaxHealth;
    public float CurrentHealth => _health.CurrentHealth;
    public bool IsAlive => !_health.IsDead;
    public bool IsThrowing => _throwWeaponController.IsThrowingAnimation;
    public bool CanAim => IsAlive && CurrentStateType != PlayerStateType.Dead && !IsThrowing;

    // Properties for states to access
    public CharacterController CharacterController => _characterController;
    public PlayerBoundaryConstraint BoundaryConstraint => _boundaryConstraint;
    public Animator Animator => _animator;
    public Health Health => _health;
    public Camera MainCamera => _camera;
    public Vector2 MoveInput => _moveInput;
    public Vector2 LookInput => _lookInput;
    public Vector3 CameraRelativeInput => _cameraRelativeInput;
    public Vector3 CurrentVelocityRef { get; set; }
    public Quaternion CurrentRotationRef { get; set; }
    public float MaxMoveSpeed => _maxMoveSpeed;
    public float MovementSharpness => _movementSharpness;
    public float RotationSharpness => _rotationSharpness;
    public float Gravity => _gravity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _boundaryConstraint = GetComponent<PlayerBoundaryConstraint>();
        _animationEventProxy = GetComponentInChildren<PlayerAnimationEventProxy>();
        _animator = GetComponentInChildren<Animator>();
        _throwWeaponController = GetComponent<ThrowWeaponController>();
        _targetFinder = GetComponent<TargetFinder>();
        _weaponController = GetComponent<WeaponController>();
        _health = GetComponent<Health>();

        // Initialize states
        _states[PlayerStateType.Idle] = new PlayerIdleState(this);
        _states[PlayerStateType.Moving] = new PlayerMovingState(this);
        _states[PlayerStateType.Dead] = new PlayerDeadState(this);
        
        // Set up targeting event handlers
        _targetFinder.OnTargetFound += HandleTargetFound;
        _targetFinder.OnTargetLost += HandleTargetLost;
    }

    private void Start()
    {
        // Cache the ref to camera
        _camera = Camera.main;

        _health.OnHealthReachedZero += HandlePlayerHealthReachedZero;
        _health.OnHealthChanged += HandleHealthChanged;
        _animationEventProxy.AnimationDieCompletedEvent += HandleAnimationDieCompleted;

        // Start in idle state
        ChangeState(PlayerStateType.Idle);
    }

    private void OnDestroy()
    {
        _health.OnHealthReachedZero -= HandlePlayerHealthReachedZero;
        _health.OnHealthChanged -= HandleHealthChanged;
        _animationEventProxy.AnimationDieCompletedEvent -= HandleAnimationDieCompleted;
        
        if (_targetFinder != null)
        {
            _targetFinder.OnTargetFound -= HandleTargetFound;
            _targetFinder.OnTargetLost -= HandleTargetLost;
        }
    }

    protected override void Update()
    {
        if (GameplayManager.Instance.IsGamePaused) return;
        
        if (_health.IsDead && CurrentStateType != PlayerStateType.Dead)
        {
            ChangeState(PlayerStateType.Dead);
        }

        base.Update();
        
        UpdateAiming();
    }
    
    private void UpdateAiming()
    {
        if (!CanAim) return;
        
        Vector3 aimDirection = new Vector3(_lookInput.x, 0, _lookInput.y).normalized;
        _targetFinder.FindTargetInDirection(aimDirection);
    }

    public void SetMoveInput(Vector2 moveInput)
    {
        if (CurrentStateType == PlayerStateType.Dead) return;
        
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
        
        if (_cameraRelativeInput.magnitude > 0.01f)
        {
            ChangeState(PlayerStateType.Moving);
        }
        else
        {
            ChangeState(PlayerStateType.Idle);
        }
    }

    public void SetLookInput(Vector2 lookInput)
    {
        if (CurrentStateType == PlayerStateType.Dead) return;
        
        _lookInput = lookInput;
    }

    public void Throw()
    {
        if (!CanAim) return;
        _animator.SetTrigger(AnimatorParameters.Throw);
        _throwWeaponController.StartThrowSequence();
    }

    public void SwitchWeapon()
    {
        if (!IsAlive) return;
        _weaponController.CycleThroughWeapons();
    }
    
    private void HandleTargetFound(Transform target)
    {
        if (!CanAim) return;
        _weaponController.Aiming(target);
    }
    
    private void HandleTargetLost()
    {
        _weaponController.StopAiming();
    }

    public void ApplyGravity()
    {
        if (!_characterController.isGrounded)
        {
            CurrentVelocityRef -= _characterController.transform.up * (_gravity * Time.deltaTime);
        }
    }

    public void RotateTowardTargetDirection()
    {
        if (IsThrowing) return;
        
        Vector3 targetDirection = new Vector3(_lookInput.x, 0, _lookInput.y).normalized;
        
        if (targetDirection.magnitude > 0.01f)
        {
            Vector3 smoothedRotateDirection = Vector3.Slerp(transform.forward, targetDirection, 
                1f - Mathf.Exp(-_rotationSharpness * Time.deltaTime)).normalized;
            CurrentRotationRef = Quaternion.LookRotation(smoothedRotateDirection);
            transform.rotation = CurrentRotationRef;
        }
    }
    
    private void HandlePlayerHealthReachedZero()
    {
        ChangeState(PlayerStateType.Dead);
    }

    private void HandleHealthChanged(float newHealth, float previousHealth)
    {
        EventBus.Instance.Publish<GameEvents.PlayerHealthChanged, EventData.PlayerHealthChangedData>(new EventData.PlayerHealthChangedData
        {
            PlayerController = this,
            NewHealth = newHealth,
            PreviousHealth = previousHealth,
        });
    }

    private void HandleAnimationDieCompleted()
    {
        OnDeathAnimationComplete?.Invoke();
    }

    public override void ChangeState(PlayerStateType newStateType)
    {
        // When changing to dead state, stop aiming
        if (newStateType == PlayerStateType.Dead)
        {
            _weaponController.StopAiming();
        }
        
        base.ChangeState(newStateType);
        
        if (newStateType == PlayerStateType.Dead)
        {
            OnDeath?.Invoke();
        }
    }
}