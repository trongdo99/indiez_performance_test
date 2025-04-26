using UnityEngine;

public class AnimatorParameters
{
    public static readonly int VelocityX = Animator.StringToHash("VelocityX");
    public static readonly int VelocityZ = Animator.StringToHash("VelocityZ");
    public static readonly int AimHorizontal = Animator.StringToHash("AimHorizontal");
    public static readonly int AimVertical = Animator.StringToHash("AimVertical");
    public static readonly int Throw = Animator.StringToHash("Throw");
    public static readonly int Die = Animator.StringToHash("Die");
    
    public static readonly int ZombieVelocity = Animator.StringToHash("Velocity");
    public static readonly int ZombieAttack = Animator.StringToHash("Attack");
    public static readonly int ZombieDie = Animator.StringToHash("Die");
}
