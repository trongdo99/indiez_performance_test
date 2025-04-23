using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public event Action OnDeath;
    
    [SerializeField] private float _maxHealth = 100f;
    
    private float _currentHealth;
    public bool IsDead => (_currentHealth <= 0f || _isDead);
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
        
        Debug.Log($"GameObject {this.name} current health: {_currentHealth}");

        if (_currentHealth <= 0)
        {
            Debug.Log($"GameObject {gameObject.name} died");
            _isDead = true;
            OnDeath?.Invoke();
        }
    }
}
