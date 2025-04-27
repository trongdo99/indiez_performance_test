using UnityEngine;

public class SniperRifle : WeaponBase
{
    public override bool TryToShoot()
    {
        if (!(Time.time > _lastShootTime + _shootDelay)) return false;
        
        _lastShootTime = Time.time;
        
        PlayMuzzleEffect();
        
        PlayGunShotSoundEffect();
        
        PlayImpulseEffect();
        
        FireProjectile();

        return true;
    }
}