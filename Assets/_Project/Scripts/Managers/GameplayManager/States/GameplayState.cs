using UnityEngine;

public abstract class GameplayState : IState
{
    protected GameplayManager _gameplayManager;

    public GameplayState(GameplayManager gameplayManager)
    {
        _gameplayManager = gameplayManager;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void LateUpdate() { }
    public virtual void HandleCollision(Collision collision) { }
}