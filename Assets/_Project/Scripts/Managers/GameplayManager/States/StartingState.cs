using System.Collections;
using UnityEngine;

public class StartingState : GameplayState
{
    private Coroutine _countdownCoroutine;

    public StartingState(GameplayManager gameplayManager) : base(gameplayManager) { }

    public override void Enter()
    {
        _countdownCoroutine = _gameplayManager.StartCoroutine(StartGameCountDown());
    }

    public override void Exit()
    {
        if (_countdownCoroutine != null)
        {
            _gameplayManager.StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }
    }

    private IEnumerator StartGameCountDown()
    {
        for (int i = _gameplayManager.TimeBeforeStart; i >= 0; i--)
        {
            EventBus.Instance.Publish<GameEvents.GameStartingCountDown, EventData.GameStartingCountDownData>(new EventData.GameStartingCountDownData
            {
                Seconds = i,
            });
            yield return new WaitForSeconds(1f);
        }
        
        EventBus.Instance.Publish<GameEvents.GameStartingCountDownCompleted>();
        _gameplayManager.ChangeState(GameplayStateType.Playing);
    }
}