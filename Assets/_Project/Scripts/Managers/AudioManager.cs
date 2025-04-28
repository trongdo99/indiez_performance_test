using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _musicSourceFadeOut;
    
    [Header("Music Clips")]
    [SerializeField] private AudioClip _menuMusic;
    [SerializeField] private AudioClip _gameplayMusic;
    [SerializeField] private AudioClip _bossMusic;
    
    [Header("Settings")]
    [SerializeField] private float _fadeInDuration = 1.0f;
    [SerializeField] private float _fadeOutDuration = 1.0f;
    [SerializeField] private float _musicVolume = 1.0f;
    
    private AudioClip _currentMusic;
    private bool _isFading;
    private Dictionary<string, AudioClip> _sceneToMusicMap = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        if (_musicSource == null)
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
        }
        
        if (_musicSourceFadeOut == null)
        {
            _musicSourceFadeOut = gameObject.AddComponent<AudioSource>();
            _musicSourceFadeOut.loop = true;
            _musicSourceFadeOut.playOnAwake = false;
        }
        
        InitializeSceneToMusicMapping();
    }
    
    private void OnEnable()
    {
        SceneLoader.Instance.OnSceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneLoader.Instance.OnSceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(string sceneName)
    {
        Debug.Log($"AudioManager: Scene loaded - {sceneName}");
        PlayMusicForScene(sceneName);
    }
    
    private void InitializeSceneToMusicMapping()
    {
        _sceneToMusicMap.Clear();
        _sceneToMusicMap.Add("MainMenu", _menuMusic);
        _sceneToMusicMap.Add("Level1", _gameplayMusic);
    }
    
    public void PlayMusicForScene(string sceneName)
    {
        if (_sceneToMusicMap.TryGetValue(sceneName, out AudioClip music))
        {
            PlayMusic(music);
        }
        else
        {
            if (sceneName.Contains("Level"))
            {
                PlayGameplayMusic();
            }
            else if (sceneName.Contains("Menu"))
            {
                PlayMenuMusic();
            }
        }
    }
    
    private void PlayMusicForCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        PlayMusicForScene(currentSceneName);
    }
    
    public void PlayMenuMusic()
    {
        PlayMusic(_menuMusic);
    }
    
    public void PlayGameplayMusic()
    {
        PlayMusic(_gameplayMusic);
    }
    
    public void PlayBossMusic()
    {
        if (_bossMusic != null)
        {
            PlayMusic(_bossMusic);
        }
    }
    
    public void PlayMusic(AudioClip clip)
    {
        if (_currentMusic == clip && _musicSource.isPlaying)
            return;
        
        _currentMusic = clip;
        
        if (clip == null)
        {
            StopMusic();
            return;
        }
        
        if (_isFading)
        {
            StopAllCoroutines();
            _isFading = false;
        }
        
        if (_musicSource.isPlaying)
        {
            StartCoroutine(CrossFadeMusic(clip));
        }
        else
        {
            StartCoroutine(FadeInMusic(clip));
        }
    }
    
    public void SetMusicVolume(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        _musicSource.volume = _musicVolume;
    }
    
    public void PauseMusic()
    {
        _musicSource.Pause();
        _musicSourceFadeOut.Pause();
    }
    
    public void ResumeMusic()
    {
        _musicSource.UnPause();
    }
    
    public void StopMusic()
    {
        StopAllCoroutines();
        _musicSource.Stop();
        _musicSourceFadeOut.Stop();
        _currentMusic = null;
        _isFading = false;
    }
    
    private IEnumerator FadeInMusic(AudioClip clip)
    {
        _isFading = true;
        
        _musicSource.clip = clip;
        _musicSource.volume = 0;
        _musicSource.Play();
        
        float timeElapsed = 0;
        
        while (timeElapsed < _fadeInDuration)
        {
            _musicSource.volume = Mathf.Lerp(0, _musicVolume, timeElapsed / _fadeInDuration);
            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        _musicSource.volume = _musicVolume;
        _isFading = false;
    }
    
    private IEnumerator CrossFadeMusic(AudioClip clip)
    {
        _isFading = true;
        
        AudioSource fadeSource = _musicSource;
        AudioSource newSource = _musicSourceFadeOut;
        
        newSource.clip = clip;
        newSource.volume = 0;
        newSource.Play();
        
        float timeElapsed = 0;
        
        while (timeElapsed < _fadeOutDuration)
        {
            float t = timeElapsed / _fadeOutDuration;
            
            fadeSource.volume = Mathf.Lerp(_musicVolume, 0, t);
            
            newSource.volume = Mathf.Lerp(0, _musicVolume, t);
            
            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        fadeSource.volume = 0;
        newSource.volume = _musicVolume;
        
        fadeSource.Stop();
        
        _musicSource = newSource;
        _musicSourceFadeOut = fadeSource;
        _isFading = false;
    }
}