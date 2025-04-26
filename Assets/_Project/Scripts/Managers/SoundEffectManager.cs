using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SoundEffectManager : MonoBehaviour, ISyncInitializable
{
    public static SoundEffectManager Instance { get; private set; }
    
    [SerializeField] private List<SoundEffectData> _soundEffects = new List<SoundEffectData>();
    [SerializeField] private float _masterVolume = 1.0f;
    [SerializeField] private float _sfxVolume = 1.0f;
    
    private readonly Dictionary<string, SoundEffectData> _soundDataById = new Dictionary<string, SoundEffectData>();
    private readonly Dictionary<string, ObjectPool<SoundEffect>> _soundPools = new Dictionary<string, ObjectPool<SoundEffect>>();
    private readonly List<SoundEffect> _activeSounds = new List<SoundEffect>();
    private readonly Dictionary<string, int> _concurrentSoundCounts = new Dictionary<string, int>();
    private readonly Dictionary<string, float> _lastPlayTimes = new Dictionary<string, float>();
    private readonly Dictionary<SoundEffect, string> _soundToIdMap = new Dictionary<SoundEffect, string>();
    
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
        InitializeSoundDatabase();
        InitializePools();
    }

    private void InitializeSoundDatabase()
    {
        // Clear existing data
        _soundDataById.Clear();
        
        // Add all sound data to the dictionary
        foreach (SoundEffectData soundData in _soundEffects)
        {
            if (soundData == null) continue;
            
            string soundId = soundData.SoundId;
            
            if (string.IsNullOrEmpty(soundId))
            {
                Debug.LogWarning($"Sound effect has empty ID: {soundData.name}", soundData);
                continue;
            }
            
            if (_soundDataById.ContainsKey(soundId))
            {
                Debug.LogWarning($"Duplicate sound effect ID: {soundId}", soundData);
                continue;
            }
            
            _soundDataById[soundId] = soundData;
            _concurrentSoundCounts[soundId] = 0;
            _lastPlayTimes[soundId] = -1000f;
        }
    }

    private void InitializePools()
    {
        // Create pools for each sound effect
        foreach (KeyValuePair<string, SoundEffectData> soundEntry in _soundDataById)
        {
            string soundId = soundEntry.Key;
            SoundEffectData soundData = soundEntry.Value;
            
            if (soundData.SoundEffectPrefab == null)
            {
                Debug.LogWarning($"Sound effect prefab not assigned for ID: {soundId}");
                continue;
            }
            
            var pool = ObjectPoolManager.Instance.CreatePool(
                soundData.SoundEffectPrefab, 
                soundData.InitialPoolSize,
                transform
            );
            
            _soundPools[soundId] = pool;
        }
    }
    
    public SoundEffect PlaySound(string soundId, Vector3 position, Transform parent = null)
    {
        if (!_soundDataById.TryGetValue(soundId, out SoundEffectData soundData))
        {
            Debug.LogWarning($"Sound effect not found with ID: {soundId}");
            return null;
        }
        
        // Check for duplicate prevention
        if (soundData.PreventDuplicates)
        {
            float timeSinceLastPlay = Time.time - _lastPlayTimes[soundId];
            if (timeSinceLastPlay < soundData.DuplicateTimeout)
            {
                return null;
            }
        }
        
        // Check max concurrent instances
        if (_concurrentSoundCounts.TryGetValue(soundId, out int count))
        {
            if (count >= soundData.MaxConcurrentInstances)
            {
                return null;
            }
        }
        
        if (!_soundPools.TryGetValue(soundId, out ObjectPool<SoundEffect> pool))
        {
            Debug.LogWarning($"Sound pool not found for ID: {soundId}");
            return null;
        }
        
        // Get a sound effect from the pool
        SoundEffect sound = pool.Get(position);
        
        if (sound == null)
        {
            Debug.LogWarning($"Failed to get sound instance from pool for ID: {soundId}");
            return null;
        }
        
        // Track which ID this sound instance belongs to
        _soundToIdMap[sound] = soundId;
        
        // Update the concurrent count
        _concurrentSoundCounts[soundId]++;
        _lastPlayTimes[soundId] = Time.time;
        
        // Apply sound effect data settings
        soundData.ApplySettingsToSoundEffect(sound);
        
        // Set completion callback
        sound.SetOnCompleteCallback(HandleSoundCompletion);
        
        // Apply current volume settings
        sound.SetVolume(soundData.Volume * _sfxVolume * _masterVolume);
        
        // Add to active sounds
        _activeSounds.Add(sound);
        
        // Set parent if specified
        if (parent != null)
        {
            sound.transform.SetParent(parent);
        }
        
        // Play the sound
        sound.Play(position, parent);
        
        return sound;
    }
    
    public SoundEffect PlaySound(SoundEffectData soundData, Vector3 position, Transform parent = null)
    {
        if (soundData == null)
        {
            Debug.LogWarning("Attempted to play null SoundEffectData");
            return null;
        }
        
        return PlaySound(soundData.SoundId, position, parent);
    }
    
    public SoundEffect PlayRandomSound(string[] soundIds, Vector3 position, Transform parent = null)
    {
        if (soundIds == null || soundIds.Length == 0) return null;
        
        string randomSoundId = soundIds[Random.Range(0, soundIds.Length)];
        return PlaySound(randomSoundId, position, parent);
    }

    public SoundEffect PlayRandomSound(List<SoundEffectData> soundDatas, Vector3 position, Transform parent = null)
    {
        if (soundDatas == null || soundDatas.Count == 0) return null;
        
        int randomIndex = Random.Range(0, soundDatas.Count);
        SoundEffectData randomSoundData = soundDatas[randomIndex];
        return PlaySound(randomSoundData, position, parent);
    }
    
    private void HandleSoundCompletion(SoundEffect sound)
    {
        if (_activeSounds.Contains(sound))
        {
            _activeSounds.Remove(sound);
            
            // Get the sound ID directly from the tracking dictionary
            if (_soundToIdMap.TryGetValue(sound, out string soundId))
            {
                // Decrement the counter
                if (_concurrentSoundCounts.ContainsKey(soundId))
                {
                    _concurrentSoundCounts[soundId] = Mathf.Max(0, _concurrentSoundCounts[soundId] - 1);
                }
                
                // Remove from tracking dictionary
                _soundToIdMap.Remove(sound);
            }
            
            // Find the correct pool for this sound
            foreach (var pool in _soundPools)
            {
                // Try to release to pool
                if (pool.Value.Release(sound))
                {
                    break;
                }
            }
        }
    }
    
    public void StopSound(SoundEffect sound)
    {
        if (sound == null || !_activeSounds.Contains(sound))
        {
            return;
        }
        
        sound.CompleteSound();
    }
    
    public void StopAllSounds()
    {
        // Create a temporary copy to avoid collection modification issues
        var soundsToStop = new List<SoundEffect>(_activeSounds);
        
        foreach (var sound in soundsToStop)
        {
            sound.CompleteSound();
        }
    }
    
    public void SetMasterVolume(float volume)
    {
        _masterVolume = Mathf.Clamp01(volume);
        UpdateAllActiveSoundVolumes();
    }
    
    public void SetSfxVolume(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        UpdateAllActiveSoundVolumes();
    }
    
    private void UpdateAllActiveSoundVolumes()
    {
        foreach (var sound in _activeSounds)
        {
            if (_soundToIdMap.TryGetValue(sound, out string soundId) && 
                _soundDataById.TryGetValue(soundId, out SoundEffectData data))
            {
                sound.SetVolume(data.Volume * _sfxVolume * _masterVolume);
            }
        }
    }
    
    // Helper method to check if a sound ID exists
    public bool HasSound(string soundId)
    {
        return _soundDataById.ContainsKey(soundId);
    }
    
    // Get reference to a sound data by ID
    public SoundEffectData GetSoundData(string soundId)
    {
        if (_soundDataById.TryGetValue(soundId, out var soundData))
        {
            return soundData;
        }
        return null;
    }
    
    // Runtime registration of new sounds
    public bool RegisterSound(SoundEffectData soundData)
    {
        if (soundData == null || string.IsNullOrEmpty(soundData.SoundId))
        {
            Debug.LogWarning("Cannot register null or invalid sound data");
            return false;
        }
        
        string soundId = soundData.SoundId;
        
        // Check if this sound is already registered
        if (_soundDataById.ContainsKey(soundId))
        {
            Debug.LogWarning($"Sound with ID {soundId} already registered");
            return false;
        }
        
        // Register the sound data
        _soundDataById[soundId] = soundData;
        _concurrentSoundCounts[soundId] = 0;
        _lastPlayTimes[soundId] = -1000f;
        
        // Create a pool for this sound
        if (soundData.SoundEffectPrefab != null)
        {
            var pool = ObjectPoolManager.Instance.CreatePool(
                soundData.SoundEffectPrefab,
                soundData.InitialPoolSize,
                transform
            );
            
            _soundPools[soundId] = pool;
        }
        
        // Add to the list for completeness
        _soundEffects.Add(soundData);
        
        return true;
    }
    
    // For debugging
    public int GetActiveCount()
    {
        return _activeSounds.Count;
    }
    
    public int GetConcurrentCount(string soundId)
    {
        if (_concurrentSoundCounts.TryGetValue(soundId, out int count))
        {
            return count;
        }
        return 0;
    }
    
    public void CleanupAllSounds()
    {
        StopAllSounds();
        _soundToIdMap.Clear();
        
        foreach (var entry in _concurrentSoundCounts)
        {
            _concurrentSoundCounts[entry.Key] = 0;
        }
    }
}