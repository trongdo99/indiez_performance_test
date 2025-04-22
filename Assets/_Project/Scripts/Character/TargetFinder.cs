using System;
using UnityEngine;

public class TargetFinder : MonoBehaviour
{
    [SerializeField] private InputReader _input;
    [SerializeField] private float _coneAngle = 45f;
    [SerializeField] private float _detectionRadius = 10f;
    [SerializeField] private LayerMask _targetableLayerMask;

    private WeaponController _weaponController;
    private Transform _targetTransform;

    private void Awake()
    {
        _weaponController = GetComponent<WeaponController>();
    }

    private void Update()
    {
        Vector2 lookInput = _input.LookInput;
        Vector3 aimDirection = new Vector3(lookInput.x, 0, lookInput.y).normalized;

        if (aimDirection.sqrMagnitude > 0.1f)
        {
            _targetTransform = FindTargetInCone(aimDirection);

            if (_targetTransform != null)
            {
                _weaponController.Aiming(_targetTransform);
            }
            else
            {
                _weaponController.StopAiming();
            }
        }
        else if (_targetTransform != null && IsTargetInRange(_targetTransform))
        {
            _weaponController.Aiming(_targetTransform);
        }
        else
        {
            _targetTransform = null;
            _weaponController.StopAiming();
        }
    }

    private void LateUpdate()
    {
        if (_input.LookInput.sqrMagnitude > 0.01f)
        {
            Vector3 aimDirection = new Vector3(_input.LookInput.x, 0, _input.LookInput.y).normalized;
            DrawCone(aimDirection);
        }

        if (_targetTransform != null)
        {
            Debug.DrawLine(transform.position, _targetTransform.position, Color.green);
        }
    }

    private Transform FindTargetInCone(Vector3 aimDirection)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRadius, _targetableLayerMask);
        
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            Vector3 toTarget = hit.transform.position - transform.position;
            
            float distance = toTarget.magnitude;
            if (distance > _detectionRadius) continue;

            float angle = Vector3.Angle(aimDirection, toTarget.normalized);
            if (angle > _coneAngle / 2) continue;

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
        Vector3 toTarget = targetTransform.position - transform.position;
        float distance = toTarget.magnitude;
        return distance < _detectionRadius;
    }
    
    private void DrawCone(Vector3 aimDirection)
    {
        var rayCount = 20;

        Vector3 origin = transform.position;

        for (int i = 0; i <= rayCount; i++)
        {
            float t = i / (float)rayCount;
            float angle = Mathf.Lerp(-_coneAngle / 2, _coneAngle / 2, t);
            Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 dir = rot * aimDirection;

            Debug.DrawLine(origin, origin + dir * _detectionRadius, Color.red);
        }
    }
}
