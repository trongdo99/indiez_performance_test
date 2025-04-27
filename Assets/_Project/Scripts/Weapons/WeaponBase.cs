using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] protected WeaponType _weaponType;
    [SerializeField] protected float _damageToHealth = -100f;
    [SerializeField] protected float _shootDelay = 1f;
    [SerializeField] protected float _speed = 100f;
    [SerializeField] protected float _missDistance = 100f;
    [SerializeField] protected Vector2 _spread = Vector2.zero;
    [SerializeField] protected LayerMask _collidableLayerMask;
    
    [Header("Camera Shake")]
    [SerializeField] protected CinemachineImpulseSource _impulseSource;
    [SerializeField] protected CinemachineImpulseDefinition.ImpulseShapes _impulseShape = CinemachineImpulseDefinition.ImpulseShapes.Recoil;
    [SerializeField] [Range(0.1f, 3f)] protected float _impulseAmplitude = 1f;
    [SerializeField] [Range(0.1f, 3f)] protected float _impulseFrequency = 1f;
    [SerializeField] [Range(0.05f, 1f)] protected float _impulseDuration = 0.2f;
    [SerializeField] protected Vector3 _impulseDirection = new Vector3(0f, -1f, 0f);
    
    [Header("Projectile Effects")]
    [SerializeField] protected TrailRenderer _bulletTrail;
    [SerializeField] protected Transform _bulletSpawnPosition;
    
    [Header("Visual Effects")]
    [SerializeField] protected VisualEffectData _muzzleFlashEffect;
    [SerializeField] protected Transform _muzzleFlashPosition;
    [SerializeField] protected VisualEffectData _impactEffect;
    [SerializeField] protected bool _useImpactEffects = true;
    
    [Header("Sound Effect")]
    [SerializeField] protected List<SoundEffectData> _gunShotSoundEffects;
    [SerializeField] protected bool _randomizePitch = true;
    [SerializeField] protected Vector2 _pitchRange = new Vector2(0.9f, 1.1f);

    [Header("IK")]
    [SerializeField] protected Transform _leftHandAttachTransform;
    [SerializeField] protected Transform _leftHandHintTransform;

    protected float _lastShootTime;

    public WeaponType WeaponType => _weaponType;
    public Transform LeftHandAttachTransform => _leftHandAttachTransform;
    public Transform LeftHandHinTransform => _leftHandHintTransform;

    protected virtual void Awake()
    {
        SetupCameraShake();
    }

    public abstract bool TryToShoot();

    public void Equip()
    {
        gameObject.SetActive(true);
    }

    public void Unequip()
    {
        gameObject.SetActive(false);
    }

    protected void SetupCameraShake()
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

    // This method fires a single projectile without playing muzzle effect
    protected virtual void FireProjectile(Vector2 spreadOverride = default)
    {
        Vector2 finalSpread = spreadOverride == default ? _spread : spreadOverride;
        Vector3 shootDirection = _bulletSpawnPosition.forward + new Vector3(
            Random.Range(-finalSpread.x, finalSpread.x), 
            Random.Range(-finalSpread.y, finalSpread.y), 
            0f
        );
        
        TrailRenderer trail = Instantiate(_bulletTrail, _bulletSpawnPosition.position, Quaternion.identity);

        if (Physics.Raycast(_bulletSpawnPosition.position, shootDirection, out RaycastHit hit, Mathf.Infinity, _collidableLayerMask, QueryTriggerInteraction.Ignore))
        {
            StartCoroutine(SpawnTrail(trail, hit.point, hit.normal, hit, true));
        }
        else
        {
            StartCoroutine(SpawnTrail(trail, _bulletSpawnPosition.position + shootDirection * _missDistance, Vector3.zero, hit,
                false));
        }
    }

    protected void PlayGunShotSoundEffect()
    {
        SoundEffect soundEffect = SoundEffectManager.Instance.PlayRandomSound(_gunShotSoundEffects, _muzzleFlashPosition.position, transform);
        
        if (soundEffect != null && _randomizePitch)
        {
            float randomPitch = Random.Range(_pitchRange.x, _pitchRange.y);
            soundEffect.SetPitch(randomPitch);
        }
    }
    
    protected void PlayMuzzleEffect()
    {
        VisualEffectManager.Instance.PlayEffect(
            _muzzleFlashEffect,
            _muzzleFlashPosition.position,
            _muzzleFlashPosition.rotation,
            transform
        );
    }

    protected void PlayImpulseEffect()
    {
        if (_impulseSource == null) return;
        
        _impulseSource.GenerateImpulse();
    }

    protected IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, RaycastHit hit, bool impact)
    {
        Vector3 startPosition = trail.transform.position;
        
        float distance = Vector3.Distance(startPosition, hitPoint);
        float startingDistance = distance;

        while (distance > 0f)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, 1 - (distance / startingDistance));
            distance -= Time.deltaTime * _speed;

            yield return null;
        }
        
        trail.transform.position = hitPoint;

        // The gameobject may be died before impact, hence collider will be null
        if (impact && hit.collider != null)
        {
            // Play impact effect using VisualEffectManager
            if (_useImpactEffects)
            {
                VisualEffectManager.Instance.PlayEffect(
                    _impactEffect,
                    hitPoint,
                    Quaternion.FromToRotation(Vector3.up, hitNormal),
                    hit.transform
                );
            }
            
            if (hit.transform.TryGetComponent(out Health health))
            {
                Debug.Log($"Hit: {hit.transform.name}");
                health.TryChangeHealth(_damageToHealth);
            }
        }
        
        Destroy(trail.gameObject, trail.time);
    }
}