using System;
using System.Collections;
using UnityEngine;

public class GameplayManager : MonoBehaviour, ISyncInitializable
{
    public static GameplayManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

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
    private float _pauseStartTime;
    private float _totalPausedTime;
    
    public bool IsGamePaused => _currentState == GameState.Paused;
    
    public void Initialize(IProgress<float> progress = null)
    {
        GameInitializer.OnInitializationComplete += HandleInitializationComplete;
    }

    private void OnDestroy()
    {
        GameInitializer.OnInitializationComplete -= HandleInitializationComplete;

        // Reset the game state in case the game was destroyed while paused
        Time.timeScale = 1f;
    }

    private void SetGameState(GameState newState)
    {
        if (_currentState == newState) return;
        
        ExitState(_currentState);
        
        GameState previousState = _currentState;
        _currentState = newState;
        Debug.Log($"Game state changed from {previousState} to {newState}");
        
        EnterState(_currentState, previousState);
        
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
                Time.timeScale = 1f;
                break;
            case GameState.PlayerDead:
                break;
            case GameState.LevelCompleted:
                break;
            case GameState.GameOver:
                break;
        }
    }

    private void EnterState(GameState state, GameState previousState)
    {
        switch (state)
        {
            case GameState.Loading:
                break;
            case GameState.Starting:
                StartCoroutine(StartGameCountDown());
                break;
            case GameState.Playing:
                if (previousState == GameState.Paused)
                {
                    _totalPausedTime += Time.time - _pauseStartTime;
                }
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                _pauseStartTime = Time.time;
                break;
            case GameState.PlayerDead:
                break;
            case GameState.LevelCompleted:
                break;
            case GameState.GameOver:
                break;
        }
    }

    public float GetGameTime()
    {
        return Time.time - _totalPausedTime;
    }

    public void PauseGame()
    {
        SetGameState(GameState.Paused);
    }

    public void ResumeGame()
    {
        SetGameState(GameState.Playing);
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
