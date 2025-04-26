using System;
using System.Collections.Generic;
using UnityEngine;

public class EventBus : MonoBehaviour, ISyncInitializable
{
    public static EventBus Instance { get; private set; }

    private readonly Dictionary<Type, List<Delegate>> _eventHandlers = new Dictionary<Type, List<Delegate>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        ClearAllHandlers();
        Instance = null;
    }

    public void Initialize(IProgress<float> progress = null)
    {
        //noop
    }

    public void Subscribe<TEvent>(Action handler) where TEvent : struct
    {
        Type eventType = typeof(TEvent);
        
        if (!_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            handlers = new List<Delegate>();
            _eventHandlers[eventType] = handlers;
        }
        
        handlers.Add(handler);
    }

    public void Subscribe<TEvent, TParam>(Action<TParam> handler) where TEvent : struct
    {
        Type eventType = typeof(TEvent);
        
        if (!_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            handlers = new List<Delegate>();
            _eventHandlers[eventType] = handlers;
        }
        
        handlers.Add(handler);
    }

    public void Unsubscribe<TEvent>(Action handler) where TEvent : struct
    {
        Type eventType = typeof(TEvent);
        
        if (_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
            
            // Clean up empty lists
            if (handlers.Count == 0)
            {
                _eventHandlers.Remove(eventType);
            }
        }
    }

    public void Unsubscribe<TEvent, TParam>(Action<TParam> handler) where TEvent : struct
    {
        Type eventType = typeof(TEvent);
        
        if (_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
            
            // Clean up empty lists
            if (handlers.Count == 0)
            {
                _eventHandlers.Remove(eventType);
            }
        }
    }

    public void Publish<TEvent>() where TEvent : struct
    {
        Type eventType = typeof(TEvent);
        
        if (_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            // Create a copy to avoid issues if handlers are added/removed during iteration
            var handlersCopy = new List<Delegate>(handlers);
            
            foreach (var handler in handlersCopy)
            {
                if (handler is Action action)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error publishing event {typeof(TEvent).Name}: {e}");
                    }
                }
            }
        }
    }

    public void Publish<TEvent, TParam>(TParam param) where TEvent : struct
    {
        Type eventType = typeof(TEvent);
        
        if (_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            // Create a copy to avoid issues if handlers are added/removed during iteration
            var handlersCopy = new List<Delegate>(handlers);
            
            foreach (var handler in handlersCopy)
            {
                if (handler is Action<TParam> action)
                {
                    try
                    {
                        action(param);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error publishing event {typeof(TEvent).Name} with param {typeof(TParam).Name}: {e}");
                    }
                }
            }
        }
    }

    public void ClearAllHandlers()
    {
        _eventHandlers.Clear();
    }

    public void ClearHandlers<TEvent>() where TEvent : struct
    {
        Type eventType = typeof(TEvent);
        _eventHandlers.Remove(eventType);
    }
    
    public int GetSubscriberCount<TEvent>() where TEvent : struct
    {
        Type eventType = typeof(TEvent);
        
        if (_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            return handlers.Count;
        }
        
        return 0;
    }
}