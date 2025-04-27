using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerCharacterController))]
public class WeaponController : MonoBehaviour
{
    [SerializeField] private WeaponType _startingWeaponType;
    [SerializeField] private Transform _weaponParent;
    [SerializeField] private List<WeaponBase> _startingWeapons;
    [SerializeField] private Transform _leftHandIkTargetTransform;
    [SerializeField] private Transform _leftHandIkHintTransform;
    [SerializeField] private Transform _aimIKTarget;
    [SerializeField] private Rig _aimRig;
    [SerializeField] private Rig _handsRig;
    [SerializeField] private float _aimAngleThreshold = 5f;

    private PlayerCharacterController _characterController;
    private Dictionary<WeaponType, WeaponBase> _weapons = new Dictionary<WeaponType, WeaponBase>();
    private WeaponBase _currentWeapon;
    private Transform _weaponLeftHandAttachTransform;
    private Transform _weaponLeftHandHintTransform;
    private Transform _currentTargetTransform;
    private bool _isAiming;

    private void Awake()
    {
        _characterController = GetComponent<PlayerCharacterController>();
        
        InitializeWeapons();
    }

    private void InitializeWeapons()
    {
        _weapons.Clear();
        
        foreach (WeaponBase weapon in _startingWeapons)
        {
            WeaponBase weaponInstance = Instantiate(weapon, _weaponParent);
            _weapons[weaponInstance.WeaponType] = weaponInstance;
            weaponInstance.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // Equip the starting weapon if exists
        if (_weapons.ContainsKey(_startingWeaponType))
        {
            SwitchWeapon(_startingWeaponType);
        }
        else if (_weapons.Count > 0)
        {
            // Otherwise use the first available weapon
            foreach (WeaponBase weapon in _weapons.Values)
            {
                SwitchWeapon(weapon.WeaponType);
                break;
            }
        }
    }

    private void Update()
    {
        UpdateHandsIK();
        
        UpdateHandIKPositions();
        
        // Check if aiming at target
        if (!_characterController.IsThrowing && _isAiming && _currentTargetTransform != null)
        {
            UpdateAimingAndFiring();
            _aimIKTarget.position = _currentTargetTransform.position;
        }
    }

    private void UpdateHandsIK()
    {
        // Turn off all IK if the player is dead
        if (!_characterController.IsAlive)
        {
            _aimRig.weight = 0;
            _handsRig.weight = 0;
            return;
        }
        
        if (_characterController.IsThrowing)
        {
            _handsRig.weight = 0f;
        }
        else
        {
            _handsRig.weight = 1f;
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
        Vector3 directionToTarget = (_currentTargetTransform.position - _currentWeapon.transform.position).normalized;
        float angle = Vector3.Angle(_currentWeapon.transform.forward, directionToTarget);
        
        if (angle < _aimAngleThreshold)
        {
            _currentWeapon.TryToShoot();
        }
    }

    public void SetWeaponVisibility(bool isVisible)
    {
        if (_currentWeapon == null) return;
        _currentWeapon.gameObject.SetActive(isVisible);
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

    public void CycleThroughWeapons()
    {
        if (_weapons.Count <= 1)
        {
            Debug.Log("Only one or zero weapons available, cannot cycle.");
            return;
        }
        
        // Get all weapon types from the dictionary
        WeaponType[] weaponTypes = _weapons.Keys.ToArray();
        
        // Sort the weapon types to ensure consistent ordering
        Array.Sort(weaponTypes);
        
        // Find the current weapon index
        int currentIndex = -1;
        if (_currentWeapon != null)
        {
            for (int i = 0; i < weaponTypes.Length; i++)
            {
                if (weaponTypes[i] == _currentWeapon.WeaponType)
                {
                    currentIndex = i;
                    break;
                }
            }
        }
        
        // Calculate the next index, looping back to 0 if at the end
        int nextIndex = (currentIndex + 1) % weaponTypes.Length;
        
        // Switch to the next weapon
        SwitchWeapon(weaponTypes[nextIndex]);
    }

    public void SwitchWeapon(WeaponType toWeapon)
    {
        if (_currentWeapon != null && _currentWeapon.WeaponType == toWeapon) return;

        if (!_weapons.ContainsKey(toWeapon)) return;
        
        // Unequip current weapon
        if (_currentWeapon != null)
        {
            _currentWeapon.Unequip();
        }

        // Equip the new weapon
        _currentWeapon = _weapons[toWeapon];
        _currentWeapon.Equip();
        
        Debug.Log($"Switch weapon to {toWeapon}");

        _weaponLeftHandAttachTransform = _currentWeapon.LeftHandAttachTransform;
        _weaponLeftHandHintTransform = _currentWeapon.LeftHandHinTransform;
    }
}