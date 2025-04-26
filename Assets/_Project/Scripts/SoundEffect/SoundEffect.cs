using System;
using UnityEngine;

public class SoundEffect : MonoBehaviour, IPoolable
{
    private AudioSource _audioSource;
    private Action<SoundEffect> _onSoundComplete;
    private bool _isPlaying;
    private float _duration;
    private float _timer;
    
    public AudioSource AudioSource => _audioSource;
    
    private void Awake()
    {
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void Update()
    {
        if (!_isPlaying || _audioSource.loop) return;
        
        _timer += Time.deltaTime;
        
        // Check if the sound has finished playing
        if (_timer >= _duration || !_audioSource.isPlaying)
        {
            CompleteSound();
        }
    }
    
    public void Play(Vector3 position, Transform parent = null)
    {
        transform.position = position;
        
        _isPlaying = true;
        _timer = 0f;
        
        if (_audioSource.clip != null)
        {
            _duration = _audioSource.clip.length;
            _audioSource.Play();
        }
        else
        {
            Debug.LogWarning("Attempted to play SoundEffect with no AudioClip assigned", this);
            CompleteSound();
        }
    }
    
    public void SetOnCompleteCallback(Action<SoundEffect> callback)
    {
        _onSoundComplete = callback;
    }
    
    public void CompleteSound()
    {
        if (!_isPlaying) return;
        
        _isPlaying = false;
        _audioSource.Stop();
        _onSoundComplete?.Invoke(this);
    }
    
    public void SetVolume(float volume)
    {
        if (_audioSource != null)
        {
            _audioSource.volume = volume;
        }
    }
    
    public void SetPitch(float pitch)
    {
        if (_audioSource != null)
        {
            _audioSource.pitch = pitch;
        }
    }
    
    public void OnGetFromPool()
    {
        _isPlaying = false;
        _timer = 0f;
        gameObject.SetActive(true);
    }
    
    public void OnReleaseToPool()
    {
        if (_isPlaying)
        {
            CompleteSound();
        }
        
        _isPlaying = false;
        _timer = 0f;
        _audioSource.Stop();
        transform.SetParent(null);
        gameObject.SetActive(false);
    }
}