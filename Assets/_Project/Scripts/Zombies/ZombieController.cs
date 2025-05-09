using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public enum ZombieStateType
{
    Idle,
    Chasing,
    Attacking,
    Ragdoll,
    Dead
}

[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class ZombieController : StateMachine<ZombieStateType>, IPoolable
{
    [SerializeField] private ZombieHitBox _hitBox;
    [SerializeField] private ZombieAnimationEventProxy _animationEventProxy;
    [SerializeField] private Transform _aimAtPosition;
    [SerializeField] private LayerMask _groundLayerMask;
    
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _rotationSpeedWhileAttacking = 5f;
    [SerializeField] private float _attackCoolDown = 2f;
    [SerializeField] private float _damageToHealth = -20f;
    [SerializeField] private float _ragdollRecoveryTime = 3f;

    [SerializeField] private float _speedMultiplier = 1f;
    [SerializeField] private float _baseMovementSpeed = 1.5f;
    [SerializeField] private float _minSpeedVariation = 0.8f;
    [SerializeField] private float _maxSpeedVariation = 1.2f;
    [SerializeField] private float _baseRotationSpeed = 90f;
    [SerializeField] private float _minRotationSpeedVariation = 0.8f;
    [SerializeField] private float _maxRotationSpeedVariation = 1.2f;
    
    [SerializeField] private bool _debugDummy;

    private ZombieManager _zombieManager;
    private ZombieSoundController _zombieSoundController;
    private ZombieDissolveEffect _dissolveEffect;
    private Health _health;
    private Animator _animator;
    private NavMeshAgent _agent;
    private Collider _collider;
    private Rigidbody _rigidbody;
    private float _lastAttackTime;
    private Transform _target;
    private Health _targetHealth;
    private float _currentSpeedVariation;
    private float _effectiveMovementSpeed;
    private float _currentRotationSpeedVariation;
    private float _effectiveRotationSpeed;

    // Properties for states to access
    public ZombieHitBox HitBox => _hitBox;
    public ZombieAnimationEventProxy AnimationEventProxy => _animationEventProxy;
    public ZombieSoundController ZombieSoundController => _zombieSoundController;
    public Transform AimAtPosition => _aimAtPosition;
    public LayerMask GroundLayerMask => _groundLayerMask;
    public float AttackRange => _attackRange;
    public float RotationSpeedWhileAttacking => _rotationSpeedWhileAttacking;
    public float AttackCoolDown => _attackCoolDown;
    public float DamageToHealth => _damageToHealth;
    public float RagdollRecoveryTime => _ragdollRecoveryTime;
    public Health Health => _health;
    public Animator Animator => _animator;
    public NavMeshAgent Agent => _agent;
    public Collider Collider => _collider;
    public Rigidbody Rigidbody => _rigidbody;
    public Transform Target => _target;
    public Health TargetHealth => _targetHealth;
    public bool IsAlive => !_health.IsDead;
    public float LastAttackTime { get => _lastAttackTime; set => _lastAttackTime = value; }

    public float SpeedMultiplier
    {
        get => _speedMultiplier;
        set
        {
            _speedMultiplier = value;
            if (_animator != null)
            {
                _animator.speed = value;
            }
        }
    }

    private void Awake()
    {
        _zombieSoundController = GetComponent<ZombieSoundController>();
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _animator = GetComponentInChildren<Animator>();
        _collider = GetComponent<Collider>();
        _dissolveEffect = GetComponent<ZombieDissolveEffect>();
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.isKinematic = true;

        InitializeSpeedVariation();

        // Initialize states
        _states[ZombieStateType.Idle] = new ZombieIdleState(this);
        _states[ZombieStateType.Chasing] = new ZombieChaseState(this);
        _states[ZombieStateType.Attacking] = new ZombieAttackState(this);
        _states[ZombieStateType.Ragdoll] = new ZombieRagdollState(this);
        _states[ZombieStateType.Dead] = new ZombieDeadState(this);
    }

    private void InitializeSpeedVariation()
    {
        _currentSpeedVariation = Random.Range(_minSpeedVariation, _maxSpeedVariation);
        _currentRotationSpeedVariation = Random.Range(_minRotationSpeedVariation, _maxRotationSpeedVariation);
        UpdateEffectiveSpeed();

        if (_animator != null)
        {
            _animator.speed = _speedMultiplier;
        }
    }

    private void UpdateEffectiveSpeed()
    {
        _effectiveMovementSpeed = _baseMovementSpeed * _currentSpeedVariation * _speedMultiplier;
        _effectiveRotationSpeed = _baseRotationSpeed * _currentRotationSpeedVariation * _speedMultiplier;

        if (_agent != null && _agent.isActiveAndEnabled)
        {
            _agent.speed = _effectiveMovementSpeed;
            _agent.angularSpeed = _effectiveRotationSpeed;
        }
    }

    private void OnEnable()
    {
        _hitBox.OnPlayerHit += DoDamage;
        _animationEventProxy.OnAttackAnimationCompleted += HandleAttackingAnimationCompleted;
        _animationEventProxy.OnDieAnimationCompleted += HandleDieAnimationCompleted;
        _health.OnHealthReachedZero += HandleOnHealthReachedZero;
        _dissolveEffect.OnDissolveCompleted += HandleOnDissolveCompleted;
    }
    
    private void OnDisable()
    {
        _hitBox.OnPlayerHit -= DoDamage;
        _animationEventProxy.OnAttackAnimationCompleted -= HandleAttackingAnimationCompleted;
        _animationEventProxy.OnDieAnimationCompleted -= HandleDieAnimationCompleted;
        _health.OnHealthReachedZero -= HandleOnHealthReachedZero;
        _dissolveEffect.OnDissolveCompleted -= HandleOnDissolveCompleted;
    }

    protected override void Update()
    {
        if (GameplayManager.Instance.IsGamePaused) return;
        if (_debugDummy) return;

        // Check for death if not already dead or in ragdoll
        if (_health.IsDead && 
            CurrentStateType != ZombieStateType.Dead && 
            CurrentStateType != ZombieStateType.Ragdoll)
        {
            ChangeState(ZombieStateType.Dead);
        }

        base.Update();
    }

    public void SetManager(ZombieManager zombieManager)
    {
        _zombieManager = zombieManager;
    }

    public void SetTarget(Transform target)
    {
        _target = target;

        if (_target != null)
        {
            _targetHealth = _target.GetComponent<Health>();
        }
        else
        {
            _targetHealth = null;
        }
    }

    public bool IsTargetDead()
    {
        return _target == null || (_targetHealth != null && _targetHealth.IsDead);
    }

    public void EnableRagDoll(float duration = 3f)
    {
        _ragdollRecoveryTime = duration;
        ChangeState(ZombieStateType.Ragdoll);
    }

    private void HandleAttackingAnimationCompleted()
    {
        if (_health.IsDead) return;

        if (IsTargetDead())
        {
            ChangeState(ZombieStateType.Idle);
            return;
        }
        
        float distance = Vector3.Distance(transform.position, _target.position);
        if (distance <= _attackRange)
        {
            // Attack again
            ChangeState(ZombieStateType.Attacking);
        }
        else
        {
            ChangeState(ZombieStateType.Chasing);
        }
    }

    private void HandleDieAnimationCompleted()
    {
        _dissolveEffect.StartDissolveEffect();
    }

    private void HandleOnHealthReachedZero()
    {
        // If in ragdoll, let the ground contact handle death animation
        if (CurrentStateType != ZombieStateType.Ragdoll)
        {
            ChangeState(ZombieStateType.Dead);
        }
        
        _zombieManager.HandleZombieDeath(this);
        
        _zombieSoundController.PlayDeathSound();
    }

    private void HandleOnDissolveCompleted()
    {
        _zombieManager.HandleZombieDeathSequenceComplete(this);
    }

    private void DoDamage(Health health)
    {
        health.TryChangeHealth(_damageToHealth);
    }

    public void OnGetFromPool()
    {
        gameObject.SetActive(true);
        
        // Reset dissolve material
        _dissolveEffect.ResetDissolveEffect();
        
        // Reset health
        _health.ResetHealth();
        
        // Reset agent
        _agent.enabled = true;
        _agent.isStopped = false;
        _agent.ResetPath();
        
        // Reset rigidbody
        _rigidbody.isKinematic = true;
        
        // Reset animator
        _animator.enabled = true;
        _animator.Rebind();
        _animator.Update(0f);
        
        // Reset collider
        _collider.enabled = true;
        
        // Reset state machine
        ChangeState(ZombieStateType.Idle);
        
        // Reset attack timer
        _lastAttackTime = 0f;
        
        // Reset zombie sound controller
        _zombieSoundController.Reset();
        
        // Reset speed variation
        InitializeSpeedVariation();
    }

    public void OnReleaseToPool()
    {
        gameObject.SetActive(false);
        
        // Clear target
        _target = null;
        _targetHealth = null;
        
        // Stop zombie sound controller
        _zombieSoundController.StopMoaning();
    
        // Stop any coroutines
        StopAllCoroutines();
        
    }
}
