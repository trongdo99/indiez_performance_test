using UnityEngine;

public class PlayingState : GameplayState
{
    private float _stateEntryTime;

    public PlayingState(GameplayManager gameplayManager) : base(gameplayManager) { }

    public override void Enter()
    {
        if (_gameplayManager.PreviousStateType == GameplayStateType.Paused)
        {
            _gameplayManager.TotalPausedTime += Time.time - _gameplayManager.PauseStartTime;
        }
        _stateEntryTime = Time.time;
    }
}