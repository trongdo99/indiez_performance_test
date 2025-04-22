using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private Transform _bulletSpawnPosition;
    [SerializeField] private TrailRenderer _bulletTrail;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private float _shootDelay = 1f;
    [SerializeField] private float _speed = 100f;
    [SerializeField] private LayerMask _targetableLayerMask;

    private float _lastShootTime;

    public void Shoot()
    {
        if (_lastShootTime + _shootDelay < Time.time)
        {
            _muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _muzzleFlash.Play();
            
            Vector3 direction = transform.forward;
            TrailRenderer trail = Instantiate(_bulletTrail, _bulletSpawnPosition.position, Quaternion.identity);

            if (Physics.Raycast(_bulletSpawnPosition.position, direction, out RaycastHit hit, _targetableLayerMask))
            {
                StartCoroutine(SpawnTrail(trail, hit.point, hit.normal, true));
            }
            else
            {
                StartCoroutine(SpawnTrail(trail, _bulletSpawnPosition.position + direction * 100f, Vector3.zero,
                    false));
            }
            
            _lastShootTime = Time.time;
        }
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, bool impact)
    {
        Vector3 startPosition = trail.transform.position;
        Vector3 direction = (hitPoint - startPosition).normalized;
        
        float distance = Vector3.Distance(startPosition, hitPoint);
        float startingDistance = distance;

        while (distance > 0f)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, 1 - (distance / startingDistance));
            distance -= Time.deltaTime * _speed;

            yield return null;
        }
        
        trail.transform.position = hitPoint;

        if (impact)
        {
        }
        
        Destroy(trail.gameObject, trail.time);
    }
}
