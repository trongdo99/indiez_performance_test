using System;
using UnityEngine;

public class ZombieAnimationEventProxy : MonoBehaviour
{
    public event Action OnAttackAnimationCompleted;
    public event Action OnDieAnimationCompleted;
    
    [SerializeField] private BoxCollider _hitBoxCollider;
    
    public void EnableHitBox()
    {
        _hitBoxCollider.enabled = true;
    }

    public void DisableHitBox()
    {
        _hitBoxCollider.enabled = false;
    }

    public void AttackAnimationCompleted()
    {
        OnAttackAnimationCompleted?.Invoke();
    }

    public void DieAnimationCompleted()
    {
        OnDieAnimationCompleted?.Invoke();
    }
}
