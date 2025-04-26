using System.Collections;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] protected Transform _bulletSpawnPosition;
    [SerializeField] protected TrailRenderer _bulletTrail;
    [SerializeField] protected float _damageToHealth = -100f;
    [SerializeField] protected float _shootDelay = 1f;
    [SerializeField] protected float _speed = 100f;
    [SerializeField] protected float _missDistance = 100f;
    [SerializeField] protected Vector2 _spread = Vector2.zero;
    [SerializeField] protected LayerMask _collidableLayerMask;
    
    [Header("Visual Effects")]
    [SerializeField] protected VisualEffectData _muzzleFlashEffect;
    [SerializeField] protected Transform _muzzleFlashPosition;
    [SerializeField] protected VisualEffectData _impactEffect;
    [SerializeField] protected bool _useImpactEffects = true;

    protected float _lastShootTime;

    public abstract void TryToShoot();

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
    
    // Play muzzle flash effect
    protected void PlayMuzzleEffect()
    {
        VisualEffectManager.Instance.PlayEffect(
            _muzzleFlashEffect,
            _muzzleFlashPosition.position,
            _muzzleFlashPosition.rotation,
            transform
        );
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