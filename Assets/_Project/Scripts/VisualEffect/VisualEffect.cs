using System;
using UnityEngine;

public class VisualEffect : MonoBehaviour, IPoolable
{
    [SerializeField] private float _lifetime = 2f;
    [SerializeField] private bool _useParticleSystemLifetime = true;
    [SerializeField] private bool _autoDeactivate = true;
    [SerializeField] private bool _looping = false;
    
    private ParticleSystem[] _particleSystems;
    private float _timer;
    private bool _isActive;
    private Action<VisualEffect> _onEffectComplete;
    private float _calculatedParticleSystemLifetime = -1f;  // Cache this value
    private bool _isComplete;
    
    protected void Awake()
    {
        _particleSystems = GetComponentsInChildren<ParticleSystem>();
        CalculateParticleSystemLifetime();
    }
    
    private void CalculateParticleSystemLifetime()
    {
        // If we have particle systems, calculate the longest duration
        if (_particleSystems != null && _particleSystems.Length > 0)
        {
            float longestDuration = 0f;
            
            foreach (var ps in _particleSystems)
            {
                var main = ps.main;
                float totalDuration = main.duration + main.startLifetime.constantMax;
                
                if (totalDuration > longestDuration)
                {
                    longestDuration = totalDuration;
                }
            }
            
            _calculatedParticleSystemLifetime = longestDuration;
            
            // If using particle system lifetime, set it now
            if (_useParticleSystemLifetime)
            {
                _lifetime = _calculatedParticleSystemLifetime;
            }
        }
    }
    
    private void Update()
    {
        if (!_isActive || _isComplete) return;
        
        if (_looping && _autoDeactivate == false) return;
        
        _timer += Time.deltaTime;
        
        // Check either timer or particle systems
        bool shouldComplete = false;
        
        if (_autoDeactivate && _timer >= _lifetime)
        {
            shouldComplete = true;
        }
        else if (!_looping)
        {
            // For non-looping effects, check if all particle systems have stopped
            shouldComplete = true;
            foreach (var ps in _particleSystems)
            {
                if (ps.isPlaying && ps.particleCount > 0)
                {
                    shouldComplete = false;
                    break;
                }
            }
        }
        
        if (shouldComplete)
        {
            CompleteEffect();
        }
    }
    
    public void Play(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        transform.position = position;
        transform.rotation = rotation;
        
        if (parent != null)
        {
            transform.SetParent(parent);
        }
        
        _isActive = true;
        _isComplete = false;
        _timer = 0f;
        
        if (_particleSystems != null)
        {
            foreach (var ps in _particleSystems)
            {
                ps.Play();
            }
        }
    }
    
    public void SetLifetime(float lifetime)
    {
        // Only accept this lifetime if we're not using particle system lifetime
        if (!_useParticleSystemLifetime)
        {
            _lifetime = lifetime;
        }
    }
    
    public void SetUseParticleSystemLifetime(bool useParticleSystemLifetime)
    {
        _useParticleSystemLifetime = useParticleSystemLifetime;
        
        // If switching to using particle system lifetime, update the lifetime value
        if (_useParticleSystemLifetime && _calculatedParticleSystemLifetime > 0)
        {
            _lifetime = _calculatedParticleSystemLifetime;
        }
    }

    public void SetAutoDeactivate(bool autoDeactivate)
    {
        _autoDeactivate = autoDeactivate;
    }
    
    public void SetLooping(bool looping)
    {
        _looping = looping;
        
        if (_particleSystems != null)
        {
            foreach (var ps in _particleSystems)
            {
                var main = ps.main;
                main.loop = looping;
            }
        }
    }
    
    public void SetOnCompleteCallback(Action<VisualEffect> callback)
    {
        _onEffectComplete = callback;
    }
    
    public void CompleteEffect()
    {
        if (_isComplete) return;
        
        _isActive = false;
        _isComplete = true;

        if (_particleSystems != null)
        {
            foreach (ParticleSystem ps in _particleSystems)
            {
                ps.Stop(true);
            }
        }

        if (_onEffectComplete != null)
        {
            Action<VisualEffect> callback = _onEffectComplete;
            // Clear the callback so we don't call it twice
            _onEffectComplete = null;
            callback.Invoke(this);
        }
    }
    
    public virtual void OnGetFromPool()
    {
        _isActive = false;
        _timer = 0f;
        _onEffectComplete = null;
        gameObject.SetActive(true);
        
        // Reset to original lifetime if using particle system lifetime
        if (_useParticleSystemLifetime && _calculatedParticleSystemLifetime > 0)
        {
            _lifetime = _calculatedParticleSystemLifetime;
        }
    }
    
    public virtual void OnReleaseToPool()
    {
        // Ensure we complete the effect before releasing it to the pool
        if (_isActive && !_isComplete)
        {
            CompleteEffect();
        }
        
        _isActive = false;
        _timer = 0f;
        
        if (_particleSystems != null)
        {
            foreach (var ps in _particleSystems)
            {
                ps.Stop(true);
                ps.Clear();
            }
        }
        
        transform.SetParent(null);
        gameObject.SetActive(false);
    }
    
    // Public method to get the lifetime (useful for debugging)
    public float GetLifetime()
    {
        return _lifetime;
    }
    
    // Public method to get whether we're using particle system lifetime
    public bool IsUsingParticleSystemLifetime()
    {
        return _useParticleSystemLifetime;
    }
}