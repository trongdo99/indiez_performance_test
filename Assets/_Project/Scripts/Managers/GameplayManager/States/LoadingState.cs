using UnityEngine;

public class LoadingState : GameplayState
{
    public LoadingState(GameplayManager gameplayManager) : base(gameplayManager) { }

    public override void Enter()
    {
        // Initialize anything needed for the loading state
        GameInitializer.OnInitializationComplete += _gameplayManager.HandleInitializationComplete;
    }

    public override void Exit()
    {
        // Clean up anything from the loading state
        GameInitializer.OnInitializationComplete -= _gameplayManager.HandleInitializationComplete;
    }
}