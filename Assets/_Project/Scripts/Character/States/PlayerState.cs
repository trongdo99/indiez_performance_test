using UnityEngine;

public abstract class PlayerState : IState
{
    protected PlayerCharacterController Player;

    public PlayerState(PlayerCharacterController player)
    {
        Player = player;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void LateUpdate() { }
    public virtual void Exit() { }
    public virtual void HandleCollision(Collision collision) { }
}