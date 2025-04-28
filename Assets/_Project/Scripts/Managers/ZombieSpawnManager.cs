using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class ZombieSpawnManager : MonoBehaviour, ISyncInitializable
{
    [SerializeField] private LevelWaves _levelWaves;
    [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();
    
    private List<Wave> _waves = new List<Wave>();
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
    private bool _bossSpawned;

    private class Wave
    {
        public WaveData Data;
        public int ZombiesRemaining;
        public int ZombiesKilled;
        
        public Wave(WaveData data)
        {
            Data = data;
            ZombiesRemaining = data.ZombiesToSpawn;
            ZombiesKilled = 0;
        }
    }

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
        _waves.Clear();
        
        if (_levelWaves == null)
        {
            Debug.LogError("No LevelWaves assigned to ZombieSpawnManager!");
            return;
        }
        
        foreach (WaveData waveData in _levelWaves.Waves)
        {
            _waves.Add(new Wave(waveData));
        }
        
        Debug.Log($"Initialized {_waves.Count} waves from level data");
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
        else if (_levelWaves.AutoProgressWaves && _currentWaveIndex >= 0 && _currentWaveIndex < _waves.Count)
        {
            float timeSinceWaveEnd = GameplayManager.Instance.GetGameTime() - _waveEndTime;
            if (timeSinceWaveEnd >= _levelWaves.TimeBetweenWaves)
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

        // Check if it's time to spawn a boss
        if (currentWave.Data.IncludesBoss && !_bossSpawned && 
            currentWave.ZombiesKilled >= (currentWave.Data.ZombiesToSpawn * currentWave.Data.BossSpawnTiming))
        {
            SpawnBossZombie(currentWave);
        }

        if (gameTime >= _nextSpawnTime && _totalZombiesSpawned < currentWave.Data.ZombiesToSpawn)
        {
            SpawnZombie(currentWave);
            _totalZombiesSpawned++;
            _nextSpawnTime = gameTime + (1f / currentWave.Data.SpawnRate);
        }

        _activeWaveZombies.RemoveAll(zombie => zombie == null);

        if (_totalZombiesSpawned >= currentWave.Data.ZombiesToSpawn && _activeWaveZombies.Count == 0)
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
        _bossSpawned = false;

        if (wave.Data.IncludesBoss)
        {
            AudioManager.Instance.PlayBossMusic();
        }
        
        Debug.Log($"Starting {wave.Data.WaveName} - Spawning {wave.Data.ZombiesToSpawn} zombies" + 
                  (wave.Data.IncludesBoss ? " with a boss" : ""));
    }

    private void CompleteCurrentWave()
    {
        if (_currentWaveIndex < 0 || _currentWaveIndex >= _waves.Count) return;

        _waveInProgress = false;
        _waveEndTime = GameplayManager.Instance.GetGameTime();

        Debug.Log($"Wave {_waves[_currentWaveIndex].Data.WaveName} completed");

        EventBus.Instance.Publish<GameEvents.WaveCompleted, EventData.WaveCompletedData>(new EventData.WaveCompletedData
        {
            WaveNumber = _currentWaveIndex + 1,
            IsFinalWave = _currentWaveIndex + 1 == _waves.Count - 1,
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

        int zombieTypeIndex = SelectZombieType(currentWave.Data);
        ZombieController zombie = ZombieManager.Instance.SpawnZombie(spawnPosition, spawnRotation, zombieTypeIndex);
        
        _activeWaveZombies.Add(zombie);
        if (zombie.TryGetComponent(out Health health))
        {
            health.OnHealthReachedZero += () =>
            {
                HandleZombieDeath(zombie, currentWave);
            };
        }
    }

    private void SpawnBossZombie(Wave currentWave)
    {
        Transform selectedSpawnPoint = SelectSpawnPoint();
        if (selectedSpawnPoint == null)
        {
            Debug.LogWarning("No zombie spawn points available, skipping boss spawn. Check the scene for ZombieSpawnPoint tags.");
            return;
        }
        
        Vector3 spawnPosition = selectedSpawnPoint.transform.position;
        Quaternion spawnRotation = selectedSpawnPoint.transform.rotation;

        int bossTypeIndex = currentWave.Data.BossZombieTypeIndex;
        ZombieController boss = ZombieManager.Instance.SpawnZombie(spawnPosition, spawnRotation, bossTypeIndex);
        
        _activeWaveZombies.Add(boss);
        _bossSpawned = true;
        if (boss.TryGetComponent(out Health health))
        {
            health.OnHealthReachedZero += () =>
            {
                HandleZombieDeath(boss, currentWave);
                EventBus.Instance.Publish<GameEvents.BossDefeated>();
            };
        }
        
        EventBus.Instance.Publish<GameEvents.BossSpawned>();
        
        Debug.Log($"Boss zombie spawned in wave {currentWave.Data.WaveName}");
    }

    private int SelectZombieType(WaveData waveData)
    {
        if (waveData.ZombieTypes == null || waveData.ZombieTypes.Count == 0) return 0;
        
        if (waveData.ZombieTypes.Count == 1) return waveData.ZombieTypes[0].ZombieTypeIndex;
        
        var totalWeight = 0f;
        foreach (var zombieType in waveData.ZombieTypes)
        {
            totalWeight += zombieType.Weight;
        }
        
        float randomValue = Random.Range(0f, totalWeight);
        var currentWeight = 0f;
        foreach (WaveData.ZombieTypeWeight zombieType in waveData.ZombieTypes)
        {
            currentWeight += zombieType.Weight;
            if (randomValue <= currentWeight)
            {
                return zombieType.ZombieTypeIndex;
            }
        }
        
        return waveData.ZombieTypes[0].ZombieTypeIndex;
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