using System;
using System.Collections;
using UnityEngine;

[System.Serializable]
public class ExplosionLightSettings
{
    [Header("Light Properties")]
    public Color InitialColor = new Color(1f, 0.8f, 0.3f, 1f);
    public Color SecondaryColor = new Color(1f, 0.4f, 0.1f, 1f);
    public float Intensity = 8f;
    public float Range = 15f;
    
    [Header("Animation")]
    public float Duration = 1.2f;
    public AnimationCurve IntensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public bool UseColorGradient = true;
    [Range(0f, 1f)] public float ColorTransitionPoint = 0.3f;
    
    [Header("Flicker Effect")]
    public bool UseFlicker = true;
    [Range(0, 0.5f)] public float FlickerIntensity = 0.2f;
    public float FlickerSpeed = 20f;
    
    [Header("Shadow Settings")]
    public bool CastShadows = true;
    public LightShadows ShadowType = LightShadows.Hard;
    public float ShadowStrength = 1f;
}

public class ExplosionLightController : MonoBehaviour
{
    [Header("Light Configuration")]
    [SerializeField] private ExplosionLightSettings _lightSettings;
    [SerializeField] private Transform _lightAttachPoint;
    
    private Light _explosionLight;
    private Light[] _secondaryLights;
    private Coroutine _currentLightRoutine;
    private Coroutine[] _secondaryLightRoutines;

    private void Awake()
    {
        SetupLight();
    }

    private void SetupLight()
    {
        if (_explosionLight != null) return;
        
        GameObject lightObj = new GameObject("ExplosionLight");
        lightObj.transform.SetParent(_lightAttachPoint != null ? _lightAttachPoint : transform);
        lightObj.transform.localPosition = Vector3.zero;
        lightObj.transform.localRotation = Quaternion.identity;
        
        _explosionLight = lightObj.AddComponent<Light>();
        _explosionLight.type = LightType.Point;
        _explosionLight.color = _lightSettings.InitialColor;
        _explosionLight.range = _lightSettings.Range;
        _explosionLight.intensity = 0; // Start with light off
        _explosionLight.shadows = _lightSettings.CastShadows ? _lightSettings.ShadowType : LightShadows.None;
        _explosionLight.shadowStrength = _lightSettings.ShadowStrength;
        _explosionLight.enabled = false;
    }
    
    public void TriggerExplosion()
    {
        if (_explosionLight == null)
        {
            SetupLight();
        }
        
        if (_currentLightRoutine != null)
        {
            StopCoroutine(_currentLightRoutine);
        }
        
        _currentLightRoutine = StartCoroutine(ExplosionLightRoutine());
    }
    
    private IEnumerator ExplosionLightRoutine()
    {
        _explosionLight.enabled = true;
        
        float elapsed = 0;
        float duration = _lightSettings.Duration;
        
        while (elapsed < duration)
        {
            float normalizedTime = elapsed / duration;
            float curveValue = _lightSettings.IntensityCurve.Evaluate(normalizedTime);
            
            float baseIntensity = _lightSettings.Intensity;
            
            if (_lightSettings.UseFlicker)
            {
                float flickerValue = Mathf.Sin(elapsed * _lightSettings.FlickerSpeed) * _lightSettings.FlickerIntensity;
                baseIntensity *= (1 + flickerValue);
            }
            
            _explosionLight.intensity = baseIntensity * curveValue;
            
            if (_lightSettings.UseColorGradient)
            {
                float colorBlend = normalizedTime < _lightSettings.ColorTransitionPoint ? 
                    normalizedTime / _lightSettings.ColorTransitionPoint : 1f;
                
                _explosionLight.color = Color.Lerp(_lightSettings.InitialColor, _lightSettings.SecondaryColor, colorBlend);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        _explosionLight.intensity = 0;
        _explosionLight.enabled = false;
        _currentLightRoutine = null;
    }
}