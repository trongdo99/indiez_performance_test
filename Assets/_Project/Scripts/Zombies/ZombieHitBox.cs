using System;
using UnityEngine;

public class ZombieHitBox : MonoBehaviour
{
    public event Action<Health> OnPlayerHit;
    
    private Collider _collider;
    
    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.enabled = false;
        _collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerCharacter") && other.TryGetComponent(out Health health))
        {
            OnPlayerHit?.Invoke(health);
        }
    }
}
