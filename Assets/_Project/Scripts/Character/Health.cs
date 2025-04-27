using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    public event Action OnHealthReachedZero;
    public event Action<float, float> OnHealthChanged;
    
    [SerializeField] private float _maxHealth = 100f;
    
    private float _currentHealth;
    public bool IsDead => (_currentHealth <= 0f || _isDead);
    private bool _isDead;
    
    public float MaxHealth => _maxHealth;
    public float CurrentHealth => _currentHealth;

    private void Awake()
    {
        _currentHealth = _maxHealth;
        _isDead = false;
    }

    public void ResetHealth()
    {
        _currentHealth = _maxHealth;
        _isDead = false;
    }
    
    public void TryChangeHealth(float amount)
    {
        if (_isDead) return;
        
        float previousHealth = _currentHealth;
        _currentHealth += amount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
        
        OnHealthChanged?.Invoke(_currentHealth, previousHealth);
        
        Debug.Log($"GameObject {this.name} current health: {_currentHealth}");

        if (_currentHealth <= 0)
        {
            _isDead = true;
            OnHealthReachedZero?.Invoke();
        }
    }
}
