using System;
using UnityEngine;

public class TargetFinder : MonoBehaviour
{
    public event Action<Transform> OnTargetFound;
    public event Action OnTargetLost;
    
    [SerializeField] private float _coneAngle = 45f;
    [SerializeField] private float _detectionRadius = 10f;
    [SerializeField] private LayerMask _targetableLayerMask;
    
    private Transform _currentTarget;
    
    public bool HasTarget => _currentTarget != null;
    public Transform CurrentTarget => _currentTarget;
    public float DetectionRadius => _detectionRadius;
    public float ConeAngle => _coneAngle;

    public Transform FindTargetInDirection(Vector3 aimDirection)
    {
        if (aimDirection.sqrMagnitude < 0.1f)
        {
            if (_currentTarget != null && IsTargetInRange(_currentTarget))
            {
                return _currentTarget;
            }
            
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
                OnTargetFound?.Invoke(bestTarget);
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
            Vector3 toTarget = hit.transform.position - transform.position;
            Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
            float distance = toTarget.magnitude;
            float dot = Vector3.Dot(aimDirection, flatToTarget);
            
            if (dot < minDot) continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = hit.transform;
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
}