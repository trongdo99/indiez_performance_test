using UnityEngine;

public class ZombieAttackState : ZombieState
{
    public ZombieAttackState(ZombieController zombieController) : base(zombieController) { }

    public override void Enter()
    {
        if (_zombieController.Agent.enabled)
        {
            _zombieController.Agent.isStopped = true;
        }
        _zombieController.Animator.SetTrigger(AnimatorParameters.ZombieAttack);
        _zombieController.LastAttackTime = Time.time;
    }

    public override void Update()
    {
        if (_zombieController.IsTargetDead())
        {
            _zombieController.ChangeState(ZombieStateType.Idle);
            return;
        }
        
        // Rotate towards target
        Vector3 direction = (_zombieController.Target.position - _zombieController.transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;
        
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        _zombieController.transform.rotation = Quaternion.Slerp(
            _zombieController.transform.rotation, 
            lookRotation, 
            Time.deltaTime * _zombieController.RotationSpeedWhileAttacking
        );
    }
}