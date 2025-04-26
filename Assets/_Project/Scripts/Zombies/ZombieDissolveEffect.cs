using System;
using System.Collections;
using UnityEngine;

public class ZombieDissolveEffect : MonoBehaviour
{
    public event Action OnDissolveCompleted;
    
    [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
    [SerializeField] private float _dissolvedSpeed = 1f;
    [SerializeField] private bool _turnOffShadow = true;
    
    private MaterialPropertyBlock _propertyBlock;

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
    }

    public void StartDissolveEffect()
    {
        if (_turnOffShadow)
        {
            _skinnedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        
        StartCoroutine(DissolveCoroutine());
    }

    public void ResetDissolveEffect()
    {
        _propertyBlock.SetFloat("_DissolveAmount", 0f);
        _skinnedMeshRenderer.SetPropertyBlock(_propertyBlock);
    }

    private IEnumerator DissolveCoroutine()
    {
        var dissolveAmount = 0f;
        while (dissolveAmount < 1f)
        {
            dissolveAmount += _dissolvedSpeed * Time.deltaTime;
            _propertyBlock.SetFloat("_DissolveAmount", dissolveAmount);
            _skinnedMeshRenderer.SetPropertyBlock(_propertyBlock);
            yield return null;
        }
        
        _propertyBlock.SetFloat("_DissolveAmount", 1f);
        _skinnedMeshRenderer.SetPropertyBlock(_propertyBlock);
        OnDissolveCompleted?.Invoke();
    }
}
