using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class ZombieSpawnManager : MonoBehaviour, ISyncInitializable
{
    [Serializable]
    public class Wave
    {
        public string WaveName = "Wave 1";
        public int ZombiesToSpawn = 10;
        public float SpawnRate = 1f;
        [HideInInspector] public int ZombiesRemaining;
        [HideInInspector] public int ZombiesKilled;
    }
    
    [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();
    [SerializeField] private List<Wave> _waves = new List<Wave>();
    [SerializeField] private float _timeBetweenWaves = 5f;
    [SerializeField] private bool _autoProgressWaves = true;
    
    private int _currentWaveIndex = -1;
    private float _nextSpawnTime;
    private float _waveEndTime;
    private bool _waveInProgress;
    private List<ZombieController> _activeWaveZombies = new List<ZombieController>();
    private int _totalZombiesKilled;
    private int _totalZombiesSpawned;
    private float _pauseStartTime;
    private float _totalPausedTime;
    private bool _isPaused;

    public void Initialize(IProgress<float> progress = null)
    {
        InitializeWaves();
        InitializeZombieSpawnPoints();
    }

    private void OnEnable()
    {
        EventBus.Instance.Subscribe<GameEvents.GameStateChanged, EventData.GameStateChangedData>(HandleGameStateChanged);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<GameEvents.GameStateChanged, EventData.GameStateChangedData>(HandleGameStateChanged);
    }

    private void InitializeWaves()
    {
        foreach (Wave wave in _waves)
        {
            wave.ZombiesRemaining = wave.ZombiesToSpawn;
            wave.ZombiesKilled = 0;
        }
    }

    private void InitializeZombieSpawnPoints()
    {
        if (_spawnPoints.Count != 0) return;
        
        var spawnPoints = new List<GameObject>(GameObject.FindGameObjectsWithTag("ZombieSpawnPoint"));
        if (spawnPoints.Count > 0)
        {
            foreach (GameObject spawnPoint in spawnPoints)
            {
                _spawnPoints.Add(spawnPoint.transform);
            }
        }
        else
        {
            Debug.LogWarning("No ZombieSpawnPoint found in the scene");
        }
    }

    private void Update()
    {
        if (_isPaused) return;
        
        if (_waveInProgress)
        {
            HandleWaveProgress();
        }
        else if (_autoProgressWaves && _currentWaveIndex >= 0 && _currentWaveIndex < _waves.Count)
        {
            float timeSinceWaveEnd = GameplayManager.Instance.GetGameTime() - _waveEndTime;
            if (timeSinceWaveEnd >= _timeBetweenWaves)
            {
                StartNextWave();
            }
        }
    }

    private void HandleWaveProgress()
    {
        if (_currentWaveIndex < 0 || _currentWaveIndex >= _waves.Count) return;
        
        Wave currentWave = _waves[_currentWaveIndex];
        
        float gameTime = GameplayManager.Instance.GetGameTime();

        if (gameTime >= _nextSpawnTime && _totalZombiesSpawned < currentWave.ZombiesToSpawn)
        {
            SpawnZombie(currentWave);
            _totalZombiesSpawned++;
            _nextSpawnTime = gameTime + (1f / currentWave.SpawnRate);
        }

        _activeWaveZombies.RemoveAll(zombie => zombie == null);

        if (_totalZombiesSpawned >= currentWave.ZombiesToSpawn && _activeWaveZombies.Count == 0)
        {
            CompleteCurrentWave();
        }
    }

    private void StartNextWave()
    {
        _currentWaveIndex++;

        if (_currentWaveIndex >= _waves.Count)
        {
            _waveInProgress = false;
            EventBus.Instance.Publish<GameEvents.AllWavesCompleted>();
            return;
        }
        
        Wave wave = _waves[_currentWaveIndex];
        _waveInProgress = true;
        _nextSpawnTime = Time.time; // Start spawning immediately
        _totalZombiesSpawned = 0;
        _activeWaveZombies.Clear();
        
        Debug.Log($"Starting {wave.WaveName} - Spawning {wave.ZombiesToSpawn} zombies");
    }

    private void CompleteCurrentWave()
    {
        if (_currentWaveIndex < 0 || _currentWaveIndex >= _waves.Count) return;

        _waveInProgress = false;
        _waveEndTime = GameplayManager.Instance.GetGameTime();

        Debug.Log($"Wave {_waves[_currentWaveIndex].WaveName} completed");

        EventBus.Instance.Publish<GameEvents.WaveCompleted, EventData.WaveCompletedData>(new EventData.WaveCompletedData
        {
            WaveNumber = _currentWaveIndex + 1,
            IsFinalWave = _currentWaveIndex == _waves.Count - 1,
        });

        // Check if this is the final wave
        if (_currentWaveIndex >= _waves.Count - 1)
        {
            Debug.Log("=== All waves completed ===");
            
            EventBus.Instance.Publish<GameEvents.AllWavesCompleted>();
        }
    }

    private void SpawnZombie(Wave currentWave)
    {
        if (ZombieManager.Instance == null) return;
        
        Transform selectedSpawnPoint = SelectSpawnPoint();
        if (selectedSpawnPoint == null)
        {
            Debug.LogWarning("No zombie spawn points available, skipping zombie spawn. Check the scene for ZombieSpawnPoint tags.");
            return;
        }
        
        Vector3 spawnPosition = selectedSpawnPoint.transform.position;
        Quaternion spawnRotation = selectedSpawnPoint.transform.rotation;

        ZombieController zombie = ZombieManager.Instance.SpawnZombie(spawnPosition, spawnRotation);
        
        _activeWaveZombies.Add(zombie);
        if (zombie.TryGetComponent(out Health health))
        {
            health.OnHealthReachedZero += () =>
            {
                HandleZombieDeath(zombie, currentWave);
            };
        }
    }

    private void HandleZombieDeath(ZombieController zombie, Wave wave)
    {
        wave.ZombiesKilled++;
        _totalZombiesKilled++;
        
        EventBus.Instance.Publish<GameEvents.TotalZombiesKilled, EventData.TotalZombiesKilledData>(new EventData.TotalZombiesKilledData
        {
            TotalZombiesKilled = _totalZombiesKilled
        });

        _activeWaveZombies.Remove(zombie);
    }

    private Transform SelectSpawnPoint()
    {
        if (_spawnPoints.Count == 0) return null;
        if (_spawnPoints.Count == 1) return _spawnPoints[0];
        
        int randomValue = Random.Range(0, _spawnPoints.Count);
        return _spawnPoints[randomValue].transform;
    }

    private void HandleGameStateChanged(EventData.GameStateChangedData data)
    {
        switch (data.NewState)
        {
            case GameplayStateType.Playing:
                if (data.PreviousState == GameplayStateType.Starting)
                {
                    StartNextWave();
                }
                else if (data.PreviousState == GameplayStateType.Paused)
                {
                    _isPaused = false;
                }
                break;
            case GameplayStateType.Paused:
                _isPaused = true;
                break;
        }
    }
}
