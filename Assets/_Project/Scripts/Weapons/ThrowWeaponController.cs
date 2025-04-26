using System;
using UnityEngine;

[RequireComponent(typeof(TargetFinder))]
public class ThrowWeaponController : MonoBehaviour
{
    [SerializeField] private GameObject _grenade;
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private float _throwCoolDown = 1f;
    [SerializeField] private float _arcHeight = 3f;
    [SerializeField] private float _minimumArcHeight = 1.5f;
    
    private float _lastThrowTime;
    private TargetFinder _targetFinder;
    private PlayerAnimationEventProxy _playerAnimationEventProxy;
    private WeaponController _weaponController;
    private bool _isThrowingAnimation;
    
    public bool IsThrowingAnimation => _isThrowingAnimation;

    private void Awake()
    {
        _targetFinder = GetComponent<TargetFinder>();
        _playerAnimationEventProxy = GetComponentInChildren<PlayerAnimationEventProxy>();
        _weaponController = GetComponent<WeaponController>();
    }

    private void Start()
    {
        _playerAnimationEventProxy.AnimationThrowEvent += HandleAnimationThrow;
        _playerAnimationEventProxy.AnimationThrowEndedEvent += HandleAnimationThrowEnded;
    }

    private void OnDestroy()
    {
        _playerAnimationEventProxy.AnimationThrowEvent -= HandleAnimationThrow;
        _playerAnimationEventProxy.AnimationThrowEndedEvent -= HandleAnimationThrowEnded;
    }

    public void StartThrowSequence()
    {
        _isThrowingAnimation = true;
        
        _weaponController.SetWeaponVisibility(false);
        _weaponController.StopAiming();
    }

    public void ThrowGrenade()
    {
        if (!CanThrow()) return;
        
        Vector3 targetPosition = DetermineThrowTargetPosition();
        Vector3 initialVelocity = CalculateThrowVelocity(_throwPoint.position, targetPosition);

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

    private Vector3 CalculateThrowVelocity(Vector3 startPos, Vector3 targetPos)
    {
        // Calculate direction to target
        Vector3 toTarget = targetPos - startPos;
        
        // Calculate horizontal distance
        Vector3 horizontalDir = new Vector3(toTarget.x, 0, toTarget.z);
        float horizontalDistance = horizontalDir.magnitude;
        
        // Clamp the horizontal distance to detection radius
        if (horizontalDistance > _targetFinder.DetectionRadius)
        {
            horizontalDistance = _targetFinder.DetectionRadius;
            horizontalDir = horizontalDir.normalized * horizontalDistance;
            targetPos = startPos + horizontalDir + new Vector3(0, toTarget.y, 0);
        }
        
        // Calculate the height of the arc based on distance
        float height = Mathf.Lerp(_minimumArcHeight, _arcHeight, 
            Mathf.Min(1f, horizontalDistance / (_targetFinder.DetectionRadius * 0.5f)));
        
        // Use physics to calculate the launch velocity
        Vector3 velocity = CalculateProjectileVelocity(startPos, targetPos, height, Physics.gravity.magnitude);
        
        return velocity;
    }
    
    private Vector3 CalculateProjectileVelocity(Vector3 startPoint, Vector3 targetPoint, float arcHeight, float gravity)
    {
        // Calculate displacement vectors
        Vector3 displacement = targetPoint - startPoint;
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0, displacement.z);
        float horizontalDistance = horizontalDisplacement.magnitude;
        float verticalDisplacement = displacement.y;
        
        // If the target is very close, use a simple upwards velocity
        if (horizontalDistance < 0.1f)
        {
            return Vector3.up * Mathf.Sqrt(2 * gravity * arcHeight);
        }
        
        // Calculate time of flight for projectile physics
        // For a fixed height and distance, we need to determine velocity
        float horizontalVelocity = Mathf.Sqrt(gravity * horizontalDistance * horizontalDistance / 
                                              (2 * (arcHeight - verticalDisplacement)));
        
        // Calculate time
        float time = horizontalDistance / horizontalVelocity;
        
        // Calculate vertical velocity
        float verticalVelocity = 0.5f * gravity * time - verticalDisplacement / time;
        
        // Combine velocities
        Vector3 result = horizontalDisplacement.normalized * horizontalVelocity;
        result.y = verticalVelocity;
        
        return result;
    }

    private void HandleAnimationThrow()
    {
        ThrowGrenade();
    }

    private void HandleAnimationThrowEnded()
    {
        _isThrowingAnimation = false;
        
        _weaponController.SetWeaponVisibility(true);
    }
}