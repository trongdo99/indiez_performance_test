using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class ObjectPoolManager : MonoBehaviour, ISyncInitializable
{
    public static ObjectPoolManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>();

    public void Initialize(IProgress<float> progress = null)
    {
        // noop
    }
    
    public ObjectPool<T> CreatePool<T>(T prefab, int initialSize, Transform parent = null) where T : Component
    {
        string poolKey = typeof(T).Name + "_" + prefab.name;
        
        if (_pools.ContainsKey(poolKey))
        {
            Debug.LogWarning($"Pool with key {poolKey} already exists!");
            return _pools[poolKey] as ObjectPool<T>;
        }

        ObjectPool<T> newPool = new ObjectPool<T>(prefab, initialSize, parent);
        _pools[poolKey] = newPool;
        return newPool;
    }

    public ObjectPool<T> GetPool<T>(T prefab) where T : Component
    {
        string poolKey = typeof(T).Name + "_" + prefab.name;
        
        if (!_pools.ContainsKey(poolKey))
        {
            Debug.LogWarning($"Pool with key {poolKey} doesn't exist! Creating new pool.");
            return CreatePool(prefab, 10);
        }

        return _pools[poolKey] as ObjectPool<T>;
    }
}

public abstract class ObjectPool
{
    // Base class for type safety in the dictionary
}

public class ObjectPool<T> : ObjectPool where T : Component
{
    private T _prefab;
    private Transform _parent;
    private List<T> _inactiveObjects;
    private List<T> _activeObjects;

    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        _prefab = prefab;
        _parent = parent;
        _inactiveObjects = new List<T>(initialSize);
        _activeObjects = new List<T>(initialSize);
        
        // Pre-instantiate objects
        for (int i = 0; i < initialSize; i++)
        {
            T obj = CreateNewObject();
            _inactiveObjects.Add(obj);
        }
    }

    private T CreateNewObject()
    {
        T newObj = Object.Instantiate(_prefab, _parent);
        newObj.gameObject.SetActive(false);
        return newObj;
    }

    public T Get(Vector3 position, Quaternion rotation)
    {
        T obj;
        
        if (_inactiveObjects.Count > 0)
        {
            // Get object from pool
            obj = _inactiveObjects[_inactiveObjects.Count - 1];
            _inactiveObjects.RemoveAt(_inactiveObjects.Count - 1);
        }
        else
        {
            // Create new object if pool is empty
            obj = CreateNewObject();
        }
        
        // Position the object
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.gameObject.SetActive(true);
        
        // Add to active objects
        _activeObjects.Add(obj);
        
        // Initialize poolable component if it exists
        if (obj.TryGetComponent(out IPoolable poolable))
        {
            poolable.OnGetFromPool();
        }
        
        return obj;
    }

    public void Release(T obj)
    {
        if (!_activeObjects.Contains(obj))
        {
            Debug.LogWarning($"Trying to release {obj.name} which isn't in the active pool!");
            return;
        }
        
        // Call release method if it implements IPoolable
        if (obj.TryGetComponent(out IPoolable poolable))
        {
            poolable.OnReleaseToPool();
        }
        
        // Deactivate and move back to inactive pool
        obj.gameObject.SetActive(false);
        _activeObjects.Remove(obj);
        _inactiveObjects.Add(obj);
    }

    public int ActiveCount => _activeObjects.Count;
    public int InactiveCount => _inactiveObjects.Count;
    public int TotalCount => _activeObjects.Count + _inactiveObjects.Count;
}

public interface IPoolable
{
    void OnGetFromPool();
    void OnReleaseToPool();
}
