using UnityEngine;

public class PausedState : GameplayState
{
    public PausedState(GameplayManager gameplayManager) : base(gameplayManager) { }

    public override void Enter()
    {
        Time.timeScale = 0f;
        _gameplayManager.PauseStartTime = Time.time;
    }

    public override void Exit()
    {
        Time.timeScale = 1f;
    }
}