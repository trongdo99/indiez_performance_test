using UnityEngine;

[CreateAssetMenu(fileName = "New Visual Effect", menuName = "Game/Visual Effects/Effect Data")]
public class VisualEffectData : ScriptableObject
{
    [Header("Effect Properties")]
    [SerializeField] private string _effectId;
    [SerializeField] private VisualEffect _effectPrefab;
    [SerializeField] private int _initialPoolSize = 5;
    
    [Header("Effect Settings")]
    [SerializeField] private bool _looping = false;
    [SerializeField] private float _lifetime = 2f;
    [SerializeField] private float _scale = 1f;
    [SerializeField] private bool _useParticleSystemLifetime = true;
    [SerializeField] private bool _autoDeactivate = true;
    
    [Header("Performance Settings")]
    [SerializeField] private int _maxConcurrentInstances = 10;
    [SerializeField] private bool _simplifyOnLowQuality = true;
    [SerializeField] [Range(0.2f, 1f)] private float _particleCountScale = 0.7f;
    [SerializeField] private bool _disableSubEmittersOnLowQuality = true; 
    [SerializeField] private bool _simplifyCollisionsOnLowQuality = true;
    
    public string EffectId => _effectId;
    public VisualEffect EffectPrefab => _effectPrefab;
    public int InitialPoolSize => _initialPoolSize;
    public bool Looping => _looping;
    public float Lifetime => _lifetime;
    public float Scale => _scale;
    public bool UseParticleSystemLifetime => _useParticleSystemLifetime;
    public bool AutoDeactivate => _autoDeactivate;
    public int MaxConcurrentInstances => _maxConcurrentInstances;
    public bool SimplifyOnLowQuality => _simplifyOnLowQuality;
    public float ParticleCountScale => _particleCountScale;
    public bool DisableSubEmittersOnLowQuality => _disableSubEmittersOnLowQuality;
    public bool SimplifyCollisionsOnLowQuality => _simplifyCollisionsOnLowQuality;
    
    
    [ContextMenu("Generate ID from name")]
    private void GenerateIdFromName()
    {
        if (string.IsNullOrEmpty(name)) return;
        
        _effectId = name.Replace(" ", "").Trim();
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    public void ApplySettingsToEffect(VisualEffect effect)
    {
        if (effect == null) return;
        
        // First set whether to use particle system lifetime
        effect.SetUseParticleSystemLifetime(_useParticleSystemLifetime);
        
        // Only set the lifetime if we're not using the particle system's lifetime
        if (!_useParticleSystemLifetime)
        {
            effect.SetLifetime(_lifetime);
        }
        
        // Apply scale
        effect.transform.localScale = Vector3.one * _scale;
        
        // Apply looping setting
        effect.SetLooping(_looping);
        
        // Apply auto deactivate setting
        effect.SetAutoDeactivate(_autoDeactivate);
    }
}