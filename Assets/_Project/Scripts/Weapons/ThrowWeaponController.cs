using System;
using UnityEngine;

[RequireComponent(typeof(TargetFinder))]
public class ThrowWeaponController : MonoBehaviour
{
    [SerializeField] private GameObject _grenade;
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private float _throwCoolDown = 1f;
    [SerializeField] private float _maxThrowHeight = 5f;
    [SerializeField] private float _initialThrowSpeed = 15f;
    
    private float _lastThrowTime;
    private TargetFinder _targetFinder;

    private void Awake()
    {
        _targetFinder = GetComponent<TargetFinder>();
    }

    public void ThrowGrenade()
    {
        if (!CanThrow()) return;
        
        Vector3 targetPosition = DetermineThrowTargetPosition();
        Vector3 initialVelocity = CalculateInitialVelocity(_throwPoint.position, targetPosition, _maxThrowHeight);

        GameObject grenadeObj = Instantiate(_grenade, _throwPoint.position, Quaternion.identity);
        if (grenadeObj.TryGetComponent(out Grenade grenade))
        {
            grenade.Initialize(initialVelocity);
            _lastThrowTime = Time.time;
        }
        else
        {
            Debug.LogError("Grenade object doesn't have Grenade component");
            Destroy(grenadeObj);
        }
    }

    private bool CanThrow()
    {
        // Check cooldown
        if (Time.time < _lastThrowTime + _throwCoolDown)
        {
            return false;
        }

        // Check game state
        if (GameplayManager.Instance.IsGamePaused)
        {
            return false;
        }

        return true;
    }

    private Vector3 DetermineThrowTargetPosition()
    {
        // Use target from TargetFinder if available
        if (_targetFinder.HasTarget)
        {
            return _targetFinder.CurrentTarget.position;
        }
        
        // Otherwise, throw in the forward direction at the detection range
        float detectionRange = _targetFinder.DetectionRadius;
        return transform.position + (transform.forward * detectionRange);
    }

    private Vector3 CalculateInitialVelocity(Vector3 start, Vector3 end, float height)
    {
        Vector3 direction = end - start;
        float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;
        float verticalDisplacement = end.y - start.y;
        
        // Calculate time based on horizontal distance and desired speed
        float time = horizontalDistance / _initialThrowSpeed;
        
        // Calculate gravity for the desired arc
        float gravity = 2 * (height - verticalDisplacement / 2) / Mathf.Pow(time / 2, 2);
        
        // Calculate initial vertical velocity
        float initialYVelocity = verticalDisplacement / time + gravity * time / 2;
        
        // Calculate horizontal velocity
        Vector3 horizontalVelocity = new Vector3(direction.x, 0f, direction.z).normalized * _initialThrowSpeed;
        
        return new Vector3(horizontalVelocity.x, initialYVelocity, horizontalVelocity.z);
    }
}