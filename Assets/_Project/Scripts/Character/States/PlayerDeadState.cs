public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(PlayerCharacterController player) : base(player) { }

    public override void Enter()
    {
        Player.Animator.SetTrigger(AnimatorParameters.Die);
    }
}