using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class ObjectPool<T> where T : Component, IPoolable
{
    private readonly T _prefab;
    private readonly Transform _poolParent;
    private readonly Stack<T> _inactiveObjects;
    private readonly List<T> _activeObjects;
    private readonly int _initialSize;
    private readonly int _maxSize;
    private readonly bool _shouldExpand;
    
    public int ActiveCount => _activeObjects.Count;
    public int InactiveCount => _inactiveObjects.Count;
    public int TotalCount => _activeObjects.Count + _inactiveObjects.Count;
    public T Prefab => _prefab;

    public ObjectPool(T prefab, int initialSize, Transform parent = null, int maxSize = 100, bool shouldExpand = true)
    {
        _prefab = prefab;
        _poolParent = parent;
        _initialSize = initialSize;
        _maxSize = maxSize;
        _shouldExpand = shouldExpand;
        _inactiveObjects = new Stack<T>(initialSize);
        _activeObjects = new List<T>(initialSize);

        // Initialize the pool with inactive objects
        InitializePool();
    }
    
    public ObjectPool(Func<T> creationMethod, int initialSize, Transform parent = null, int maxSize = 100, bool shouldExpand = true)
    {
        _prefab = creationMethod.Invoke();
        _poolParent = parent;
        _initialSize = initialSize;
        _maxSize = maxSize;
        _shouldExpand = shouldExpand;
        _inactiveObjects = new Stack<T>(initialSize);
        _activeObjects = new List<T>(initialSize);

        // Initialize the pool with inactive objects
        InitializePool();
    }

    private void InitializePool()
    {
        for (int i = 0; i < _initialSize; i++)
        {
            CreateNewInstance(false);
        }
    }

    private T CreateNewInstance(bool setActive = false)
    {
        if (_inactiveObjects.Count + _activeObjects.Count >= _maxSize)
        {
            Debug.LogWarning($"Object pool for {_prefab.name} has reached its maximum size of {_maxSize}");
            return null;
        }

        T newObject = Object.Instantiate(_prefab, _poolParent);
        newObject.gameObject.SetActive(setActive);
        
        if (!setActive)
        {
            _inactiveObjects.Push(newObject);
        }
        
        return newObject;
    }

    public T Get(Vector3 position = default, Quaternion rotation = default)
    {
        T obj;

        if (_inactiveObjects.Count > 0)
        {
            obj = _inactiveObjects.Pop();
        }
        else if (_shouldExpand)
        {
            obj = CreateNewInstance(true);
            if (obj == null)
            {
                // Return the oldest active object if we can't create more
                if (_activeObjects.Count > 0)
                {
                    obj = _activeObjects[0];
                    _activeObjects.RemoveAt(0);
                }
                else
                {
                    Debug.LogError($"Failed to get object from pool for {_prefab.name}: pool is empty and can't expand");
                    return null;
                }
            }
        }
        else
        {
            // If we can't expand and there are no inactive objects, return null
            Debug.LogWarning($"No objects available in pool for {_prefab.name} and pool cannot expand");
            return null;
        }

        if (obj != null)
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.gameObject.SetActive(true);
            obj.OnGetFromPool();
            _activeObjects.Add(obj);
        }

        return obj;
    }

    public bool Release(T obj)
    {
        if (obj == null) return false;

        // Check if this object belongs to this pool
        if (!IsPooledObject(obj)) return false;

        // Remove from active list
        _activeObjects.Remove(obj);

        // Reset the object
        obj.OnReleaseToPool();
        obj.transform.SetParent(_poolParent);
        
        // Add back to inactive stack
        _inactiveObjects.Push(obj);

        return true;
    }

    public void ReleaseAll()
    {
        // Create a copy of active objects to avoid modification during iteration
        var objectsToRelease = new List<T>(_activeObjects);
        
        foreach (var obj in objectsToRelease)
        {
            Release(obj);
        }
    }

    public bool IsPooledObject(T obj)
    {
        // Check if the object belongs to this pool
        return _activeObjects.Contains(obj) || _inactiveObjects.Contains(obj);
    }
}