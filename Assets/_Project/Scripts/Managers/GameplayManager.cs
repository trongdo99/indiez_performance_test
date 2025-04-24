using System;
using System.Collections;
using UnityEngine;

public class GameplayManager : MonoBehaviour, ISyncInitializable
{
    public enum GameState
    {
        Loading,
        Starting,
        Playing,
        Paused,
        PlayerDead,
        LevelCompleted,
        GameOver,
    }
    
    public event Action<GameState, GameState> OnGameStateChanged;
    public event Action<int> OnCountDownTick;
    public event Action OnCountDownCompleted;
    
    [SerializeField] private int _timeBeforeStart = 3;
    
    private GameState _currentState = GameState.Loading;
    
    public void Initialize(IProgress<float> progress = null)
    {
        GameInitializer.OnInitializationComplete += HandleInitializationComplete;
    }

    private void OnDestroy()
    {
        GameInitializer.OnInitializationComplete -= HandleInitializationComplete;
    }

    private void SetGameState(GameState newState)
    {
        if (_currentState == newState) return;
        
        ExitState(_currentState);
        
        GameState previousState = _currentState;
        _currentState = newState;
        Debug.Log($"Game state changed from {previousState} to {newState}");
        
        EnterState(_currentState);
        
        OnGameStateChanged?.Invoke(_currentState, previousState);
    }
    
    private void ExitState(GameState state)
    {
        switch (state)
        {
            case GameState.Loading:
                break;
            case GameState.Starting:
                break;
            case GameState.Playing:
                break;
            case GameState.Paused:
                break;
            case GameState.PlayerDead:
                break;
            case GameState.LevelCompleted:
                break;
            case GameState.GameOver:
                break;
        }
    }

    private void EnterState(GameState state)
    {
        switch (state)
        {
            case GameState.Loading:
                break;
            case GameState.Starting:
                StartCoroutine(StartGameCountDown());
                break;
            case GameState.Playing:
                break;
            case GameState.Paused:
                break;
            case GameState.PlayerDead:
                break;
            case GameState.LevelCompleted:
                break;
            case GameState.GameOver:
                break;
        }
    }
    
    private void HandleInitializationComplete()
    {
        SetGameState(GameState.Starting);
    }

    private IEnumerator StartGameCountDown()
    {
        for (int i = _timeBeforeStart; i >= 0; i--)
        {
            OnCountDownTick?.Invoke(i);
            yield return new WaitForSeconds(1f);
        }
        
        OnCountDownCompleted?.Invoke();
        
        SetGameState(GameState.Playing);
    }
}
