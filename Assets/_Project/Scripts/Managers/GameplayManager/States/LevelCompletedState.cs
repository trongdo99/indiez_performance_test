using UnityEngine;

public class LevelCompletedState : GameplayState
{
    private const float SlowdownDuration = 1.5f;
    private const float TargetTimeScale = 0.3f;
    private float _currentSlowdownTime;
    private bool _slowdownComplete;
    
    public LevelCompletedState(GameplayManager gameplayManager) : base(gameplayManager) { }
    
    public override void Update()
    {
        if (_slowdownComplete) return;

        _currentSlowdownTime += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(_currentSlowdownTime / SlowdownDuration);

        float easeOutDuration = 1 - Mathf.Pow(1 - progress, 2);
        Time.timeScale = Mathf.Lerp(1f, TargetTimeScale, easeOutDuration);

        if (progress >= 1f)
        {
            _slowdownComplete = true;
            
            EventBus.Instance.Publish<GameEvents.ShowVictoryPanel>();
            
            Time.timeScale = TargetTimeScale;
        }
    }
}