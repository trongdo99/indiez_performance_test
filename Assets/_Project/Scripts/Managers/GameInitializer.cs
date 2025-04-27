using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    public static event Action OnInitializationComplete;
    
    [Header("Manager prefabs")]
    [SerializeField] private GameObject _eventBusPrefab;
    [SerializeField] private GameObject _gameplayManagerPrefab;
    [SerializeField] private GameObject _gameUIManagerPrefab;
    [SerializeField] private GameObject _playerManagerPrefab;
    [SerializeField] private GameObject _zombieManagerPrefab;
    [SerializeField] private GameObject _zombieSpawnManagerPrefab;
    [SerializeField] private GameObject _objectPoolManagerPrefab;
    [SerializeField] private GameObject _visualEffectManagerPrefab;
    [SerializeField] private GameObject _soundEffectManagerPrefab;
    
    [Header("Scene objects")]
    [SerializeField] private CinemachineCamera _topDownCamera;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private BoxCollider _levelBoundary;

    private GameInitializer _gameInitializer;
    private readonly Dictionary<string, float> _managerWeights = new Dictionary<string, float>();
    private readonly Dictionary<string, float> _managerContributions = new Dictionary<string, float>();
    private IProgress<float> _initProgress;
    private bool _isInitialized;
    private float _currentProgress;
    private Player _player;
    private ZombieManager _zombieManager;

    private void Start()
    {
        SetupManagerWeight();
    }

    private void SetupManagerWeight()
    {
        _managerWeights.Add("GameplayManager", 0.2f);
        _managerWeights.Add("GameUIManager", 0.2f);
        _managerWeights.Add("PlayerManager", 0.2f);
        _managerWeights.Add("ZombieManager", 0.2f);
        _managerWeights.Add("ZombieSpawnManager", 0.2f);
    }

    public async Task InitializeGame(IProgress<float> initProgress = null)
    {
        if (_isInitialized)
        {
            Debug.Log("Game is already initialized, skipping");
            return;
        }

        _isInitialized = true;
        _initProgress = initProgress;
        _currentProgress = 0f;
        
        Debug.Log("Game initializing starting ...");
        
        InitializeSceneObjects();

        var managersParentObj = new GameObject("MANAGERS");
        
        await InitializeManagers(managersParentObj.transform);
        
        SetupDependencies();

        _isInitialized = false;
        Debug.Log("Game initialization completed");
        
        UpdateProgress(1f);
        OnInitializationComplete?.Invoke();
    }

    private void InitializeSceneObjects()
    {
        _mainCamera = Instantiate(_mainCamera);
        _topDownCamera = Instantiate(_topDownCamera);
        _levelBoundary = Instantiate(_levelBoundary);
    }

    private async Task InitializeManagers(Transform parent)
    {
        // Initialize manaagers in the following order:
        // 0. EventBus
        await InitializeManager<EventBus>(_eventBusPrefab, "EventBus", parent);
        
        // 1. ObjectPoolManager
        await InitializeManager<ObjectPoolManager>(_objectPoolManagerPrefab, "ObjectPoolManager", parent);
        
        // 2. VisualEffectManager
        await InitializeManager<VisualEffectManager>(_visualEffectManagerPrefab, "VisualEffectManager", parent);
        
        // 3. SoundEffectManager
        await InitializeManager<SoundEffectManager>(_soundEffectManagerPrefab, "SoundEffectManager", parent);
        
        // 4. GameplayManager
        await InitializeManager<GameplayManager>(_gameplayManagerPrefab, "GameplayManager", parent);
        
        // 5. GameUIManager
        await InitializeManager<GameUIManager>(_gameUIManagerPrefab, "GameUIManager", parent);
        
        // 6. ZombieManager
        _zombieManager = await InitializeManager<ZombieManager>(_zombieManagerPrefab, "ZombieManager", parent);

        // 7. ZombieSpawnManager
        await InitializeManager<ZombieSpawnManager>(_zombieSpawnManagerPrefab, "ZombieSpawnManager", parent);

        // 8. PlayerManager
        _player = await InitializeManager<Player>(_playerManagerPrefab, "PlayerManager", parent);
    }

    private void SetupDependencies()
    {
        Debug.Log("Setting up dependencies ...");
        
        // topDownCamera
        _topDownCamera.GetComponent<CinemachineConfiner3D>().BoundingVolume = _levelBoundary;
        
        // Player
        _player.SetupPlayerFollowCamera(_topDownCamera);
        
        // ZombieManager
        _zombieManager.SetPlayerCharacterTransform(_player.PlayerCharacterTransform);
    }

    private async Task<T> InitializeManager<T>(GameObject prefab, string managerName, Transform parent) where T : MonoBehaviour
    {
        if (prefab == null)
        {
            Debug.LogWarning($"{managerName} prefab not assigned, skipping initialization");

            if (_managerWeights.TryGetValue(managerName, out float weight))
            {
                _currentProgress += weight;
                UpdateProgress(_currentProgress);
            }

            return null;
        }
        
        Debug.Log($"Initializing {managerName} ...");
        
        GameObject managerObj = Instantiate(prefab, parent);
        managerObj.name = managerName;

        T manager = managerObj.GetComponent<T>();
        if (manager == null)
        {
            throw new InvalidOperationException($"Component of type {typeof(T).Name} not found on {managerName} prefab");
        }
        
        if (managerObj.TryGetComponent(out IAsyncInitializable initializable))
        {
            var progressTracker= new ManagerProgressTracker(this, managerName);
            await initializable.Initialize(progressTracker);
        }
        else if (managerObj.TryGetComponent(out ISyncInitializable syncInitializable))
        {
            var progressTracker= new ManagerProgressTracker(this, managerName);
            progressTracker.Report(0f);
            syncInitializable.Initialize(progressTracker);
            progressTracker.Report(1f);
        }
        else
        {
            Debug.LogWarning($"{managerName} doesn't implement IInitializable interface, skipping initialization");
            
            if (_managerWeights.TryGetValue(managerName, out float weight))
            {
                _currentProgress += weight;
                UpdateProgress(_currentProgress);
            }
        }
        
        Debug.Log($"{managerName} initialized successfully");
        return manager;
    }
    
    private void UpdateProgress(float managerProgress, string managerName = null)
    {
        if (managerName != null && _managerWeights.TryGetValue(managerName, out float weight))
        {
            float contributedProgress = weight * managerProgress;

            if (managerProgress >= 1f)
            {
                _currentProgress += weight;
                
                StoreManagerProgress(managerName, weight);
            }
            else
            {
                StoreManagerProgress(managerName, contributedProgress);
                
                _currentProgress = CalculateProgressWithoutOverlap(managerName, contributedProgress);
            }
        }

        _initProgress?.Report(_currentProgress);
        Debug.Log($"Progress: {_currentProgress:P0} - Manager: {managerName} ({managerProgress:P0})");
    }

    private void StoreManagerProgress(string managerName, float contribution)
    {
        _managerContributions[managerName] = contribution;
    }

    private float CalculateProgressWithoutOverlap(string currentManager, float newContribution)
    {
        var calculatedProgress = 0f;

        foreach (var pair in _managerContributions)
        {
            if (pair.Key == currentManager)
            {
                calculatedProgress += newContribution;
            }
            else
            {
                calculatedProgress += pair.Value;
            }
        }

        if (!_managerContributions.ContainsKey(currentManager))
        {
            calculatedProgress += newContribution;
        }

        return calculatedProgress;
    }

    private class ManagerProgressTracker : IProgress<float>
    {
        private readonly GameInitializer _initializer;
        private readonly string _managerName;
        
        public ManagerProgressTracker(GameInitializer initializer, string managerName)
        {
            _initializer = initializer;
            _managerName = managerName;
        }
        
        public void Report(float value)
        {
            value = Mathf.Clamp01(value);
            _initializer.UpdateProgress(value, _managerName);
        }
    }
}

public interface IAsyncInitializable
{
    public Task Initialize(IProgress<float> progress = null);
}

public interface ISyncInitializable
{
    public void Initialize(IProgress<float> progress = null);
}