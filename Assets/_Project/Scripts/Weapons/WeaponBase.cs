using System.Collections;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] protected Transform _bulletSpawnPosition;
    [SerializeField] protected TrailRenderer _bulletTrail;
    [SerializeField] protected ParticleSystem _muzzleFlash;
    [SerializeField] protected float _damageToHealth = -100f;
    [SerializeField] protected float _shootDelay = 1f;
    [SerializeField] protected float _speed = 100f;
    [SerializeField] protected float _missDistance = 100f;
    [SerializeField] protected Vector2 _spread = Vector2.zero;
    [SerializeField] protected LayerMask _collidableLayerMask;

    protected float _lastShootTime;

    public abstract void TryToShoot();

    protected virtual void Shoot()
    {
        Vector3 shootDirection = _bulletSpawnPosition.forward + new Vector3(Random.Range(-_spread.x, _spread.x), Random.Range(-_spread.y, _spread.y), 0f);
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
            if (hit.transform.TryGetComponent(out Health health))
            {
                Debug.Log($"Hit: {hit.transform.name}");
                health.TryChangeHealth(_damageToHealth);
            }
        }
        
        Destroy(trail.gameObject, trail.time);
    }
}
