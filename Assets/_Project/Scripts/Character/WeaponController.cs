using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerCharacterController))]
public class WeaponController : MonoBehaviour
{
    [SerializeField] private WeaponType _currentWeapon;
    [SerializeField] private GameObject _shotgun;
    [SerializeField] private GameObject _sniperRifle;
    
    [SerializeField] private Transform _leftHandIkTargetTransform;
    [SerializeField] private Transform _leftHandIkHintTransform;
    [SerializeField] private Transform _aimIKTarget;
    [SerializeField] private Rig _aimRig;
    [SerializeField] private Rig _handsRig;
    [SerializeField] private float _aimAngleThreshold = 5f;

    private PlayerCharacterController _characterController;
    private WeaponBase _weaponBase;
    private Transform _weaponLeftHandAttachTransform;
    private Transform _weaponLeftHandHintTransform;
    private Transform _currentTargetTransform;
    private bool _isAiming;

    private void Awake()
    {
        _characterController = GetComponent<PlayerCharacterController>();
    }

    private void Start()
    {
        SetUpWeapon(_currentWeapon);
        _characterController.OnDeath += HandlePlayerOnDeath;
    }

    private void OnDestroy()
    {
        _characterController.OnDeath -= HandlePlayerOnDeath;
    }

    private void Update()
    {
        // Handle weapon switch input
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            SwitchWeapon(_currentWeapon == WeaponType.Shotgun ? WeaponType.SniperRifle : WeaponType.Shotgun);
        }
        
        // Update IK positions
        UpdateHandIKPositions();
        
        // Check if aiming at target
        if (_isAiming && _currentTargetTransform != null)
        {
            UpdateAimingAndFiring();
            _aimIKTarget.position = _currentTargetTransform.position;
        }
    }
    
    private void UpdateHandIKPositions()
    {
        if (_weaponLeftHandAttachTransform != null && _weaponLeftHandHintTransform != null)
        {
            _leftHandIkTargetTransform.position = _weaponLeftHandAttachTransform.position;
            _leftHandIkTargetTransform.rotation = _weaponLeftHandAttachTransform.rotation;
            _leftHandIkHintTransform.position = _weaponLeftHandHintTransform.position;
            _leftHandIkHintTransform.rotation = _weaponLeftHandHintTransform.rotation;
        }
    }
    
    private void UpdateAimingAndFiring()
    {
        Vector3 directionToTarget = (_currentTargetTransform.position - _weaponBase.transform.position).normalized;
        float angle = Vector3.Angle(_weaponBase.transform.forward, directionToTarget);
        
        if (angle < _aimAngleThreshold)
        {
            _weaponBase.TryToShoot();
        }
    }

    public void Aiming(Transform targetTransform)
    {
        _currentTargetTransform = targetTransform;
        _aimIKTarget.position = targetTransform.position;
        _aimRig.weight = 1f;
        _isAiming = true;
    }

    public void StopAiming()
    {
        _currentTargetTransform = null;
        _aimRig.weight = 0f;
        _isAiming = false;
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
                _weaponBase = _shotgun.GetComponent<WeaponBase>();
                _weaponLeftHandAttachTransform = _shotgun.transform.GetChild(0).transform;
                _weaponLeftHandHintTransform = _shotgun.transform.GetChild(1).transform;
                break;
            case WeaponType.SniperRifle:
                _sniperRifle.SetActive(true);
                _weaponBase = _sniperRifle.GetComponent<WeaponBase>();
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

    private void HandlePlayerOnDeath()
    {
        _aimRig.weight = 0f;
        _handsRig.weight = 0f;
        _isAiming = false;
    }
}