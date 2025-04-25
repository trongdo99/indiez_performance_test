using UnityEngine;

public abstract class ZombieState : IState
{
    protected ZombieController _zombieController;

    public ZombieState(ZombieController zombieController)
    {
        _zombieController = zombieController;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void LateUpdate() { }
    public virtual void Exit() { }
    public virtual void HandleCollision(Collision collision) { }
}