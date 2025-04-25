using UnityEngine;

public class ZombieChaseState : ZombieState
{
    public ZombieChaseState(ZombieController zombieController) : base(zombieController) { }

    public override void Enter()
    {
        if (!_zombieController.Agent.enabled)
        {
            _zombieController.Agent.enabled = true;
        }
        _zombieController.Agent.isStopped = false;
    }

    public override void Update()
    {
        if (_zombieController.IsTargetDead())
        {
            _zombieController.ChangeState(ZombieStateType.Idle);
            return;
        }
        
        float distance = Vector3.Distance(_zombieController.transform.position, _zombieController.Target.position);
        if (distance <= _zombieController.AttackRange && Time.time - _zombieController.LastAttackTime >= _zombieController.AttackCoolDown)
        {
            _zombieController.ChangeState(ZombieStateType.Attacking);
            return;
        }
        
        if (_zombieController.Agent.enabled)
        {
            _zombieController.Agent.SetDestination(_zombieController.Target.position);
            float normalizedMoveSpeed = Mathf.Clamp01(_zombieController.Agent.velocity.magnitude / _zombieController.Agent.speed);
            _zombieController.Animator.SetFloat(AnimatorParameters.ZombieVelocity, normalizedMoveSpeed);
        }
    }
}