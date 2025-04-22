using UnityEngine;

public class SniperRifle : WeaponBase
{
    public override void TryToShoot()
    {
        if (!(Time.time > _lastShootTime + _shootDelay)) return;
        
        _lastShootTime = Time.time;
        
        _muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _muzzleFlash.Play();
            
        Shoot();
            
        _lastShootTime = Time.time;
    }
}
