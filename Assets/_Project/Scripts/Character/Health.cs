using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    
    private float _currentHealth;
    private bool _isDead;

    private void Awake()
    {
        _currentHealth = _maxHealth;
        _isDead = false;
    }

    public void TryChangeHealth(float amount)
    {
        if (_isDead) return;
        
        _currentHealth += amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);

        if (_currentHealth <= 0)
        {
            Debug.Log($"GameObject {gameObject.name} died");
            _isDead = true;
            Destroy(gameObject);
        }
    }
}
