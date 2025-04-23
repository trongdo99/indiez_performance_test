using System;
using UnityEngine;
using UnityEngine.Serialization;

public class CharacterAnimatorController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [FormerlySerializedAs("_characterMovementController")] [SerializeField] private PlayerCharacterController _playerCharacterController;

    private void Awake()
    {
        _animator.fireEvents = false;
    }

    private void LateUpdate()
    {
        Vector3 currentVelocity = _playerCharacterController.CurrentVelocity.normalized;
        float velocityZ = Vector3.Dot(currentVelocity, transform.forward);
        float velocityX = Vector3.Dot(currentVelocity, transform.right);
        _animator.SetFloat(AnimatorParameters.VelocityZ, velocityZ);
        _animator.SetFloat(AnimatorParameters.VelocityX, velocityX);
    }
}
