using System;
using System.Collections.Generic;
using UnityEngine;

public class VisualEffectManager : MonoBehaviour, ISyncInitializable
{
    public static VisualEffectManager Instance { get; private set; }
    
    [Header("Effect References")]
    [SerializeField] private List<VisualEffectData> _effectDataList = new List<VisualEffectData>();
    [SerializeField] private bool _applyQualitySettings = true;
    [SerializeField] [Range(0, 1)] private float _globalEffectScale = 1f;
    
    private Dictionary<string, VisualEffectData> _effectDataById = new Dictionary<string, VisualEffectData>();
    private Dictionary<string, ObjectPool<VisualEffect>> _effectPools = new Dictionary<string, ObjectPool<VisualEffect>>();
    private HashSet<VisualEffect> _activeEffects = new HashSet<VisualEffect>();
    private Dictionary<string, int> _concurrentEffectCounts = new Dictionary<string, int>();
    private Dictionary<VisualEffect, string> _effectToIdMap = new Dictionary<VisualEffect, string>();
    
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
        InitializeEffectDatabase();
        InitializePools();
    }
    
    private void InitializeEffectDatabase()
    {
        // Clear existing data
        _effectDataById.Clear();
        
        // Add all effect data to the dictionary
        foreach (VisualEffectData effectData in _effectDataList)
        {
            if (effectData == null) continue;
            
            string effectId = effectData.EffectId;
            
            if (string.IsNullOrEmpty(effectId))
            {
                Debug.LogWarning($"Visual Effect Data has empty ID: {effectData.name}");
                continue;
            }
            
            if (_effectDataById.ContainsKey(effectId))
            {
                Debug.LogWarning($"Duplicate Visual Effect ID found: {effectId}. Only the first occurrence will be used.");
                continue;
            }
            
            _effectDataById.Add(effectId, effectData);
            _concurrentEffectCounts[effectId] = 0;
        }
    }
    
    private void InitializePools()
    {
        // Create pools for each effect
        foreach (var effectEntry in _effectDataById)
        {
            string effectId = effectEntry.Key;
            VisualEffectData effectData = effectEntry.Value;
            
            if (effectData.EffectPrefab == null)
            {
                Debug.LogWarning($"Effect prefab not assigned for ID: {effectId}");
                continue;
            }
            
            var pool = ObjectPoolManager.Instance.CreatePool(
                effectData.EffectPrefab,
                effectData.InitialPoolSize,
                transform
            );
            
            _effectPools[effectId] = pool;
        }
    }
    
    public VisualEffect PlayEffect(string effectId, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        // Check if the effect exists
        if (!_effectDataById.TryGetValue(effectId, out VisualEffectData effectData))
        {
            Debug.LogWarning($"Effect data not found for ID: {effectId}");
            return null;
        }
        
        // Get the pool for this effect
        if (!_effectPools.TryGetValue(effectId, out var pool))
        {
            Debug.LogWarning($"Effect pool not found for ID: {effectId}");
            return null;
        }
        
        // Check if we've reached the max concurrent instances for this effect
        if (_concurrentEffectCounts[effectId] >= effectData.MaxConcurrentInstances)
        {
            return null;
        }
        
        // Get the effect from the pool
        VisualEffect effect = pool.Get(position, rotation);
        
        if (effect == null)
        {
            Debug.LogWarning($"Failed to get effect instance from pool for ID: {effectId}");
            return null;
        }
        
        // Track which ID this effect instance belongs to
        _effectToIdMap[effect] = effectId;
        
        // Set parent if specified
        if (parent != null)
        {
            effect.transform.SetParent(parent);
        }
        
        // Apply effect data settings
        effectData.ApplySettingsToEffect(effect);
        
        // Apply quality settings
        if (_applyQualitySettings)
        {
            ApplyQualitySettings(effect, effectData);
        }
        
        // Apply global scale
        effect.transform.localScale *= _globalEffectScale;
        
        // Set callback for when effect completes
        effect.SetOnCompleteCallback(HandleEffectCompletion);
        
        // Add to active effects
        _activeEffects.Add(effect);
        _concurrentEffectCounts[effectId]++;
        
        // Play the effect
        effect.Play(position, rotation, parent);
        
        return effect;
    }
    
    public VisualEffect PlayEffect(VisualEffectData effectData, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (effectData == null) return null;
        return PlayEffect(effectData.EffectId, position, rotation, parent);
    }
    
    private void ApplyQualitySettings(VisualEffect effect, VisualEffectData effectData)
    {
        // Only apply on lower quality settings
        int qualityLevel = QualitySettings.GetQualityLevel();
        bool isLowQuality = qualityLevel < QualitySettings.names.Length / 2;
        
        if (!isLowQuality || !effectData.SimplifyOnLowQuality)
            return;
        
        ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particleSystems)
        {
            // 1. Adjust particle count
            var main = ps.main;
            main.maxParticles = Mathf.Max(1, Mathf.FloorToInt(main.maxParticles * effectData.ParticleCountScale));
            
            // 2. Simplify emission rate without changing particle size
            var emission = ps.emission;
            if (emission.enabled && emission.rateOverTime.mode == ParticleSystemCurveMode.Constant)
            {
                emission.rateOverTime = emission.rateOverTime.constant * effectData.ParticleCountScale;
            }
            
            // 3. Handle sub-emitters
            if (effectData.DisableSubEmittersOnLowQuality)
            {
                var subEmitters = ps.subEmitters;
                if (subEmitters.enabled && subEmitters.subEmittersCount > 0)
                {
                    subEmitters.enabled = false;
                }
            }
            
            // 4. Simplify collisions
            if (effectData.SimplifyCollisionsOnLowQuality)
            {
                var collision = ps.collision;
                if (collision.enabled)
                {
                    collision.quality = ParticleSystemCollisionQuality.Low;
                    collision.maxCollisionShapes = Mathf.Min(collision.maxCollisionShapes, 2);
                }
            }
            
            // 5. Reduce texture sheet animation frames
            var textureSheet = ps.textureSheetAnimation;
            if (textureSheet.enabled && textureSheet.numTilesX * textureSheet.numTilesY > 4)
            {
                // Use fewer animation frames on low-end devices
                textureSheet.numTilesX = Mathf.Max(1, textureSheet.numTilesX / 2);
                textureSheet.numTilesY = Mathf.Max(1, textureSheet.numTilesY / 2);
            }
            
            // 6. Disable velocity inheritance which can be costly
            var inheritVelocity = ps.inheritVelocity;
            if (inheritVelocity.enabled && qualityLevel < 1)
            {
                inheritVelocity.enabled = false;
            }
        }
    }
    
    public void StopEffect(VisualEffect effect)
    {
        if (effect == null || !_activeEffects.Contains(effect))
        {
            return;
        }
        
        effect.CompleteEffect();
    }
    
    public void StopAllEffects()
    {
        // Create a temporary copy to avoid collection modification issues
        var effectsToStop = new List<VisualEffect>(_activeEffects);
        
        foreach (var effect in effectsToStop)
        {
            effect.CompleteEffect();
        }
    }
    
    private void HandleEffectCompletion(VisualEffect effect)
    {
        Debug.Log($"HandleEffectCompletion called for effect: {effect.name}");

        if (_activeEffects.Contains(effect))
        {
            _activeEffects.Remove(effect);

            // Get the effect ID directly from our tracking dictionary
            string effectId = GetEffectIdForInstance(effect);
            if (!string.IsNullOrEmpty(effectId))
            {
                int oldcount = _concurrentEffectCounts[effectId];

                // Decrement the counter
                if (_concurrentEffectCounts.ContainsKey(effectId))
                {
                    _concurrentEffectCounts[effectId] = Mathf.Max(0, _concurrentEffectCounts[effectId] - 1);
                }

                Debug.Log(
                    $"Decremented count for effect '{effectId}' from {oldcount} to {_concurrentEffectCounts[effectId]}");

                // Remove from tracking dictionary
                _effectToIdMap.Remove(effect);
            }
            else
            {
                Debug.LogWarning($"Could not find effect ID for completed effect: {effect.name}");
            }

            // Find the correct pool for this effect
            foreach (var pool in _effectPools)
            {
                // Try to release to pool
                if (pool.Value.Release(effect))
                {
                    break;
                }
            }
        }
        else
        {
            Debug.LogWarning($"Effect {effect.name} was not found in active effects list");
        }
    }
    
    private string GetEffectIdForInstance(VisualEffect effect)
    {
        if (_effectToIdMap.TryGetValue(effect, out string effectId))
        {
            return effectId;
        }
        return string.Empty;
    }
    
    // Helper method to check if an effect ID exists
    public bool HasEffect(string effectId)
    {
        return _effectDataById.ContainsKey(effectId);
    }
    
    // Get reference to an effect data by ID
    public VisualEffectData GetEffectData(string effectId)
    {
        if (_effectDataById.TryGetValue(effectId, out var effectData))
        {
            return effectData;
        }
        return null;
    }
    
    // Runtime registration of new effects
    public bool RegisterEffect(VisualEffectData effectData)
    {
        if (effectData == null || string.IsNullOrEmpty(effectData.EffectId))
        {
            Debug.LogWarning("Cannot register null or invalid effect data");
            return false;
        }
        
        string effectId = effectData.EffectId;
        
        // Check if this effect is already registered
        if (_effectDataById.ContainsKey(effectId))
        {
            Debug.LogWarning($"Effect with ID {effectId} already registered");
            return false;
        }
        
        // Register the effect data
        _effectDataById[effectId] = effectData;
        _concurrentEffectCounts[effectId] = 0;
        
        // Create a pool for this effect
        if (effectData.EffectPrefab != null)
        {
            var pool = ObjectPoolManager.Instance.CreatePool(
                effectData.EffectPrefab,
                effectData.InitialPoolSize,
                transform
            );
            
            _effectPools[effectId] = pool;
        }
        
        // Add to the list for completeness
        _effectDataList.Add(effectData);
        
        return true;
    }
    
    public void CleanupAllEffects()
    {
        StopAllEffects();
        _effectToIdMap.Clear();
        
        foreach (var entry in _concurrentEffectCounts)
        {
            _concurrentEffectCounts[entry.Key] = 0;
        }
    }
}