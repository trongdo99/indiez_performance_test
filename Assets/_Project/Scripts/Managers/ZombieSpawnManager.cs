using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class ZombieSpawnManager : MonoBehaviour, ISyncInitializable
{
    public static event Action OnAllWavesCompleted;
    public static event Action<int> OnWaveCompleted;
    
    [System.Serializable]
    public class SpawnPoint
    {
        public Transform Location;
    }

    [System.Serializable]
    public class Wave
    {
        public string WaveName = "Wave 1";
        public int ZombiesToSpawn = 10;
        public float SpawnRate = 1f;
        [HideInInspector] public int ZombiesRemaining;
        [HideInInspector] public int ZombiesKilled;
    }
    
    [SerializeField] private List<SpawnPoint> _spawnPoints = new List<SpawnPoint>();
    [SerializeField] private List<Wave> _waves = new List<Wave>();
    [SerializeField] private float _timeBetweenWaves = 5f;
    [SerializeField] private float _timeBeforeFirstWave = 3f;
    [SerializeField] private bool _autoProgressWaves = true;
    
    private int _currentWaveIndex = -1;
    private float _nextSpawnTime;
    private float _waveEndTime;
    private bool _waveInProgress;
    private List<ZombieController> _activeWaveZombies = new List<ZombieController>();
    private int _totalZombiesKilled;
    private int _totalZombiesSpawned;

    public void Initialize(IProgress<float> progress = null)
    {
        progress?.Report(0f);

        foreach (Wave wave in _waves)
        {
            wave.ZombiesRemaining = wave.ZombiesToSpawn;
            wave.ZombiesKilled = 0;
        }

        GameInitializer.OnInitializationComplete += HandleInitializationComplete;
    }

    private void HandleInitializationComplete()
    {
        if (_autoProgressWaves)
        {
            Invoke("StartNextWave", _timeBeforeFirstWave);
        }
    }

    private void Update()
    {
        if (_waveInProgress)
        {
            HandleWaveProgress();
        }
        else if (_autoProgressWaves && _currentWaveIndex >= 0 && _currentWaveIndex < _waves.Count)
        {
            float timeSinceWaveEnd = Time.time - _waveEndTime;
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

        if (Time.time >= _nextSpawnTime && _totalZombiesSpawned < currentWave.ZombiesToSpawn)
        {
            SpawnZombie(currentWave);
            _totalZombiesSpawned++;
            _nextSpawnTime = Time.time + (1f / currentWave.SpawnRate);
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
            OnAllWavesCompleted?.Invoke();
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
        _waveEndTime = Time.time;

        Debug.Log($"Wave {_waves[_currentWaveIndex].WaveName} completed");

        OnWaveCompleted?.Invoke(_currentWaveIndex);

        // Check if this is the final wave
        if (_currentWaveIndex >= _waves.Count - 1)
        {
            Debug.Log("=== All waves completed ===");
            
            OnAllWavesCompleted?.Invoke();
        }
    }

    private void SpawnZombie(Wave currentWave)
    {
        if (ZombieManager.Instance == null) return;
        
        SpawnPoint selectedSpawnPoint = SelectSpawnPoint();
        Vector3 spawnPosition = selectedSpawnPoint.Location.position;
        Quaternion spawnRotation = selectedSpawnPoint.Location.rotation;

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

        _activeWaveZombies.Remove(zombie);
    }

    private SpawnPoint SelectSpawnPoint()
    {
        if (_spawnPoints.Count == 0) return null;
        if (_spawnPoints.Count == 1) return _spawnPoints[0];
        
        int randomValue = Random.Range(0, _spawnPoints.Count);
        return _spawnPoints[randomValue];
    }
}
