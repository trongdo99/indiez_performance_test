using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerBoundaryConstraint : MonoBehaviour
{
    [SerializeField] private BoxCollider _levelBounds;
    [SerializeField] private float _boundaryPushForce = 10f;
    [SerializeField] private float _boundaryBuffer = 0.5f;

    private CharacterController _characterController;
    private Vector3 _boundsCenter;
    private Vector3 _boundsExtents;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (_levelBounds == null)
        {
            _levelBounds = GameObject.FindGameObjectWithTag("LevelBoundary")?.GetComponent<BoxCollider>();

            if (_levelBounds == null)
            {
                Debug.LogWarning("No level bounds found. Disable player bounds checking");
                enabled = false;
                return;
            }
        }

        _boundsCenter = _levelBounds.transform.position + _levelBounds.center;
        _boundsExtents = Vector3.Scale(_levelBounds.size * 0.5f, _levelBounds.transform.lossyScale);
    }

    public Vector3 ConstrainVelocityToBounds(Vector3 velocity)
    {
        if (_levelBounds == null) return velocity;

        Vector3 constrainedVelocity = velocity;
        
        float distanceToLeftBoundary = (transform.position.x - _characterController.radius) - (_boundsCenter.x - _boundsExtents.x);
        float distanceToRightBoundary = (_boundsCenter.x + _boundsExtents.x) - (transform.position.x + _characterController.radius);
        float distanceToBackBoundary = (transform.position.z - _characterController.radius) - (_boundsCenter.z - _boundsExtents.z);
        float distanceToFrontBoundary = (_boundsCenter.z + _boundsExtents.z) - (transform.position.z + _characterController.radius);

        // Check left/right boundaries
        if (distanceToLeftBoundary < 0)
        {
            // Already outside left boundary, push back in
            constrainedVelocity.x = Mathf.Max(0, constrainedVelocity.x) + _boundaryPushForce * Mathf.Abs(distanceToLeftBoundary);
        }
        else if (distanceToLeftBoundary < _boundaryBuffer && constrainedVelocity.x < 0)
        {
            // Approaching left boundary, reduce velocity based on proximity
            float reductionFactor = distanceToLeftBoundary / _boundaryBuffer;
            constrainedVelocity.x *= reductionFactor;
        }
        
        if (distanceToRightBoundary < 0)
        {
            // Already outside right boundary, push back in
            constrainedVelocity.x = Mathf.Min(0, constrainedVelocity.x) - _boundaryPushForce * Mathf.Abs(distanceToRightBoundary);
        }
        else if (distanceToRightBoundary < _boundaryBuffer && constrainedVelocity.x > 0)
        {
            // Approaching right boundary, reduce velocity based on proximity
            float reductionFactor = distanceToRightBoundary / _boundaryBuffer;
            constrainedVelocity.x *= reductionFactor;
        }
        
        // Check front/back boundaries
        if (distanceToBackBoundary < 0)
        {
            // Already outside back boundary, push back in
            constrainedVelocity.z = Mathf.Max(0, constrainedVelocity.z) + _boundaryPushForce * Mathf.Abs(distanceToBackBoundary);
        }
        else if (distanceToBackBoundary < _boundaryBuffer && constrainedVelocity.z < 0)
        {
            // Approaching back boundary, reduce velocity based on proximity
            float reductionFactor = distanceToBackBoundary / _boundaryBuffer;
            constrainedVelocity.z *= reductionFactor;
        }
        
        if (distanceToFrontBoundary < 0)
        {
            // Already outside front boundary, push back in
            constrainedVelocity.z = Mathf.Min(0, constrainedVelocity.z) - _boundaryPushForce * Mathf.Abs(distanceToFrontBoundary);
        }
        else if (distanceToFrontBoundary < _boundaryBuffer && constrainedVelocity.z > 0)
        {
            // Approaching front boundary, reduce velocity based on proximity
            float reductionFactor = distanceToFrontBoundary / _boundaryBuffer;
            constrainedVelocity.z *= reductionFactor;
        }
        
        return constrainedVelocity;
    }
}
