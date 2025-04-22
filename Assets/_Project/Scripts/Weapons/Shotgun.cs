using UnityEngine;

public class Shotgun : WeaponBase
{
    [SerializeField] private float _bulletsPerShoot = 5;
    
    public override void TryToShoot()
    {
        if (!(Time.time > _lastShootTime + _shootDelay)) return;
        
        _lastShootTime = Time.time;
        
        _muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _muzzleFlash.Play();

        for (int i = 0; i < _bulletsPerShoot; i++)
        {
            Shoot();
        }
            
        _lastShootTime = Time.time;
    }
}
