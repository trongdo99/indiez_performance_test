using System;
using System.Collections.Generic;
using UnityEngine;

public class TargetFinder : MonoBehaviour
{
    public event Action<Transform> OnTargetFound;
    public event Action OnTargetLost;
    
    [SerializeField] private float _coneAngle = 45f;
    [SerializeField] private float _detectionRadius = 10f;
    [SerializeField] private LayerMask _targetableLayerMask;
    
    private Transform _currentTarget;
    private Dictionary<Transform, ZombieController> _zombieCache = new Dictionary<Transform, ZombieController>();
    
    public bool HasTarget => _currentTarget != null;
    public Transform CurrentTarget => _currentTarget;
    public float DetectionRadius => _detectionRadius;
    public float ConeAngle => _coneAngle;

    private void OnEnable()
    {
        ZombieManager.Instance.OnZombieDeath += HandleZombieDeath;
    }

    private void OnDisable()
    {
        ZombieManager.Instance.OnZombieDeath -= HandleZombieDeath;
    }

    public Transform FindTargetInDirection(Vector3 aimDirection)
    {
        if (aimDirection.sqrMagnitude < 0.1f)
        {
            // Keep existing target if in range and not dead/dying
            if (_currentTarget != null && IsTargetValid(_currentTarget))
            {
                return _currentTarget;
            }
            
            // No direction input and no valid current target
            if (_currentTarget != null)
            {
                _currentTarget = null;
                OnTargetLost?.Invoke();
            }
            return null;
        }

        Transform bestTarget = FindBestTargetInCone(aimDirection);
        
        // Check if target changed
        if (bestTarget != _currentTarget)
        {
            _currentTarget = bestTarget;
            
            if (bestTarget != null)
            {
                ZombieController zombie = GetCachedZombieController(bestTarget);
                OnTargetFound?.Invoke(zombie.AimAtPosition);
            }
            else
            {
                OnTargetLost?.Invoke();
            }
        }
        
        return bestTarget;
    }

    private Transform FindBestTargetInCone(Vector3 aimDirection)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRadius, _targetableLayerMask);
        
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;
        float minDot = Mathf.Cos(_coneAngle * 0.5f * Mathf.Deg2Rad);

        foreach (Collider hit in hits)
        {
            Transform targetTransform = hit.transform;
            
            // Skip invalid targets (dead, dying, etc.)
            if (!IsTargetValid(targetTransform))
                continue;
                
            Vector3 toTarget = targetTransform.position - transform.position;
            Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
            float distance = toTarget.magnitude;
            float dot = Vector3.Dot(aimDirection, flatToTarget);
            
            if (dot < minDot) continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = targetTransform;
            }
        }

        return bestTarget;
    }

    private bool IsTargetInRange(Transform targetTransform)
    {
        if (targetTransform == null) return false;
        
        Vector3 toTarget = targetTransform.position - transform.position;
        float distance = toTarget.magnitude;
        return distance < _detectionRadius;
    }
    
    private bool IsTargetValid(Transform targetTransform)
    {
        if (targetTransform == null) return false;
        
        // Check if target is in range
        if (!IsTargetInRange(targetTransform)) return false;
        
        // Check if the target is a zombie using cache
        ZombieController zombie = GetCachedZombieController(targetTransform);
        if (zombie != null)
        {
            // Exclude zombies in Dead
            if (zombie.CurrentStateType == ZombieStateType.Dead)
            {
                return false;
            }
            
            // Also check health directly
            if (zombie.Health != null && zombie.Health.IsDead)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private ZombieController GetCachedZombieController(Transform targetTransform)
    {
        // Check if we have this zombie in cache already
        if (_zombieCache.TryGetValue(targetTransform, out ZombieController cachedZombie))
        {
            return cachedZombie;
        }
        
        // Not in cache, try to get the component
        ZombieController zombieController = targetTransform.GetComponent<ZombieController>();
        
        // Cache the result (even if null)
        _zombieCache[targetTransform] = zombieController;
        
        return zombieController;
    }
    
    private void HandleZombieDeath(ZombieController zombie)
    {
        // If this was the current target, clear it
        if (_currentTarget != null && zombie.transform == _currentTarget)
        {
            _currentTarget = null;
            OnTargetLost?.Invoke();
        }
    }
}