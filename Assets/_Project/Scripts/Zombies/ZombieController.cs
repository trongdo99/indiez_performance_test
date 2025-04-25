using System;
using System.Collections;
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
        Ragdoll,
        Dead,
    }
    
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

    private ZombieState _currentState = ZombieState.Idle;
    private ZombieDissolveEffect _dissolveEffect;
    private Health _health;
    private Animator _animator;
    private NavMeshAgent _agent;
    private Collider _collider;
    private Rigidbody _rigidbody;
    private float _lastAttackTime;
    private Transform _target;
    private Health _targetHealth;
    private bool _hasHitGround;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _animator = GetComponentInChildren<Animator>();
        _collider = GetComponent<Collider>();
        _dissolveEffect = GetComponent<ZombieDissolveEffect>();
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.isKinematic = true;
        
        _hitBox.OnPlayerHit += DoDamage;
        _animationEventProxy.OnAttackAnimationCompleted += HandleAttackingAnimationCompleted;
        _animationEventProxy.OnDieAnimationCompleted += HandleDieAnimationCompleted;
        _health.OnHealthReachedZero += HandleOnHealthReachedZero;
        _dissolveEffect.OnDissolveCompleted += HandleOnDissolveCompleted;
    }

    private void OnDestroy()
    {
        _hitBox.OnPlayerHit -= DoDamage;
        _animationEventProxy.OnAttackAnimationCompleted -= HandleAttackingAnimationCompleted;
        _animationEventProxy.OnDieAnimationCompleted -= HandleDieAnimationCompleted;
        _health.OnHealthReachedZero -= HandleOnHealthReachedZero;
        _dissolveEffect.OnDissolveCompleted -= HandleOnDissolveCompleted;
    }

    private void Update()
    {
        if (GameplayManager.Instance.IsGamePaused) return;
        
        if (_debugDummy) return;

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
            case ZombieState.Ragdoll:
                // Ragdoll is handled by physics system
                break;
        }
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

    private void SetState(ZombieState newState, bool force = false)
    {
        if (!force && _currentState == newState) return;
        
        Debug.Log($"Zombie state changed from {_currentState} to {newState}");
        
        _currentState = newState;

        switch (_currentState)
        {
            case ZombieState.Idle:
                if (_agent.enabled)
                {
                    _agent.isStopped = true;
                }
                _animator.SetFloat(AnimatorParameters.ZombieVelocity, 0f);
                break;
                
            case ZombieState.Chasing:
                if (!_agent.enabled)
                {
                    _agent.enabled = true;
                }
                _agent.isStopped = false;
                break;
                
            case ZombieState.Attacking:
                if (_agent.enabled)
                {
                    _agent.isStopped = true;
                }
                _animator.SetTrigger(AnimatorParameters.ZombieAttack);
                _lastAttackTime = Time.time;
                break;
                
            case ZombieState.Ragdoll:
                _agent.enabled = false;
                _animator.enabled = false;
                _collider.enabled = true;
                _rigidbody.isKinematic = false;
                _hasHitGround = false;
                
                StartCoroutine(MonitorGroundContact(_ragdollRecoveryTime));
                break;
                
            case ZombieState.Dead:
                if (_currentState != ZombieState.Ragdoll)
                {
                    _animator.enabled = true;
                    _animator.SetTrigger(AnimatorParameters.ZombieDie);
                    
                    _rigidbody.isKinematic = true;
                    if (_agent.enabled)
                    {
                        _agent.isStopped = true;
                        _agent.enabled = false;
                    }
                    
                    _targetCollider.enabled = false;
                }
                
                // If coming from ragdoll, ground contact will handle the rest
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
        if (IsTargetDead())
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
        
        if (_agent.enabled)
        {
            _agent.SetDestination(_target.position);
            float normalizedMoveSpeed = Mathf.Clamp01(_agent.velocity.magnitude / _agent.speed);
            _animator.SetFloat(AnimatorParameters.ZombieVelocity, normalizedMoveSpeed);
        }
    }

    private void RotateTowardsTarget()
    {
        if (IsTargetDead())
        {
            SetState(ZombieState.Idle);
            return;
        }
        
        Vector3 direction = (_target.position - transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;
        
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * _rotationSpeedWhileAttacking);
    }

    private bool IsTargetDead()
    {
        return _target == null || (_targetHealth != null && _targetHealth.IsDead);
    }

    public void EnableRagDoll(float duration = 3f)
    {
        _ragdollRecoveryTime = duration;
        SetState(ZombieState.Ragdoll);
    }

    private IEnumerator MonitorGroundContact(float maxDuration)
    {
        float startTime = Time.time;

        while (Time.time - startTime < maxDuration && !_hasHitGround)
        {
            yield return null;
        }

        if (_health.IsDead)
        {
            PlayDeathAnimation();
        }
        else
        {
            RecoverFromRagdoll();
        }
    }

    private void PlayDeathAnimation()
    {
        _rigidbody.isKinematic = true;
        _targetCollider.enabled = false;
        
        _animator.enabled = true;
        _animator.SetTrigger(AnimatorParameters.ZombieDie);
        
        _currentState = ZombieState.Dead;
    }

    private void RecoverFromRagdoll()
    {
        if (_currentState != ZombieState.Ragdoll || _health.IsDead) return;
        
        _rigidbody.isKinematic = true;
        _animator.enabled = true;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
        {
            _agent.enabled = true;
            _agent.Warp(hit.position);
            
            if (_target != null && !IsTargetDead())
            {
                SetState(ZombieState.Chasing);
            }
            else
            {
                SetState(ZombieState.Idle);
            }
        }
        else
        {
            SetState(ZombieState.Idle);
        }
    }

    private void HandleAttackingAnimationCompleted()
    {
        if (_health.IsDead) return;

        if (IsTargetDead())
        {
            SetState(ZombieState.Idle);
            return;
        }
        
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

    private void HandleOnHealthReachedZero()
    {
        // If in ragdoll, let the ground contact handle death animation
        if (_currentState != ZombieState.Ragdoll)
        {
            SetState(ZombieState.Dead);
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

    private void OnCollisionEnter(Collision other)
    {
        if (_currentState != ZombieState.Ragdoll) return;

        // Check if collided with ground layer
        if (((1 << other.gameObject.layer) & _groundLayerMask) != 0)
        {
            _hasHitGround = true;
        }
    }
}
