public class ZombieDeadState : ZombieState
{
    public ZombieDeadState(ZombieController zombieController) : base(zombieController) { }

    public override void Enter()
    {
        _zombieController.TargetCollider.enabled = false;
        
        if (_zombieController.CurrentStateType != ZombieStateType.Ragdoll)
        {
            _zombieController.Collider.enabled = false;
            
            _zombieController.Animator.enabled = true;
            _zombieController.Animator.SetTrigger(AnimatorParameters.ZombieDie);
            
            _zombieController.Rigidbody.isKinematic = true;
            if (_zombieController.Agent.enabled)
            {
                _zombieController.Agent.isStopped = true;
                _zombieController.Agent.enabled = false;
            }
            
            _zombieController.TargetCollider.enabled = false;
        }
    }
}