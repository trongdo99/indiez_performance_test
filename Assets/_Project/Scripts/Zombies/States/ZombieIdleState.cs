public class ZombieIdleState : ZombieState
{
    public ZombieIdleState(ZombieController zombieController) : base(zombieController) { }

    public override void Enter()
    {
        if (_zombieController.Agent.enabled)
        {
            _zombieController.Agent.isStopped = true;
        }
        _zombieController.Animator.SetFloat(AnimatorParameters.ZombieVelocity, 0f);
    }

    public override void Update()
    {
        if (_zombieController.Target != null && !_zombieController.Health.IsDead)
        {
            _zombieController.ChangeState(ZombieStateType.Chasing);
        }
    }
}