using UnityEngine;

public class PlayerMovingState : PlayerState
{
    public PlayerMovingState(PlayerCharacterController player) : base(player) { }

    public override void Update()
    {
        Vector3 targetVelocity = Player.CameraRelativeInput.normalized * Player.MaxMoveSpeed;
        
        if (Player.BoundaryConstraint != null)
        {
            targetVelocity = Player.BoundaryConstraint.ConstrainVelocityToBounds(targetVelocity);
        }
        
        Player.CurrentVelocityRef = Vector3.Lerp(Player.CurrentVelocityRef, targetVelocity,
            1f - Mathf.Exp(-Player.MovementSharpness * Time.deltaTime));
        
        Player.ApplyGravity();
        
        Player.CharacterController.Move(Player.CurrentVelocityRef * Time.deltaTime);
        
        Player.Animator.SetFloat(AnimatorParameters.VelocityX, Player.CurrentVelocityRef.x);
        Player.Animator.SetFloat(AnimatorParameters.VelocityZ, Player.CurrentVelocityRef.z);
    }

    public override void LateUpdate()
    {
        Player.RotateTowardTargetDirection();
    }
}