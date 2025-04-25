using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerCharacterController player) : base(player) { }

    public override void Enter()
    {
        Player.Animator.SetFloat(AnimatorParameters.VelocityX, 0f);
        Player.Animator.SetFloat(AnimatorParameters.VelocityZ, 0f);
    }

    public override void Update()
    {
        Player.ApplyGravity();
        
        Player.CurrentVelocityRef = Vector3.Lerp(Player.CurrentVelocityRef, Vector3.zero,
            1f - Mathf.Exp(-Player.MovementSharpness * Time.deltaTime));
        
        Player.CharacterController.Move(Player.CurrentVelocityRef * Time.deltaTime);
    }

    public override void LateUpdate()
    {
        Player.RotateTowardTargetDirection();
    }
}