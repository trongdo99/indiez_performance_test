using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour, ISyncInitializable
{
    public static ObjectPoolManager Instance { get; private set; }
    
    [SerializeField] private Transform _poolsParent;
    
    private Dictionary<Type, Dictionary<int, object>> _poolsByPrefabId = new Dictionary<Type, Dictionary<int, object>>();
    private Dictionary<Type, Transform> _poolParents = new Dictionary<Type, Transform>();

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
        if (_poolsParent == null)
        {
            _poolsParent = transform;
        }
    }

    public ObjectPool<T> CreatePool<T>(T prefab, int initialSize, Transform parent = null, int maxSize = 100, bool shouldExpand = true) where T : Component, IPoolable
    {
        if (prefab == null)
        {
            Debug.LogError("Cannot create pool with null prefab");
            return null;
        }
        
        // Create a type-specific parent if not provided
        Transform poolParent = parent;
        if (poolParent == null)
        {
            Type type = typeof(T);
            if (!_poolParents.TryGetValue(type, out poolParent))
            {
                // Create a new parent for this type
                GameObject parentObj = new GameObject($"{type.Name}Pool");
                parentObj.transform.SetParent(_poolsParent);
                poolParent = parentObj.transform;
                _poolParents[type] = poolParent;
            }
        }
        
        // Create a prefab-specific parent
        GameObject prefabPoolObj = new GameObject($"{prefab.name}Pool");
        prefabPoolObj.transform.SetParent(poolParent);
        
        // Create the pool
        var pool = new ObjectPool<T>(prefab, initialSize, prefabPoolObj.transform, maxSize, shouldExpand);
        
        // Store the pool by prefab ID
        int prefabId = prefab.GetInstanceID();
        Type componentType = typeof(T);
        
        if (!_poolsByPrefabId.TryGetValue(componentType, out var typePools))
        {
            typePools = new Dictionary<int, object>();
            _poolsByPrefabId[componentType] = typePools;
        }
        
        typePools[prefabId] = pool;
        
        return pool;
    }

    public ObjectPool<T> GetPool<T>(T prefab) where T : Component, IPoolable
    {
        if (prefab == null) return null;
        
        int prefabId = prefab.GetInstanceID();
        Type componentType = typeof(T);
        
        if (_poolsByPrefabId.TryGetValue(componentType, out var typePools))
        {
            if (typePools.TryGetValue(prefabId, out var pool))
            {
                return (ObjectPool<T>)pool;
            }
        }
        
        return null;
    }
    
    public T Get<T>(T prefab, Vector3 position = default, Quaternion rotation = default, int initialPoolSize = 5) where T : Component, IPoolable
    {
        ObjectPool<T> pool = GetPool(prefab);
        
        // Create pool if it doesn't exist
        if (pool == null)
        {
            pool = CreatePool(prefab, initialPoolSize);
        }
        
        if (pool != null)
        {
            return pool.Get(position, rotation);
        }
        
        return null;
    }
    
    public bool Release<T>(T obj) where T : Component, IPoolable
    {
        if (obj == null) return false;
        
        Type componentType = typeof(T);
        
        // Try to find the pool that owns this object
        if (_poolsByPrefabId.TryGetValue(componentType, out var typePools))
        {
            foreach (var poolEntry in typePools)
            {
                var pool = (ObjectPool<T>)poolEntry.Value;
                if (pool.IsPooledObject(obj))
                {
                    return pool.Release(obj);
                }
            }
        }
        
        // If no pool was found, destroy the object
        Destroy(obj.gameObject);
        return false;
    }
    
    public void ReleaseAll<T>() where T : Component, IPoolable
    {
        Type componentType = typeof(T);
        
        if (_poolsByPrefabId.TryGetValue(componentType, out var typePools))
        {
            foreach (var poolEntry in typePools)
            {
                var pool = (ObjectPool<T>)poolEntry.Value;
                pool.ReleaseAll();
            }
        }
    }
    
    public void ClearAllPools()
    {
        _poolsByPrefabId.Clear();
        
        // Destroy all pool parent objects
        foreach (var parent in _poolParents.Values)
        {
            if (parent != null)
            {
                Destroy(parent.gameObject);
            }
        }
        
        _poolParents.Clear();
    }
}