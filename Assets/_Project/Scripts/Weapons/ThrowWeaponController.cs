using UnityEngine;

public class ThrowWeaponController : MonoBehaviour
{
    [SerializeField] private GameObject _grenade;
    [SerializeField] private Transform _grenadeSpawnPoint;
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private LayerMask _zombieLayer;
    
    [SerializeField] private float _throwCoolDown = 1f;
    [SerializeField] private float _maxThrowRange = 10f;
    [SerializeField] private float _maxThrowHeight = 5f;
    [SerializeField] private float _initialThrowSpeed = 15f;

    private float _lastThrowTime;

    public void ThrowGrenade()
    {
        if (!CanThrow()) return;
        
        Vector3 targetPosition = FindThrowTargetPosition();
        
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
        if (Time.time < _lastThrowTime + _throwCoolDown) return false;

        if (GameplayManager.Instance.IsGamePaused) return false;

        return true;
    }

    private Vector3 FindThrowTargetPosition()
    {
        Vector3 forwardPosition = transform.position + (transform.forward * _maxThrowRange);
        forwardPosition.y = transform.position.y;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _maxThrowRange, _zombieLayer,
            QueryTriggerInteraction.Ignore);

        float closestDistance = _maxThrowRange;
        Transform closestTarget = null;

        foreach (Collider hitCollider in hitColliders)
        {
            Vector3 directionToTarget = hitCollider.transform.position - transform.position;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
            if (angleToTarget > 45f) continue;
            
            float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = hitCollider.transform;
            }
        }

        if (closestTarget != null)
        {
            return closestTarget.position;
        }
        
        return forwardPosition;
    }

    private Vector3 CalculateInitialVelocity(Vector3 start, Vector3 end, float height)
    {
        Vector3 direction = end - start;
        float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;
        float verticalDisplacement = end.y - start.y;
        float time = horizontalDistance / _initialThrowSpeed;
        float gravity = 2 * (height - verticalDisplacement / 2) / Mathf.Pow(time / 2, 2);
        float initialYVelocity = verticalDisplacement / time + gravity * time / 2;
        Vector3 horizontalVelocity = new Vector3(direction.x, 0f, direction.z).normalized * _initialThrowSpeed;
        return new Vector3(horizontalVelocity.x, initialYVelocity, horizontalVelocity.z);
    }
}
