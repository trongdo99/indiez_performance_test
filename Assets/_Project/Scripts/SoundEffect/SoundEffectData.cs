using UnityEngine;

[CreateAssetMenu(fileName = "New Sound Effect", menuName = "Game/Audio/Sound Effect Data")]
public class SoundEffectData : ScriptableObject
{
    [Header("Sound Properties")]
    [SerializeField] private string _soundId;
    [SerializeField] private SoundEffect _soundEffectPrefab;
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private int _initialPoolSize = 5;
    
    [Header("Playback Settings")]
    [SerializeField] [Range(0f, 1f)] private float _volume = 1f;
    [SerializeField] [Range(0.1f, 3f)] private float _pitch = 1f;
    [SerializeField] [Range(0f, 1f)] private float _spatialBlend = 0f; // 0 = 2D, 1 = 3D
    [SerializeField] private bool _loop = false;
    [SerializeField] private float _minDistance = 1f;
    [SerializeField] private float _maxDistance = 500f;
    
    [Header("Performance Settings")]
    [SerializeField] private int _maxConcurrentInstances = 10;
    [SerializeField] private bool _preventDuplicates = false;
    [SerializeField] private float _duplicateTimeout = 0.1f;
    
    public string SoundId => _soundId;
    public SoundEffect SoundEffectPrefab => _soundEffectPrefab;
    public AudioClip AudioClip => _audioClip;
    public int InitialPoolSize => _initialPoolSize;
    public float Volume => _volume;
    public float Pitch => _pitch;
    public float SpatialBlend => _spatialBlend;
    public bool Loop => _loop;
    public float MinDistance => _minDistance;
    public float MaxDistance => _maxDistance;
    public int MaxConcurrentInstances => _maxConcurrentInstances;
    public bool PreventDuplicates => _preventDuplicates;
    public float DuplicateTimeout => _duplicateTimeout;
    
    [ContextMenu("Generate ID from name")]
    private void GenerateIdFromName()
    {
        if (string.IsNullOrEmpty(name)) return;
        
        _soundId = name.Replace(" ", "").Trim();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
    
    public void ApplySettingsToAudioSource(AudioSource audioSource)
    {
        if (audioSource == null) return;
        
        audioSource.clip = _audioClip;
        audioSource.volume = _volume;
        audioSource.pitch = _pitch;
        audioSource.spatialBlend = _spatialBlend;
        audioSource.loop = _loop;
        audioSource.minDistance = _minDistance;
        audioSource.maxDistance = _maxDistance;
    }
    
    public void ApplySettingsToSoundEffect(SoundEffect soundEffect)
    {
        if (soundEffect == null) return;
        
        ApplySettingsToAudioSource(soundEffect.AudioSource);
    }
}