using System;
using UnityEngine;

public class CharacterAnimatorController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private CharacterMovementController _characterMovementController;

    private void Awake()
    {
        _animator.fireEvents = false;
    }

    private void LateUpdate()
    {
        Vector3 currentVelocity = _characterMovementController.CurrentVelocity.normalized;
        float velocityZ = Vector3.Dot(currentVelocity, transform.forward);
        float velocityX = Vector3.Dot(currentVelocity, transform.right);
        _animator.SetFloat(AnimatorParameters.VelocityZ, velocityZ);
        _animator.SetFloat(AnimatorParameters.VelocityX, velocityX);
    }
}
