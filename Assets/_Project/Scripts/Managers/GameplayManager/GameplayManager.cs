using System;
using UnityEngine;

public enum GameplayStateType
{
    Loading,
    Starting,
    Playing,
    Paused,
    PlayerDead,
    LevelCompleted,
    GameOver
}

public class GameplayManager : StateMachine<GameplayStateType>, ISyncInitializable
{
    public static GameplayManager Instance { get; private set; }

    public event Action<int> OnCountDownTick;
    public event Action OnCountDownCompleted;
    
    [SerializeField] private int _timeBeforeStart = 3;
    
    private ZombieSpawnManager _zombieSpawnManager;
    private float _pauseStartTime;
    private float _totalPausedTime;
    
    public bool IsGamePaused => CurrentStateType == GameplayStateType.Paused;
    public int TimeBeforeStart => _timeBeforeStart;
    public float PauseStartTime { get => _pauseStartTime; set => _pauseStartTime = value; }
    public float TotalPausedTime { get => _totalPausedTime; set => _totalPausedTime = value; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;

        // Initialize state dictionary
        _states[GameplayStateType.Loading] = new LoadingState(this);
        _states[GameplayStateType.Starting] = new StartingState(this);
        _states[GameplayStateType.Playing] = new PlayingState(this);
        _states[GameplayStateType.Paused] = new PausedState(this);
        _states[GameplayStateType.PlayerDead] = new PlayerIsDeadState(this);
        _states[GameplayStateType.LevelCompleted] = new LevelCompletedState(this);
        _states[GameplayStateType.GameOver] = new GameOverState(this);

        // Set initial state
        _currentStateType = GameplayStateType.Loading;
        _currentState = _states[_currentStateType];
        // Manually call Enter() to initialize the state
        _currentState.Enter();
    }

    public void Initialize(IProgress<float> progress = null)
    {
        ChangeState(GameplayStateType.Loading);
    }
    
    public void SetZombieSpawnManager(ZombieSpawnManager zombieSpawnManager)
    {
        _zombieSpawnManager = zombieSpawnManager;
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<GameEvents.AllWavesCompleted>(HandleAllWavesCompleted);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<GameEvents.AllWavesCompleted>(HandleAllWavesCompleted);
    }

    private void OnDestroy()
    {
        GameInitializer.OnInitializationComplete -= HandleInitializationComplete;
        
        // Reset the game state in case the game was destroyed while paused
        Time.timeScale = 1f;
        
        Instance = null;
    }
    
    public void TriggerCountDownTick(int seconds)
    {
        OnCountDownTick?.Invoke(seconds);
    }

    public void TriggerCountDownCompleted()
    {
        OnCountDownCompleted?.Invoke();
    }

    public override void ChangeState(GameplayStateType newStateType)
    {
        if (_currentStateType == newStateType) return;
        
        GameplayStateType previousState = _currentStateType;
        
        base.ChangeState(newStateType);
        
        Debug.Log($"Invoking OnGameStateChanged with new={_currentStateType}, prev={previousState}");
        EventBus.Instance.Publish<GameEvents.GameStateChanged, EventData.GameStateChangedData>(new EventData.GameStateChangedData
        {
            NewState = _currentStateType,
            PreviousState = previousState,
        });
    }

    public float GetGameTime()
    {
        return Time.time - _totalPausedTime;
    }

    public void PauseGame()
    {
        ChangeState(GameplayStateType.Paused);
    }

    public void ResumeGame()
    {
        ChangeState(GameplayStateType.Playing);
    }
    
    public void HandleInitializationComplete()
    {
        ChangeState(GameplayStateType.Starting);
    }

    private void HandleAllWavesCompleted()
    {
        ChangeState(GameplayStateType.LevelCompleted);
    }
}