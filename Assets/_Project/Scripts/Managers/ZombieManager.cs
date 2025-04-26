using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieManager : MonoBehaviour, ISyncInitializable
{
    public static ZombieManager Instance { get; private set; }

    [Serializable]
    public class ZombieType
    {
        public string TypeName;
        public GameObject ZombiePrefab;
        public int InitialPoolSize = 10;
        [HideInInspector] public ZombieController Controller; // Cached controller component
        [HideInInspector] public ObjectPool<ZombieController> Pool; // Cached pool reference
    }
    
    [SerializeField] private List<ZombieType> _zombieTypes;
    
    private List<ZombieController> _activeZombies = new List<ZombieController>();
    private Transform _playerTransform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Initialize(IProgress<float> progress = null)
    {
        // Initialize pools for each zombie type
        for (int i = 0; i < _zombieTypes.Count; i++)
        {
            progress?.Report((float)i / _zombieTypes.Count * 0.9f);
            
            ZombieType zombieType = _zombieTypes[i];
            
            // Cache the controller component
            zombieType.Controller = zombieType.ZombiePrefab.GetComponent<ZombieController>();
            
            if (zombieType.Controller == null)
            {
                Debug.LogError($"Zombie prefab {zombieType.TypeName} doesn't have a ZombieController component");
                continue;
            }
            
            // Create pool for this zombie type and cache it
            zombieType.Pool = ObjectPoolManager.Instance.CreatePool(
                zombieType.Controller, 
                zombieType.InitialPoolSize, 
                transform
            );
            
            Debug.Log($"Created pool for {zombieType.TypeName} with initial size {zombieType.InitialPoolSize}");
        }
    }

    public void SetPlayerCharacterTransform(Transform playerTransform)
    {
        _playerTransform = playerTransform;
        foreach (ZombieController zombie in _activeZombies)
        {
            zombie.SetTarget(_playerTransform);
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    // ReSharper disable Unity.PerformanceAnalysis
    public ZombieController SpawnZombie(Vector3 position, Quaternion rotation, int typeIndex = 0)
    {
        // Validate typeIndex
        if (typeIndex < 0 || typeIndex >= _zombieTypes.Count)
        {
            Debug.LogWarning($"Invalid zombie type index {typeIndex}. Using default type 0.");
            typeIndex = 0;
        }
        
        ZombieType selectedType = _zombieTypes[typeIndex];
        
        // Quick check that we have a valid pool
        if (selectedType.Pool == null)
        {
            Debug.LogError($"No pool exists for zombie type {selectedType.TypeName}");
            return null;
        }
        
        // Get zombie directly from the cached pool
        ZombieController zombieController = selectedType.Pool.Get(position, rotation);
        
        if (zombieController == null)
        {
            Debug.LogError($"Failed to get zombie from pool for type {selectedType.TypeName}");
            return null;
        }
        
        // Set target if player exists
        if (_playerTransform != null)
        {
            zombieController.SetTarget(_playerTransform);
        }
        
        // Set ZombieManager reference on the zombie controller
        zombieController.SetManager(this);
        
        _activeZombies.Add(zombieController);

        Debug.Log($"Spawned {selectedType.TypeName} zombie at {position}");
        return zombieController;
    }

    public void HandleZombieDeath(ZombieController zombie)
    {
        _activeZombies.Remove(zombie);
    }

    public void HandleZombieDeathSequenceComplete(ZombieController zombie)
    {
        StartCoroutine(ReturnZombieToPoolAfterDelay(zombie, 1f));
    }
    
    private IEnumerator ReturnZombieToPoolAfterDelay(ZombieController zombie, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Find the zombie type
        int typeIndex = GetZombieTypeIndex(zombie);
        if (typeIndex >= 0)
        {
            // Access the cached pool directly
            _zombieTypes[typeIndex].Pool.Release(zombie);
            
            Debug.Log($"Returned {_zombieTypes[typeIndex].TypeName} zombie to pool");
        }
        else
        {
            Debug.LogWarning($"Could not determine zombie type for {zombie.name}. Unable to return to pool.");
        }
    }
    
    private int GetZombieTypeIndex(ZombieController zombie)
    {
        string zombieName = zombie.gameObject.name.Replace("(Clone)", "");
        
        for (int i = 0; i < _zombieTypes.Count; i++)
        {
            if (_zombieTypes[i].ZombiePrefab.name == zombieName)
            {
                return i;
            }
        }
        
        return -1;
    }
}