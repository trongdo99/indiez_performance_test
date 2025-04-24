using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    public static event Action OnInitializationComplete;
    
    [Header("Manager prefabs")]
    [SerializeField] private GameObject _playerManagerPrefab;
    [SerializeField] private GameObject _zombieManagerPrefab;
    [SerializeField] private GameObject _zombieSpawnManagerPrefab;
    
    [Header("Scene objects")]
    [SerializeField] private CinemachineCamera _topDownCamera;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private BoxCollider _levelBoundary;
    [SerializeField] private Canvas _gameUI;

    private Dictionary<Type, MonoBehaviour> _managers = new Dictionary<Type, MonoBehaviour>();
    private GameInitializer _gameInitializer;
    private readonly Dictionary<string, float> _managerWeights = new Dictionary<string, float>();
    private readonly Dictionary<string, float> _managerContributions = new Dictionary<string, float>();
    private IProgress<float> _initProgress;
    private bool _isInitialized;
    private float _currentProgress;
    
    private Player _player;
    private ZombieManager _zombieManager;
    private ZombieSpawnManager _zombieSpawnManager;

    private void Start()
    {
        SetupManagerWeight();
    }

    private void SetupManagerWeight()
    {
        _managerWeights.Add("PlayerManager", 0.33f);
        _managerWeights.Add("ZombieManager", 0.33f);
        _managerWeights.Add("ZombieSpawnManager", 0.33f);
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
        _gameUI = Instantiate(_gameUI);
    }

    private async Task InitializeManagers(Transform parent)
    {
        // Initialize manaagers in the following order:
        try
        {
            // 1. ZombieManager
            _zombieManager = await InitializeManager<ZombieManager>(_zombieManagerPrefab, "ZombieManager", parent);

            // 2. ZombieSpawnManager
            _zombieSpawnManager =
                await InitializeManager<ZombieSpawnManager>(_zombieSpawnManagerPrefab, "ZombieSpawnManager", parent);

            // 3. PlayerManager
            _player = await InitializeManager<Player>(_playerManagerPrefab, "PlayerManager", parent);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during manager initialization: {e.Message}");
        }
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

            throw new InvalidOperationException($"Prefab for {managerName} is not assigned");
        }
        
        Debug.Log($"Initializing {managerName} ...");
        
        GameObject managerObj = Instantiate(prefab, parent);
        managerObj.name = managerName;

        T manager = managerObj.GetComponent<T>();
        if (manager == null)
        {
            throw new InvalidOperationException($"Component of type {typeof(T).Name} not found on {managerName} prefab");
        }
        else
        {
            _managers[typeof(T)] = manager;
        }
        
        if (managerObj.TryGetComponent(out IInitializable initializable))
        {
            var progressTracker= new ManagerProgressTracker(this, managerName);
            await initializable.Initialize(progressTracker);
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

    private void SetupDependencies()
    {
        _topDownCamera.GetComponent<CinemachineConfiner3D>().BoundingVolume = _levelBoundary;
        _player.SetupPlayerFollowCamera(_topDownCamera);
        _zombieManager.SetPlayerCharacterTransform(_player.PlayerCharacterTransform);
    }
    
    public T GetManager<T>() where T : MonoBehaviour
    {
        if (_managers.TryGetValue(typeof(T), out MonoBehaviour manager))
        {
            return manager as T;;
        }
        
        Debug.LogWarning($"Manager of type {typeof(T).Name} not found");
        return null;
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

public interface IInitializable
{
    public Task Initialize(IProgress<float> progress = null);
}