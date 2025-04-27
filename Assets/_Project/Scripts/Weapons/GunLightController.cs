using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MuzzleFlashLightSettings
{
    [Header("Light Properties")]
    public Color LightColor = new Color(1f, 0.7f, 0.3f, 1f);
    public float Intensity = 3f;
    public float Range = 5f;
    
    [Header("Animation")]
    public float Duration = 0.07f;
    public AnimationCurve FadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [Tooltip("Random intensity variation")]
    public float IntensityVariation = 0.2f;
    
    [Header("Shadow Settings")]
    public bool CastShadows = false;
    public LightShadows ShadowType = LightShadows.Hard;
    public float ShadowStrength = 0.8f;
}

public class GunLightController : MonoBehaviour
{
    [SerializeField] private MuzzleFlashLightSettings _lightSettings;
    [SerializeField] private Transform _lightAttachPoint;
    
    private Light _muzzleFlashLight;
    private Coroutine _currentLightRoutine;
    
    private void Awake()
    {
        SetupLight();
    }
    
    private void SetupLight()
    {
        if (_muzzleFlashLight != null) return;
        
        var lightObj = new GameObject("MuzzleFlashLight");
        lightObj.transform.SetParent(_lightAttachPoint != null ? _lightAttachPoint : transform);
        lightObj.transform.localPosition = Vector3.zero;
        lightObj.transform.localRotation = Quaternion.identity;
        
        _muzzleFlashLight = lightObj.AddComponent<Light>();
        _muzzleFlashLight.type = LightType.Point;
        _muzzleFlashLight.color = _lightSettings.LightColor;
        _muzzleFlashLight.range = _lightSettings.Range;
        _muzzleFlashLight.intensity = 0; // Start with light off
        _muzzleFlashLight.shadows = _lightSettings.CastShadows ? _lightSettings.ShadowType : LightShadows.None;
        _muzzleFlashLight.shadowStrength = _lightSettings.ShadowStrength;
        _muzzleFlashLight.enabled = false;
    }
    
    public void TriggerMuzzleFlash()
    {
        if (_muzzleFlashLight == null)
        {
            SetupLight();
        }
        
        if (_currentLightRoutine != null)
        {
            StopCoroutine(_currentLightRoutine);
        }
        
        _currentLightRoutine = StartCoroutine(FlashRoutine());
    }
    
    private IEnumerator FlashRoutine()
    {
        float baseIntensity = _lightSettings.Intensity;
        if (_lightSettings.IntensityVariation > 0)
        {
            baseIntensity += Random.Range(-_lightSettings.IntensityVariation, _lightSettings.IntensityVariation);
        }
        
        _muzzleFlashLight.enabled = true;
        
        float elapsed = 0;
        while (elapsed < _lightSettings.Duration)
        {
            float curveValue = _lightSettings.FadeCurve.Evaluate(elapsed / _lightSettings.Duration);
            _muzzleFlashLight.intensity = baseIntensity * curveValue;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        _muzzleFlashLight.intensity = 0;
        _muzzleFlashLight.enabled = false;
        _currentLightRoutine = null;
    }
}