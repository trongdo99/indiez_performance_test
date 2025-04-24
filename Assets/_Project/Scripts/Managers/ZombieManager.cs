using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ZombieManager : MonoBehaviour, IInitializable
{
    public static ZombieManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [System.Serializable]
    public class ZombieType
    {
        public string TypeName;
        public GameObject ZombiePrefab;
    }
    
    [SerializeField] private List<ZombieType> _zombieTypes;
    
    private List<ZombieController> _activeZombies = new List<ZombieController>();
    private Transform _playerTransform;

    public async Task Initialize(IProgress<float> progress = null)
    {
        progress?.Report(1f);
    }

    public void SetPlayerCharacterTransform(Transform playerTransform)
    {
        _playerTransform = playerTransform;
        foreach (ZombieController zombie in _activeZombies)
        {
            zombie.SetTarget(_playerTransform);
        }
    }

    public ZombieController SpawnZombie(Vector3 position, Quaternion rotation, int specificTypeIndex = -1)
    {
        ZombieType selectedType = _zombieTypes[0];
        if (specificTypeIndex >= 0 && specificTypeIndex < _zombieTypes.Count)
        {
            selectedType = _zombieTypes[specificTypeIndex];
        }

        GameObject zombieObj = Instantiate(selectedType.ZombiePrefab, position, rotation);
        var zombieController = zombieObj.GetComponent<ZombieController>();
        if (zombieController == null)
        {
            Debug.LogError($"Zombie prefab {selectedType.TypeName} doesn't have a ZombieController component");
            Destroy(zombieObj);
            return null;
        }

        if (_playerTransform != null)
        {
            zombieController.SetTarget(_playerTransform);
        }
        
        _activeZombies.Add(zombieController);

        if (zombieController.TryGetComponent(out Health health))
        {
            health.OnHealthReachedZero += () => HandleZombieDeath(zombieController);
        }
        
        Debug.Log($"Spawned {selectedType.TypeName} zombie at {position}");
        return zombieController;
    }

    private void HandleZombieDeath(ZombieController zombie)
    {
        _activeZombies.Remove(zombie);
    }
}
