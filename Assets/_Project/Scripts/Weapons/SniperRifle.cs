using UnityEngine;

public class SniperRifle : WeaponBase
{
    public override void TryToShoot()
    {
        if (!(Time.time > _lastShootTime + _shootDelay)) return;
        
        _lastShootTime = Time.time;
        
        PlayMuzzleEffect();
        
        PlayGunShotSoundEffect();
        
        FireProjectile();
    }
}