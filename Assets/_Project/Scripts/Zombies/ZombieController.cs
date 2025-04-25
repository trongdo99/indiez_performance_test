using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum ZombieStateType
{
    Idle,
    Chasing,
    Attacking,
    Ragdoll,
    Dead
}

[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class ZombieController : StateMachine<ZombieStateType>
{
    [SerializeField] private ZombieHitBox _hitBox;
    [SerializeField] private ZombieAnimationEventProxy _animationEventProxy;
    [SerializeField] private Collider _targetCollider;
    [SerializeField] private LayerMask _groundLayerMask;
    
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _rotationSpeedWhileAttacking = 5f;
    [SerializeField] private float _attackCoolDown = 2f;
    [SerializeField] private float _damageToHealth = -20f;
    [SerializeField] private float _ragdollRecoveryTime = 3f;
    
    [SerializeField] private bool _debugDummy;

    private ZombieDissolveEffect _dissolveEffect;
    private Health _health;
    private Animator _animator;
    private NavMeshAgent _agent;
    private Collider _collider;
    private Rigidbody _rigidbody;
    private float _lastAttackTime;
    private Transform _target;
    private Health _targetHealth;

    // Properties for states to access
    public ZombieHitBox HitBox => _hitBox;
    public ZombieAnimationEventProxy AnimationEventProxy => _animationEventProxy;
    public Collider TargetCollider => _targetCollider;
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
    public float LastAttackTime { get => _lastAttackTime; set => _lastAttackTime = value; }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _animator = GetComponentInChildren<Animator>();
        _collider = GetComponent<Collider>();
        _dissolveEffect = GetComponent<ZombieDissolveEffect>();
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.isKinematic = true;

        // Initialize states
        _states[ZombieStateType.Idle] = new ZombieIdleState(this);
        _states[ZombieStateType.Chasing] = new ZombieChaseState(this);
        _states[ZombieStateType.Attacking] = new ZombieAttackState(this);
        _states[ZombieStateType.Ragdoll] = new ZombieRagdollState(this);
        _states[ZombieStateType.Dead] = new ZombieDeadState(this);
    }

    private void Start()
    {
        _hitBox.OnPlayerHit += DoDamage;
        _animationEventProxy.OnAttackAnimationCompleted += HandleAttackingAnimationCompleted;
        _animationEventProxy.OnDieAnimationCompleted += HandleDieAnimationCompleted;
        _health.OnHealthReachedZero += HandleOnHealthReachedZero;
        _dissolveEffect.OnDissolveCompleted += HandleOnDissolveCompleted;

        // Start in idle state
        ChangeState(ZombieStateType.Idle);
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

    private void OnDestroy()
    {
        _hitBox.OnPlayerHit -= DoDamage;
        _animationEventProxy.OnAttackAnimationCompleted -= HandleAttackingAnimationCompleted;
        _animationEventProxy.OnDieAnimationCompleted -= HandleDieAnimationCompleted;
        _health.OnHealthReachedZero -= HandleOnHealthReachedZero;
        _dissolveEffect.OnDissolveCompleted -= HandleOnDissolveCompleted;
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

    public void StartDissolveEffect()
    {
        _dissolveEffect.StartDissolveEffect();
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
    }

    private void HandleOnDissolveCompleted()
    {
        Destroy(gameObject);
    }

    private void DoDamage(Health health)
    {
        health.TryChangeHealth(_damageToHealth);
    }
}
