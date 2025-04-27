using UnityEngine;

[RequireComponent(typeof(Light))]
public class FireLightController : MonoBehaviour
{
    [Header("Fire Light Settings")]
    [SerializeField] private float _baseIntensity = 1.5f;
    [SerializeField, Range(0.0f, 1.0f)] private float _intensityVariation = 0.3f;
    [SerializeField] private float _flickerSpeed = 5.0f;
    
    [Header("Color Settings")]
    [SerializeField] private Color _baseColor = new Color(1.0f, 0.6f, 0.1f, 1.0f);
    [SerializeField, Range(0.0f, 0.3f)] 
    private float _redVariation = 0.05f;
    [SerializeField, Range(0.0f, 0.3f)] 
    private float _greenVariation = 0.15f;
    
    [Header("Settings")]
    [SerializeField] private bool _usePerlinNoise = true;
    [SerializeField] private bool _useSineWave = false;
    [SerializeField] private float _sineWaveSpeed = 2.0f;
    [SerializeField] private float _sineMagnitude = 0.2f;
    
    private Light _fireLight;
    private float _noiseOffset;
    private float _colorNoiseOffset;
    
    private void Awake()
    {
        _fireLight = GetComponent<Light>();
        _noiseOffset = Random.Range(0, 1000.0f);
        _colorNoiseOffset = Random.Range(0, 1000.0f);
    }
    
    private void Start()
    {
        _fireLight.color = _baseColor;
    }
    
    private void Update()
    {
        UpdateLightIntensity();
        
        UpdateLightColor();
    }
    
    private void UpdateLightIntensity()
    {
        var fluctuation = 1.0f;
        
        if (_usePerlinNoise)
        {
            float noise = Mathf.PerlinNoise(Time.time * _flickerSpeed, _noiseOffset);
            fluctuation *= 1.0f + (noise - 0.5f) * _intensityVariation;
        }
        
        if (_useSineWave)
        {
            float sine = Mathf.Sin(Time.time * _sineWaveSpeed);
            fluctuation *= 1.0f + sine * _sineMagnitude;
        }
        
        _fireLight.intensity = _baseIntensity * fluctuation;
    }
    
    private void UpdateLightColor()
    {
        float redNoise = Mathf.PerlinNoise(Time.time * 1.5f, _colorNoiseOffset);
        float greenNoise = Mathf.PerlinNoise(Time.time * 2.0f, _colorNoiseOffset + 100);
        
        Color currentColor = _baseColor;
        currentColor.r += (redNoise - 0.5f) * _redVariation;
        currentColor.g += (greenNoise - 0.5f) * _greenVariation;
        
        _fireLight.color = currentColor;
    }
}
