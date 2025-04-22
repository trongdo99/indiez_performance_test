using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private WeaponType _currentWeapon;
    [SerializeField] private GameObject _shotgun;
    [SerializeField] private GameObject _sniperRifle;
    [SerializeField] private Transform _leftHandIkTargetTransform;
    [SerializeField] private Transform _leftHandIkHintTransform;

    private Transform _weaponLeftHandAttachTransform;
    private Transform _weaponLeftHandHintTransform;
    
    private void Start()
    {
        SetUpWeapon(_currentWeapon);
    }

    private void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            SwitchWeapon(_currentWeapon == WeaponType.Shotgun ? WeaponType.SniperRifle : WeaponType.Shotgun);
        }
        
        _leftHandIkTargetTransform.position = _weaponLeftHandAttachTransform.position;
        _leftHandIkTargetTransform.rotation = _weaponLeftHandAttachTransform.rotation;
        _leftHandIkHintTransform.position = _weaponLeftHandHintTransform.position;
        _leftHandIkHintTransform.rotation = _weaponLeftHandHintTransform.rotation;
    }

    public void SwitchWeapon(WeaponType toWeapon)
    {
        if (toWeapon == _currentWeapon) return;
        
        CleanUpPreviousWeapon(_currentWeapon);
        _currentWeapon = toWeapon;
        SetUpWeapon(toWeapon);
    }

    private void SetUpWeapon(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Shotgun:
                _shotgun.SetActive(true);
                _weaponLeftHandAttachTransform = _shotgun.transform.GetChild(0).transform;
                _weaponLeftHandHintTransform = _shotgun.transform.GetChild(1).transform;
                break;
            case WeaponType.SniperRifle:
                _sniperRifle.SetActive(true);
                _weaponLeftHandAttachTransform = _sniperRifle.transform.GetChild(0).transform;
                _weaponLeftHandHintTransform = _sniperRifle.transform.GetChild(1).transform;
                break;
        }
    }

    private void CleanUpPreviousWeapon(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Shotgun:
                _shotgun.SetActive(false);
                break;
            case WeaponType.SniperRifle:
                _sniperRifle.SetActive(false);
                break;
        }
    }
}
