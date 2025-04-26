using UnityEngine;

public class Shotgun : WeaponBase
{
    [SerializeField] private int _pelletsPerShot = 5;
    [SerializeField] private Vector2 _pelletSpread = new Vector2(0.1f, 0.1f);  // Override the base spread for shotgun pellets
    [SerializeField] private bool _useRandomizedSpread = true;
    [SerializeField] private float _pelletSpreadAngle = 30f;  // For cone-shaped spread pattern
    
    public override void TryToShoot()
    {
        if (!(Time.time > _lastShootTime + _shootDelay)) return;
        
        _lastShootTime = Time.time;
        
        // Play muzzle effect only once
        PlayMuzzleEffect();
        
        if (_useRandomizedSpread)
        {
            // Random spread pattern
            for (int i = 0; i < _pelletsPerShot; i++)
            {
                FireProjectile();
            }
        }
        else
        {
            // Organized cone pattern
            FirePelletPattern();
        }
    }
    
    // This method fires a single projectile without playing muzzle effect
    protected virtual void FireProjectile()
    {
        Vector3 shootDirection = _bulletSpawnPosition.forward + new Vector3(
            Random.Range(-_pelletSpread.x, _pelletSpread.x), 
            Random.Range(-_pelletSpread.y, _pelletSpread.y), 
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
    
    private void FirePelletPattern()
    {
        // Fire one pellet straight ahead
        Vector3 shootDirection = _bulletSpawnPosition.forward;
        FirePatternedProjectile(shootDirection);
        
        // If we only want one pellet, we're done
        if (_pelletsPerShot <= 1) return;
        
        // Calculate how to distribute the remaining pellets in a cone
        int remainingPellets = _pelletsPerShot - 1;
        
        // If we have an even number of pellets remaining, distribute them evenly
        // Otherwise, we already fired the center pellet, distribute the rest
        float angleStep = _pelletSpreadAngle / (remainingPellets > 1 ? remainingPellets - 1 : 1);
        float currentAngle = -_pelletSpreadAngle / 2;
        
        for (int i = 0; i < remainingPellets; i++)
        {
            // Calculate direction based on angle
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            Vector3 direction = rotation * _bulletSpawnPosition.forward;
            
            FirePatternedProjectile(direction);
            
            currentAngle += angleStep;
        }
    }
    
    private void FirePatternedProjectile(Vector3 direction)
    {
        TrailRenderer trail = Instantiate(_bulletTrail, _bulletSpawnPosition.position, Quaternion.identity);

        if (Physics.Raycast(_bulletSpawnPosition.position, direction, out RaycastHit hit, Mathf.Infinity, _collidableLayerMask, QueryTriggerInteraction.Ignore))
        {
            StartCoroutine(SpawnTrail(trail, hit.point, hit.normal, hit, true));
        }
        else
        {
            StartCoroutine(SpawnTrail(trail, _bulletSpawnPosition.position + direction * _missDistance, Vector3.zero, hit, false));
        }
    }
}