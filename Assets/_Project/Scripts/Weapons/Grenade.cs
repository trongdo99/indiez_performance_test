using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

public class Grenade : MonoBehaviour
{
    [Header("Grenade Settings")]
    [SerializeField] private float _detonationTime = 3f;
    [SerializeField] private float _explosionRadius = 5f;
    [SerializeField] private float _explosionDamage = 100f;
    [SerializeField] private float _explosionForce = 500f;
    [SerializeField] private LayerMask _damageLayers;
    [SerializeField] private VisualEffectData _explosionEffect;
    [SerializeField] private SoundEffectData _explosionSoundEffectData;
    
    [Header("Camera Shake")]
    [SerializeField] protected CinemachineImpulseSource _impulseSource;
    [SerializeField] protected CinemachineImpulseDefinition.ImpulseShapes _impulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion;
    [SerializeField] [Range(0.1f, 3f)] protected float _impulseAmplitude = 1f;
    [SerializeField] [Range(0.1f, 3f)] protected float _impulseFrequency = 1f;
    [SerializeField] [Range(0.05f, 1f)] protected float _impulseDuration = 0.2f;
    [SerializeField] protected Vector3 _impulseDirection = new Vector3(0f, -1f, 0f);

    [Header("Explosion Lightning")]
    [SerializeField] private bool _useExplosionLightning = true;
    [SerializeField] private ExplosionLightSettings _explosionLightSettings;
    
    private Rigidbody _rigidbody;
    private ExplosionLightController _explosionLightController;
    private bool _hasDetonated;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        
        SetupCameraShake();
        SetupExplosionLightController();
    }

    private void SetupCameraShake()
    {
        if (_impulseSource == null)
        {
            _impulseSource = GetComponent<CinemachineImpulseSource>();
            if (_impulseSource == null)
            {
                _impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            }
        }

        _impulseSource.ImpulseDefinition.ImpulseShape = _impulseShape;
        _impulseSource.ImpulseDefinition.AmplitudeGain = _impulseAmplitude;
        _impulseSource.ImpulseDefinition.FrequencyGain = _impulseFrequency;
        _impulseSource.ImpulseDefinition.ImpulseDuration = _impulseDuration;
        _impulseSource.DefaultVelocity = _impulseDirection.normalized;
    }

    private void SetupExplosionLightController()
    {
        if (_explosionLightController != null) return;
        _explosionLightController = GetComponent<ExplosionLightController>();
    }

    public void Initialize(Vector3 initialVelocity)
    {
        _rigidbody.linearVelocity = initialVelocity;
        _rigidbody.AddTorque(Random.insideUnitSphere * 5f, ForceMode.VelocityChange);

        StartCoroutine(DetonationSequence());
    }

    private IEnumerator DetonationSequence()
    {
        var elapsedTime = 0f;
        while (elapsedTime < _detonationTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Detonate();
    }

    private void Detonate()
    {
        if (_hasDetonated) return;
        _hasDetonated = true;
        
        PlayImpulseEffect();
        
        SoundEffectManager.Instance.PlaySound(_explosionSoundEffectData, transform.position);

        VisualEffectManager.Instance.PlayEffect(_explosionEffect, transform.position, Quaternion.identity);

        if (_useExplosionLightning && _explosionLightController != null)
        {
            _explosionLightController.TriggerExplosion();
        }
        
        ApplyExplosionDamageAndForce();
        
        MakeInvisible();
        
        Destroy(gameObject, Mathf.Max(_explosionLightSettings.Duration, 2f));
    }

    private void ApplyExplosionDamageAndForce()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _explosionRadius, _damageLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider hitCollider in hitColliders)
        {
            float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
            float damageMultiplier = 1 - (distance / _explosionDamage);
            float damage = _explosionDamage * damageMultiplier;
            
            if (hitCollider.TryGetComponent(out Rigidbody rigidbody) && hitCollider.TryGetComponent(out ZombieController zombieController))
            {
                zombieController.EnableRagDoll(2f);
                rigidbody.AddExplosionForce(_explosionForce, transform.position, _explosionRadius, 1f, ForceMode.Impulse);
            }
            
            if (hitCollider.TryGetComponent(out Health health))
            {
                health.TryChangeHealth(-damage);
            }
        }
    }

    private void MakeInvisible()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        Collider collider = GetComponent<Collider>();
        collider.enabled = false;

        _rigidbody.isKinematic = true;
        
    }

    private void PlayImpulseEffect()
    {
        if (_impulseSource == null) return;
        
        _impulseSource.GenerateImpulse();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}
