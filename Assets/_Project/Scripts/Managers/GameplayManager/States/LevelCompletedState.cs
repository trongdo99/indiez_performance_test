public class LevelCompletedState : GameplayState
{
    public LevelCompletedState(GameplayManager gameplayManager) : base(gameplayManager) { }

    public override void Enter()
    {
        // TODO: Reload the current scene for now
        SceneLoader.Instance.ReloadCurrentSceneAsync();
    }
}