using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class Grenade : MonoBehaviour
{
    [SerializeField] private float _detonationTime = 3f;
    [SerializeField] private float _explosionRadius = 5f;
    [SerializeField] private float _explosionDamage = 100f;
    [SerializeField] private float _explosionForce = 500f;
    [SerializeField] private LayerMask _damageLayers;
    [SerializeField] private VisualEffectData _explosionEffect;
    [SerializeField] private SoundEffectData _explosionSoundEffectData;
    
    private Rigidbody _rigidbody;
    private bool _hasDetonated;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 initialVelocity)
    {
        _rigidbody.linearVelocity = initialVelocity;
        _rigidbody.AddTorque(Random.insideUnitSphere * 5f, ForceMode.VelocityChange);

        StartCoroutine(DetonationSequence());
    }

    private IEnumerator DetonationSequence()
    {
        var elapsedTime = 0f;
        while (elapsedTime < _detonationTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Detonate();
    }

    private void Detonate()
    {
        if (_hasDetonated) return;
        _hasDetonated = true;
        
        SoundEffectManager.Instance.PlaySound(_explosionSoundEffectData, transform.position);

        VisualEffectManager.Instance.PlayEffect(_explosionEffect, transform.position, Quaternion.identity);
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _explosionRadius, _damageLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider hitCollider in hitColliders)
        {
            float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
            float damageMultiplier = 1 - (distance / _explosionDamage);
            float damage = _explosionDamage * damageMultiplier;
            
            if (hitCollider.TryGetComponent(out Rigidbody rigidbody) && hitCollider.TryGetComponent(out ZombieController zombieController))
            {
                zombieController.EnableRagDoll(2f);
                rigidbody.AddExplosionForce(_explosionForce, transform.position, _explosionRadius, 1f, ForceMode.Impulse);
            }
            
            if (hitCollider.TryGetComponent(out Health health))
            {
                health.TryChangeHealth(-damage);
            }
        }
        
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}
