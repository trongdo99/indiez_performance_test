using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class ZombieController : MonoBehaviour
{
    public enum ZombieState
    {
        Idle,
        Chasing,
        Attacking,
        Dead,
    }
    
    [SerializeField] private Transform _target;
    [SerializeField] private ZombieHitBox _hitBox;
    [SerializeField] private ZombieAnimationEventProxy _animationEventProxy;
    [SerializeField] private Collider _targetCollider;
    
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _rotationSpeedWhileAttacking = 5f;
    [SerializeField] private float _attackCoolDown = 2f;
    [SerializeField] private float _damageToHealth = -20f;
    
    [SerializeField] private bool _debugDummy;

    private ZombieState _currentState = ZombieState.Idle;
    private ZombieDissolveEffect _dissolveEffect;
    private Health _health;
    private Animator _animator;
    private NavMeshAgent _agent;
    private Collider _collider;
    private float _lastAttackTime;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _animator = GetComponentInChildren<Animator>();
        _collider = GetComponent<Collider>();
        _dissolveEffect = GetComponent<ZombieDissolveEffect>();
        
        _hitBox.OnPlayerHit += DoDamage;
        _animationEventProxy.OnAttackAnimationCompleted += HandleAttackingAnimationCompleted;
        _animationEventProxy.OnDieAnimationCompleted += HandleDieAnimationCompleted;
        _health.OnDeath += HandleOnDeath;
        _dissolveEffect.OnDissolveCompleted += HandleOnDissolveCompleted;
    }

    private void OnDestroy()
    {
        _hitBox.OnPlayerHit -= DoDamage;
        _animationEventProxy.OnAttackAnimationCompleted -= HandleAttackingAnimationCompleted;
        _animationEventProxy.OnDieAnimationCompleted -= HandleDieAnimationCompleted;
        _health.OnDeath -= HandleOnDeath;
        _dissolveEffect.OnDissolveCompleted -= HandleOnDissolveCompleted;
    }

    private void Update()
    {
        if (_debugDummy) return;
        
        if (_health.IsDead)
        {
            SetState(ZombieState.Dead);
        }

        switch (_currentState)
        {
            case ZombieState.Idle:
                HandleIdle();
                break;
            case ZombieState.Chasing:
                HandleChasing();
                break;
            case ZombieState.Attacking:
                RotateTowardsTarget();
                break;
        }
    }

    private void SetState(ZombieState newState, bool force = false)
    {
        if (!force && _currentState == newState ) return;
        Debug.Log($"Zombie state changed from {_currentState} to {newState}");
        _currentState = newState;

        switch (_currentState)
        {
            case ZombieState.Idle:
                _agent.isStopped = true;
                _animator.SetFloat(AnimatorParameters.ZombieVelocity, 0f);
                break;
            case ZombieState.Chasing:
                _agent.isStopped = false;
                break;
            case ZombieState.Attacking:
                _agent.isStopped = true;
                _animator.SetTrigger(AnimatorParameters.ZombieAttack);
                _lastAttackTime = Time.time;
                break;
            case ZombieState.Dead:
                _collider.enabled = false;
                _targetCollider.enabled = false;
                _agent.isStopped = true;
                _animator.SetTrigger(AnimatorParameters.ZombieDie);
                break;
        }
    }

    private void HandleIdle()
    {
        if (_target != null && !_health.IsDead)
        {
            SetState(ZombieState.Chasing);
        }
    }

    private void HandleChasing()
    {
        if (_target == null)
        {
            SetState(ZombieState.Idle);
            return;
        }
        
        float distance = Vector3.Distance(transform.position, _target.position);
        if (distance <= _attackRange && Time.time - _lastAttackTime >= _attackCoolDown)
        {
            SetState(ZombieState.Attacking);
            return;
        }
        
        _agent.SetDestination(_target.position);
        float normalizedMoveSpeed = Mathf.Clamp01(_agent.velocity.magnitude / _agent.speed);
        _animator.SetFloat(AnimatorParameters.ZombieVelocity, normalizedMoveSpeed);
    }

    private void RotateTowardsTarget()
    {
        Vector3 direction = (_target.position - transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;
        
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * _rotationSpeedWhileAttacking);
    }

    private void HandleAttackingAnimationCompleted()
    {
        Debug.Log("Attack complete");
        if (_health.IsDead) return;
        
        float distance = Vector3.Distance(transform.position, _target.position);
        if (distance <= _attackRange)
        {
            // Attack again
            SetState(ZombieState.Attacking, true);
        }
        else
        {
            SetState(ZombieState.Chasing);
        }
    }

    private void HandleDieAnimationCompleted()
    {
        _dissolveEffect.StartDissolveEffect();
    }

    private void HandleOnDeath()
    {
        SetState(ZombieState.Dead);
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
