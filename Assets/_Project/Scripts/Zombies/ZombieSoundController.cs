using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class ZombieSoundSettings
{
    [Header("Sound Assets")]
    public List<SoundEffectData> MoanSounds;
    public List<SoundEffectData> AttackSounds;
    public List<SoundEffectData> DeathSounds;
    
    [Header("Moaning Timing")]
    public float MinMoanInterval = 5f;
    public float MaxMoanInterval = 12f;
    
    [Header("Probability")]
    [Range(0f, 1f)]
    public float MoanChance = 0.4f;
    
    [Header("Pitch Variation")]
    public bool RandomizePitch = true;
    public Vector2 PitchRange = new Vector2(0.8f, 1.2f);
}

public class ZombieSoundController : MonoBehaviour
{
    [SerializeField] private ZombieSoundSettings _soundSettings;
    
    [Header("Sound Emission")]
    [SerializeField] private Transform _soundEmissionPoint;
    [SerializeField] private float _distanceToStartMoaning = 30f;
    [SerializeField] private float _guaranteedSoundRange = 10f;
    
    private ZombieController _zombieController;
    private bool _isDead;
    private bool _hasActiveMoaningSound;
    private Coroutine _moanRoutine;
    private Transform _playerTransform;
    private float _distanceToPlayer = Mathf.Infinity;
    
    private static int _activeMoaningSources;
    private static int _maxConcurrentMoaningSources = 3; // Maximum zombies moaning at once
    
    private void Awake()
    {
        if (_soundEmissionPoint == null)
            _soundEmissionPoint = transform;
        _zombieController = GetComponent<ZombieController>();
    }

    private void OnDestroy()
    {
        if (_hasActiveMoaningSound)
        {
            _activeMoaningSources = Mathf.Max(0, _activeMoaningSources - 1);
        }
    }

    private void Start()
    {
        _playerTransform = _zombieController.Target;

        if (_moanRoutine == null)
        {
            _moanRoutine = StartCoroutine(MoanRoutine(Random.Range(0.5f, 5f)));
        }
    }
    
    private void Update()
    {
        if (_isDead || _playerTransform == null) return;
        
        _distanceToPlayer = Vector3.Distance(_soundEmissionPoint.position, _playerTransform.position);
    }
    
    private IEnumerator MoanRoutine(float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);
        
        while (!_isDead)
        {
            bool shouldTryMoan = ShouldZombieMoan();
            
            if (shouldTryMoan && _soundSettings.MoanSounds.Count > 0)
            {
                PlayMoanSound();
            }
            
            float nextInterval = Random.Range(_soundSettings.MinMoanInterval, _soundSettings.MaxMoanInterval);
            
            yield return new WaitForSeconds(nextInterval);
        }
    }
    
    private bool ShouldZombieMoan()
    {
        if (_distanceToPlayer > _distanceToStartMoaning)
            return false;
            
        float chance = _soundSettings.MoanChance;
        // Increase the chance the closer the zombie to the player
        if (_distanceToPlayer <= _guaranteedSoundRange)
        {
            chance = Mathf.Lerp(chance, 1f, 1f - (_distanceToPlayer / _guaranteedSoundRange));
        }
        
        if (_activeMoaningSources >= _maxConcurrentMoaningSources)
        {
            chance *= 0.15f;
        }
        
        return Random.value < chance;
    }
    
    private void PlayMoanSound()
    {
        if (_soundSettings.MoanSounds == null || _soundSettings.MoanSounds.Count == 0)
            return;
            
        SoundEffect soundEffect = SoundEffectManager.Instance.PlayRandomSound(_soundSettings.MoanSounds, _soundEmissionPoint.position);
        
        if (soundEffect != null)
        {
            if (_soundSettings.RandomizePitch)
            {
                float randomPitch = Random.Range(_soundSettings.PitchRange.x, _soundSettings.PitchRange.y);
                soundEffect.SetPitch(randomPitch);
            }
            
            _activeMoaningSources++;
            _hasActiveMoaningSound = true;

            Action<SoundEffect> originalCallback = soundEffect.GetOnCompleteCallback();
            Action<SoundEffect> chainedCallback = (effect) =>
            {
                OnSoundComplete(effect);
                
                originalCallback?.Invoke(effect);
            };
            soundEffect.SetOnCompleteCallback(chainedCallback);
        }
    }
    
    private void OnSoundComplete(SoundEffect soundEffect)
    {
        _activeMoaningSources = Mathf.Max(0, _activeMoaningSources - 1);
        _hasActiveMoaningSound = false;
    }
    
    public void PlayAttackSound()
    {
        if (!_isDead || _soundSettings.AttackSounds == null || _soundSettings.AttackSounds.Count == 0) return;
        
        SoundEffect soundEffect = SoundEffectManager.Instance.PlayRandomSound(_soundSettings.AttackSounds, _soundEmissionPoint.position);
        
        if (soundEffect != null && _soundSettings.RandomizePitch)
        {
            float randomPitch = Random.Range(_soundSettings.PitchRange.x, _soundSettings.PitchRange.y);
            soundEffect.SetPitch(randomPitch);
        }
    }
    
    public void PlayDeathSound()
    {
        if (!_isDead || _soundSettings.DeathSounds == null || _soundSettings.DeathSounds.Count == 0) return;
        
        _isDead = true;
        
        if (_moanRoutine != null)
        {
            StopCoroutine(_moanRoutine);
            _moanRoutine = null;
        }
        
        SoundEffect soundEffect = SoundEffectManager.Instance.PlayRandomSound(_soundSettings.DeathSounds, _soundEmissionPoint.position);
        
        if (soundEffect != null && _soundSettings.RandomizePitch)
        {
            float randomPitch = Random.Range(_soundSettings.PitchRange.x, _soundSettings.PitchRange.y);
            soundEffect.SetPitch(randomPitch);
        }
    }

    public void Reset()
    {
        if (_moanRoutine != null)
        {
            StopCoroutine(_moanRoutine);
            _moanRoutine = null;
        }
        
        _isDead = false;
        
        _moanRoutine = StartCoroutine(MoanRoutine(Random.Range(0.5f, 5f)));
    }

    public void StopMoaning()
    {
        if (_moanRoutine != null)
        {
            StopCoroutine(_moanRoutine);
            _moanRoutine = null;
        }

        if (_hasActiveMoaningSound)
        {
            _activeMoaningSources = Mathf.Max(0, _activeMoaningSources - 1);
            _hasActiveMoaningSound = false;
        }
        
        _isDead = false;
    }
}